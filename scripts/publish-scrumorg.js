/**
 * Scrum.org Course Publish Automation
 *
 * Creates a Scrum.org course listing by copying the most recent course
 * of the same type for the same trainer, then updating dates and registration URL.
 *
 * Usage: node publish-scrumorg.js '{"courseType":"PSMA","startDate":"2026-07-15","endDate":"2026-07-16","trainerName":"Alex Brown","registrationUrl":"https://www.bagile.co.uk/course-schedule/psma-150726/","username":"alexbrown@bagile.co.uk","password":"..."}'
 *
 * Output: JSON to stdout with { listingUrl: "https://www.scrum.org/courses/..." }
 * Errors: exit code 1 with message to stderr
 *
 * Page structure (Drupal CMS):
 * - Course management: /admin/courses/manage (paginated table)
 * - Each row has: Date | Title | Trainer | Location | Type | Status | Language | Pricing | Operations
 * - Operations column has: Edit (/node/{id}/edit), dropdown with Copy (/node/{id}/replicate?destination)
 * - Copy uses Drupal Replicate module — creates a clone and redirects to the edit form
 * - Edit form has date fields, registration URL field, save button
 */

const { chromium } = require('playwright');

// Scrum.org taxonomy IDs for course types (used when creating courses from scratch)
const COURSE_TYPE_TAXONOMY_ID = {
    'PSM':    '104',
    'PSPO':   '105',
    'PSK':    '133',
    'PALE':   '130',
    'EBM':    '248',
    'APS':    '103',
    'APSSD':  '108',
    'PSMA':   '164',
    'PSPOA':  '210',
    'PSMAI':  '318',
    'PSPOAI': '313',
    'PSFS':   '292',
    'PSU':    '200',
};

const COURSE_TYPE_MAP = {
    'PSM':    'Professional Scrum Master',
    'PSPO':   'Professional Scrum Product Owner',
    'PSK':    'Professional Scrum with Kanban',
    'PALE':   'Professional Agile Leadership - Essentials',
    'EBM':    'Professional Agile Leadership - Evidence Based Management',
    'APS':    'Applying Professional Scrum',
    'APSSD':  'Applying Professional Scrum for Software Development',
    'PSMA':   'Professional Scrum Master - Advanced',
    'PSPOA':  'Professional Scrum Product Owner - Advanced',
    'PSMAI':  'Professional Scrum Master - AI Essentials',
    'PSPOAI': 'Professional Scrum Product Owner - AI Essentials',
    'PSFS':   'Professional Scrum Facilitation Skills',
    'PSU':    'Professional Scrum with User Experience',
};

async function main() {
    // Support reading args from a file: node publish-scrumorg.js @/path/to/args.json
    const rawArg = process.argv[2];
    const args = rawArg.startsWith('@')
        ? JSON.parse(require('fs').readFileSync(rawArg.slice(1), 'utf-8'))
        : JSON.parse(rawArg);
    const { courseType, startDate, endDate, trainerName, registrationUrl, username, password, cookies, templateNodeId: fixedTemplateNodeId } = args;

    const courseName = COURSE_TYPE_MAP[courseType.toUpperCase()];
    if (!courseName) {
        throw new Error(`Unknown course type: ${courseType}`);
    }

    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext();

    // Restore session cookies if provided (avoids needing password)
    if (cookies && cookies.length > 0) {
        await context.addCookies(cookies);
        console.error(`Restored ${cookies.length} session cookies`);
    }

    const page = await context.newPage();
    page.setDefaultTimeout(30000);

    try {
        // 1. Login to Scrum.org (skip if session cookies are valid)
        console.error('Checking Scrum.org session...');
        await page.goto('https://www.scrum.org/user/login');

        // Check if already logged in (redirected away from login page)
        if (page.url().includes('/user/login')) {
            if (!username || !password) {
                throw new Error('Not logged in via cookies and no username/password provided');
            }
            console.error('Session expired — logging in with credentials...');
            await page.fill('input[name="name"]', username);
            await page.fill('input[name="pass"]', password);
            await page.click('input[type="submit"][value="Log in"]');
            await page.waitForNavigation({ waitUntil: 'networkidle' });
        }
        console.error('Logged in');

        // 2. Find template node ID (either supplied directly or by searching the course list)
        let templateNodeId = fixedTemplateNodeId || null;

        if (!templateNodeId) {
            await page.goto('https://www.scrum.org/admin/courses/manage');
            await page.waitForSelector('table');
            console.error('On course management page — searching for template...');

            const rows = await page.$$('table tbody tr');
            for (const row of rows) {
                const cells = await row.$$('td');
                if (cells.length < 9) continue;

                const titleText = await cells[1].textContent();
                const trainerText = await cells[2].textContent();

                const titleClean = titleText.replace(/\s+/g, ' ').trim().replace(/👤\s*\d+/, '').trim();
                if (titleClean !== courseName) continue;
                if (!trainerText.includes(trainerName)) continue;

                const editLink = await cells[8].$('a[href*="/node/"]');
                if (editLink) {
                    const href = await editLink.getAttribute('href');
                    const match = href.match(/\/node\/(\d+)\//);
                    if (match) {
                        templateNodeId = match[1];
                        console.error(`Found template: node ${templateNodeId} — "${titleClean}" by ${trainerText.trim()}`);
                        break;
                    }
                }
            }

            if (!templateNodeId) {
                throw new Error(`No existing "${courseName}" course found for ${trainerName} on the current page. Check if more pages need to be scanned.`);
            }
        } else {
            console.error(`Using supplied template node: ${templateNodeId}`);
        }

        // 4. Copy via Replicate, or create from scratch if permission denied
        let newNodeId = null;
        let createdFromScratch = false;

        console.error(`Copying course node ${templateNodeId}...`);
        await page.goto(`https://www.scrum.org/node/${templateNodeId}/replicate?destination`);
        await page.waitForLoadState('networkidle');
        console.error(`After replicate, URL: ${page.url()}`);

        const afterReplicateUrl = page.url();
        const replicateWorked = !afterReplicateUrl.includes(`/node/${templateNodeId}/replicate`);

        if (!replicateWorked) {
            // 403 or confirmation page stuck — fall back to creating from scratch
            console.error('Replicate blocked or stuck — creating course from scratch...');
            createdFromScratch = true;

            const taxonomyId = COURSE_TYPE_TAXONOMY_ID[courseType.toUpperCase()];
            if (!taxonomyId) throw new Error(`No taxonomy ID for course type: ${courseType}`);

            await page.goto('https://www.scrum.org/node/add/course');
            await page.waitForLoadState('networkidle');

            // Helper: set a <select> value even if hidden by Select2/Chosen
            async function setSelect(selector, value) {
                await page.evaluate(({ sel, val }) => {
                    const el = document.querySelector(sel);
                    if (el) { el.value = val; el.dispatchEvent(new Event('change', { bubbles: true })); }
                }, { sel: selector, val: value });
            }

            // Set course type
            await setSelect('#edit-field-type-of-course-taxonomy', taxonomyId);
            console.error(`Set course type: ${courseType} (taxonomy ${taxonomyId})`);

            // Set scope to Public
            await setSelect('#edit-field-course-scope', 'Public');

            // Set class format to Traditional
            await setSelect('#edit-field-class-format', 'Traditional');

            // Delivery method: Virtual Classroom
            await page.evaluate(() => {
                const el = document.querySelector('#edit-field-course-delivery-method-virtualclassroom');
                if (el) { el.checked = true; el.dispatchEvent(new Event('change', { bubbles: true })); }
            });

            // Language: English
            await setSelect('#edit-field-course-language-0-value', 'en');

            // Set all remaining fields via JS to bypass visibility constraints
            await page.evaluate(({ regUrl, sd, ed }) => {
                function setField(sel, val) {
                    const el = document.querySelector(sel);
                    if (!el) return false;
                    el.value = val;
                    el.dispatchEvent(new Event('input', { bubbles: true }));
                    el.dispatchEvent(new Event('change', { bubbles: true }));
                    return true;
                }
                // Open for registration checkbox
                const cb = document.querySelector('#edit-field-open-for-registration-value');
                if (cb && !cb.checked) { cb.checked = true; cb.dispatchEvent(new Event('change', { bubbles: true })); }
                // Registration method
                setField('#edit-registration-method', 'external');
                // Registration URL (may be conditionally hidden)
                setField('#edit-field-register-link-0-uri', regUrl);
                // Dates
                setField('#edit-field-start-date-0-value-date', sd);
                setField('#edit-field-end-date-0-value-date', ed);
            }, { regUrl: registrationUrl, sd: startDate, ed: endDate });
            console.error(`Set registration URL: ${registrationUrl}`);
            console.error(`Set dates: ${startDate} — ${endDate}`);

        } else {
            // If landed on a confirmation page, click through
            const confirmBtn = await page.$('input[type="submit"], button[type="submit"]');
            if (confirmBtn && afterReplicateUrl.includes('/replicate')) {
                console.error('Confirmation page — clicking submit...');
                await confirmBtn.click();
                await page.waitForLoadState('networkidle');
            }
            console.error(`Replicated — now editing at: ${page.url()}`);
            const newNodeMatch = page.url().match(/\/node\/(\d+)\/edit/);
            newNodeId = newNodeMatch ? newNodeMatch[1] : null;
        }

        // 5–8: For replicated courses, update trainer/dates/regURL on the edit form.
        //       For scratch-created courses, these were already set above — skip.
        if (!createdFromScratch) {
            // 5. Update trainer if copying from a different trainer's course
            const trainerInput = await page.$('#edit-field-trainer-0-target-id, input[id*="trainer"]');
            if (trainerInput) {
                await trainerInput.fill('');
                await trainerInput.fill(trainerName);
                try {
                    await page.waitForSelector('.ui-autocomplete li, .autocomplete-suggestions li', { timeout: 5000 });
                    const suggestion = await page.$('.ui-autocomplete li:first-child, .autocomplete-suggestions li:first-child');
                    if (suggestion) { await suggestion.click(); console.error(`Set trainer: ${trainerName}`); }
                    else console.error(`WARNING: No autocomplete suggestion for trainer "${trainerName}"`);
                } catch { console.error(`WARNING: Trainer autocomplete did not appear`); }
            } else {
                console.error('INFO: No trainer field found');
            }

            // 7. Edit the course dates (replicate form uses field_course_date)
            const startDateInput = await page.$('#edit-field-course-date-0-value-date');
            if (startDateInput) { await startDateInput.fill(''); await startDateInput.fill(startDate); console.error(`Set start date: ${startDate}`); }
            else console.error('WARNING: Could not find start date input');

            const endDateInput = await page.$('#edit-field-course-date-0-end-value-date');
            if (endDateInput) { await endDateInput.fill(''); await endDateInput.fill(endDate); console.error(`Set end date: ${endDate}`); }
            else console.error('WARNING: Could not find end date input');
        }

        // 8. Set the registration URL (replicate form — scratch form already set it)
        const regUrlInput = createdFromScratch ? null : await page.$('#edit-field-registration-url-0-uri');
        if (regUrlInput) {
            await regUrlInput.fill('');
            await regUrlInput.fill(registrationUrl);
            console.error(`Set registration URL: ${registrationUrl}`);
        } else {
            // Try alternative selector
            const altInput = await page.$('input[name*="registration_url"], input[name*="registration-url"]');
            if (altInput) {
                await altInput.fill('');
                await altInput.fill(registrationUrl);
                console.error(`Set registration URL (alt selector): ${registrationUrl}`);
            } else {
                console.error('WARNING: Could not find registration URL input');
            }
        }

        // 9. Save the course
        // For scratch-created courses use "Save and Schedule"; for replicated use "Save"
        const saveSelector = createdFromScratch
            ? '#edit-saveandschedule, input[value="Save and Schedule"]'
            : '#edit-submit, input[value="Save"]';
        const saveButton = await page.$(saveSelector);
        if (saveButton) {
            await saveButton.click();
            await page.waitForLoadState('networkidle');
            console.error(`Course ${createdFromScratch ? 'created and scheduled' : 'saved'}`);
        } else {
            // Fallback: try any submit
            const anySubmit = await page.$('input[type="submit"]');
            if (anySubmit) { await anySubmit.click(); await page.waitForLoadState('networkidle'); }
            else throw new Error('Could not find Save button on the edit form');
        }

        // 10. Get the listing URL
        let listingUrl = page.url();

        // Extract new node ID from current URL if not set
        if (!newNodeId) {
            const match = listingUrl.match(/\/node\/(\d+)/);
            if (match) newNodeId = match[1];
        }

        // Navigate to canonical course URL if not already there
        if (newNodeId && !listingUrl.includes('/courses/')) {
            await page.goto(`https://www.scrum.org/node/${newNodeId}`);
            listingUrl = page.url();
        }

        console.error(`Listing URL: ${listingUrl}`);

        // Output result as JSON to stdout
        console.log(JSON.stringify({ listingUrl }));

    } catch (err) {
        // Take screenshot before closing to aid debugging
        try {
            const screenshotPath = require('path').join(require('os').tmpdir(), `scrumorg-error-${Date.now()}.png`);
            await page.screenshot({ path: screenshotPath, fullPage: false });
            console.error(`Screenshot saved: ${screenshotPath}`);
        } catch {}
        throw err;
    } finally {
        await browser.close();
    }
}

main().catch(err => {
    console.error(`Error: ${err.message}`);
    process.exit(1);
});

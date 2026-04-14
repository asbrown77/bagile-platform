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
    const args = JSON.parse(process.argv[2]);
    const { courseType, startDate, endDate, trainerName, registrationUrl, username, password } = args;

    const courseName = COURSE_TYPE_MAP[courseType.toUpperCase()];
    if (!courseName) {
        throw new Error(`Unknown course type: ${courseType}`);
    }

    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext();
    const page = await context.newPage();
    page.setDefaultTimeout(30000);

    try {
        // 1. Login to Scrum.org
        console.error('Logging in to Scrum.org...');
        await page.goto('https://www.scrum.org/user/login');

        // Check if already logged in (redirected to profile)
        if (page.url().includes('/user/login')) {
            await page.fill('input[name="name"]', username);
            await page.fill('input[name="pass"]', password);
            await page.click('input[type="submit"][value="Log in"]');
            await page.waitForNavigation({ waitUntil: 'networkidle' });
        }
        console.error('Logged in');

        // 2. Navigate to course management
        await page.goto('https://www.scrum.org/admin/courses/manage');
        await page.waitForSelector('table');
        console.error('On course management page');

        // 3. Find the template course — matching course type and trainer
        // Table rows have cells: Date, Title, Trainer, Location, Type, Status, Language, Pricing, Operations
        // The Edit link in Operations contains /node/{nodeId}/edit
        let templateNodeId = null;

        const rows = await page.$$('table tbody tr');
        for (const row of rows) {
            const cells = await row.$$('td');
            if (cells.length < 9) continue;

            const titleText = await cells[1].textContent();
            const trainerText = await cells[2].textContent();

            // Match exact course name (not substring — "Professional Scrum Master" should not match "Professional Scrum Master - Advanced")
            const titleClean = titleText.replace(/\s+/g, ' ').trim().replace(/👤\s*\d+/, '').trim();
            if (titleClean !== courseName) continue;
            if (!trainerText.includes(trainerName)) continue;

            // Found a match — extract node ID from the Edit link
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

        // 4. Copy the course using Drupal Replicate
        console.error(`Copying course node ${templateNodeId}...`);
        await page.goto(`https://www.scrum.org/node/${templateNodeId}/replicate?destination`);
        await page.waitForLoadState('networkidle');

        // After replication, we're on the edit form of the new copy
        const editUrl = page.url();
        console.error(`Replicated — now editing at: ${editUrl}`);

        // Extract the new node ID from the edit URL
        const newNodeMatch = editUrl.match(/\/node\/(\d+)\/edit/);
        const newNodeId = newNodeMatch ? newNodeMatch[1] : null;

        // 5. Edit the course dates
        // Scrum.org uses a Drupal date field — look for the date input
        // The field name pattern is edit-field-course-date-0-value-date
        const startDateInput = await page.$('#edit-field-course-date-0-value-date');
        if (startDateInput) {
            await startDateInput.fill('');
            await startDateInput.fill(startDate);
            console.error(`Set start date: ${startDate}`);
        } else {
            console.error('WARNING: Could not find start date input');
        }

        const endDateInput = await page.$('#edit-field-course-date-0-end-value-date');
        if (endDateInput) {
            await endDateInput.fill('');
            await endDateInput.fill(endDate);
            console.error(`Set end date: ${endDate}`);
        } else {
            console.error('WARNING: Could not find end date input');
        }

        // 6. Set the registration URL
        const regUrlInput = await page.$('#edit-field-registration-url-0-uri');
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

        // 7. Save the course
        const saveButton = await page.$('#edit-submit, input[type="submit"][value="Save"]');
        if (saveButton) {
            await saveButton.click();
            await page.waitForLoadState('networkidle');
            console.error('Course saved');
        } else {
            throw new Error('Could not find Save button on the edit form');
        }

        // 8. Get the listing URL
        // After saving, we should be on the course view page
        let listingUrl = page.url();

        // If we have the node ID, construct the canonical listing URL
        if (newNodeId && !listingUrl.includes('/courses/')) {
            // Try navigating to the view
            await page.goto(`https://www.scrum.org/node/${newNodeId}`);
            listingUrl = page.url();
        }

        console.error(`Listing URL: ${listingUrl}`);

        // Output result as JSON to stdout
        console.log(JSON.stringify({ listingUrl }));

    } finally {
        await browser.close();
    }
}

main().catch(err => {
    console.error(`Error: ${err.message}`);
    process.exit(1);
});

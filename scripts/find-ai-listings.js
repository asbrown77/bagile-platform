/**
 * Find Alex Brown's PSMAI and PSPOAI course listings on Scrum.org.
 * Searches all pages of the admin table and returns node IDs + URLs.
 */
const { chromium } = require('playwright');
const https = require('https');

async function main() {
  const cfg = await new Promise((res, rej) => {
    const opts = {
      hostname: 'api.bagile.co.uk', port: 443,
      path: '/api/admin/service-config', method: 'GET',
      headers: { 'X-Api-Key': 'Drqji6wXpwXbyp5IDcAq/V2IY+/l21Z/' },
      rejectUnauthorized: false,
    };
    let d = '';
    https.request(opts, r => { r.on('data', x => d += x); r.on('end', () => res(JSON.parse(d))); }).on('error', rej).end();
  });
  const cookies = JSON.parse(cfg.scrumorg_session_cookies);

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  await context.addCookies(cookies);
  const page = await context.newPage();
  page.setDefaultTimeout(30000);

  const found = [];
  let pageNum = 0;

  while (true) {
    const url = `https://www.scrum.org/admin/courses/manage?page=${pageNum}`;
    await page.goto(url);
    await page.waitForLoadState('networkidle');

    const rows = await page.$$('table tbody tr');
    if (rows.length === 0) break;

    for (const row of rows) {
      const cells = await row.$$('td');
      if (cells.length < 3) continue;

      const titleText = (await cells[1].textContent()).replace(/\s+/g, ' ').trim();
      const trainerText = (await cells[2].textContent()).trim();

      if (!titleText.includes('AI Essentials')) continue;
      if (!trainerText.includes('Alex Brown')) continue;

      const editLink = await cells[cells.length - 1].$('a[href*="/node/"]');
      if (editLink) {
        const href = await editLink.getAttribute('href');
        const match = href.match(/\/node\/(\d+)\//);
        if (match) {
          const nodeId = match[1];
          // Get the canonical listing URL
          await page.goto(`https://www.scrum.org/node/${nodeId}`);
          await page.waitForLoadState('networkidle');
          const listingUrl = page.url();
          const dateText = (await cells[0].textContent()).trim();
          found.push({ nodeId, title: titleText, trainer: trainerText.slice(0, 30), date: dateText, listingUrl });
          await page.goto(url); // go back to table
          await page.waitForLoadState('networkidle');
        }
      }
    }

    // Check if there's a next page
    const nextLink = await page.$('a[rel="next"], li.next a, .pager__item--next a');
    if (!nextLink) break;
    pageNum++;
  }

  console.log(`Found ${found.length} AI Essentials courses for Alex Brown:`);
  found.forEach(f => console.log(JSON.stringify(f)));
  await browser.close();
}

main().catch(e => { console.error(e.message); process.exit(1); });

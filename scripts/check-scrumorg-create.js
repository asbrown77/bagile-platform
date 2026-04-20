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
    const req = https.request(opts, r => { r.on('data', x => d += x); r.on('end', () => res(JSON.parse(d))); });
    req.on('error', rej);
    req.end();
  });
  const cookies = JSON.parse(cfg.scrumorg_session_cookies);

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  await context.addCookies(cookies);
  const page = await context.newPage();
  page.setDefaultTimeout(20000);

  // 1. Check /node/add
  await page.goto('https://www.scrum.org/node/add');
  console.log('node/add URL:', page.url());
  const nodeAddBody = await page.textContent('body');
  console.log('node/add (first 600):', nodeAddBody.replace(/\s+/g, ' ').slice(0, 600));

  // 2. Check course management for add/create links
  await page.goto('https://www.scrum.org/admin/courses/manage');
  await page.waitForLoadState('networkidle');
  const links = await page.$$eval('a', els =>
    els
      .map(e => ({ text: e.textContent.trim(), href: e.href }))
      .filter(e => e.text && /add|create|new course/i.test(e.text))
  );
  console.log('Add/create links on manage page:', JSON.stringify(links.slice(0, 10), null, 2));

  // 3. Check operations on the Chris PSPOAI course (Dec 2025) if visible
  const rows = await page.$$('table tbody tr');
  for (const row of rows) {
    const text = await row.textContent();
    if (text.includes('AI Essentials')) {
      const ops = await row.$eval('td:last-child', td => td.innerHTML);
      console.log('AI Essentials row ops:', ops.slice(0, 300));
    }
  }

  await browser.close();
}

main().catch(e => { console.error(e.message); process.exit(1); });

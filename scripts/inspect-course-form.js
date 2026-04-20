/**
 * Inspect the scrum.org "Create Course" form fields.
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
  page.setDefaultTimeout(20000);

  await page.goto('https://www.scrum.org/node/add/course');
  await page.waitForLoadState('networkidle');
  console.log('Form URL:', page.url());

  // List all inputs, selects, textareas
  const fields = await page.$$eval(
    'input, select, textarea',
    els => els.map(el => ({
      tag: el.tagName,
      id: el.id,
      name: el.name || el.getAttribute('name'),
      type: el.type,
      value: el.tagName === 'SELECT'
        ? Array.from(el.options).map(o => `${o.value}=${o.text}`).join(' | ')
        : (el.value || '').slice(0, 80),
      placeholder: el.placeholder || '',
    })).filter(f => f.id || f.name)
  );

  console.log('Form fields:');
  fields.forEach(f => console.log(JSON.stringify(f)));

  // Take a screenshot
  const path = require('path');
  const os = require('os');
  const ss = path.join(os.tmpdir(), 'scrumorg-create-form.png');
  await page.screenshot({ path: ss, fullPage: true });
  console.log('Screenshot:', ss);

  await browser.close();
}

main().catch(e => { console.error(e.message); process.exit(1); });

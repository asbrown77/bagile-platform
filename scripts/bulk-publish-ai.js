/**
 * Bulk publish 13 PSMAI/PSPOAI courses to Scrum.org using stored session cookies.
 */
const { execSync } = require('child_process');
const https = require('https');
const path = require('path');
const fs = require('fs');
const os = require('os');

const API_KEY = "Drqji6wXpwXbyp5IDcAq/V2IY+/l21Z/";
const SCRIPT = path.join(__dirname, 'publish-scrumorg.js');

// Template node IDs copied from Chris's existing AI courses:
// PSMAI  → node 105242 (https://www.scrum.org/courses/professional-scrum-master-ai-essentials-2026-04-30-105242)
// PSPOAI → node 105229 (https://www.scrum.org/courses/professional-scrum-product-owner-ai-essentials-2026-04-22-105229)
const TEMPLATE = { PSMAI: '105242', PSPOAI: '105229' };

const courses = [
  {id:9,  courseType:'PSMAI',  startDate:'2026-08-05', endDate:'2026-08-05', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13017'},
  {id:11, courseType:'PSMAI',  startDate:'2026-08-12', endDate:'2026-08-12', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13019'},
  {id:17, courseType:'PSPOAI', startDate:'2026-08-26', endDate:'2026-08-26', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13025'},
  {id:20, courseType:'PSMAI',  startDate:'2026-09-03', endDate:'2026-09-03', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13028'},
  {id:22, courseType:'PSPOAI', startDate:'2026-09-09', endDate:'2026-09-09', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13030'},
  {id:28, courseType:'PSPOAI', startDate:'2026-09-23', endDate:'2026-09-23', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13036'},
  {id:31, courseType:'PSMAI',  startDate:'2026-09-30', endDate:'2026-09-30', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13039'},
  {id:39, courseType:'PSMAI',  startDate:'2026-10-21', endDate:'2026-10-21', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13047'},
  {id:42, courseType:'PSPOAI', startDate:'2026-10-28', endDate:'2026-10-28', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13050'},
  {id:45, courseType:'PSMAI',  startDate:'2026-11-04', endDate:'2026-11-04', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13053'},
  {id:54, courseType:'PSPOAI', startDate:'2026-11-25', endDate:'2026-11-25', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13062'},
  {id:57, courseType:'PSMAI',  startDate:'2026-12-02', endDate:'2026-12-02', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13065'},
  {id:62, courseType:'PSPOAI', startDate:'2026-12-16', endDate:'2026-12-16', regUrl:'https://www.bagile.co.uk/?post_type=product&p=13070'},
];

function request(method, url, body, headers) {
  return new Promise((resolve, reject) => {
    const parsed = new URL(url);
    const bodyStr = body ? JSON.stringify(body) : undefined;
    const opts = {
      hostname: parsed.hostname,
      port: parsed.port || 443,
      path: parsed.pathname + parsed.search,
      method,
      headers: { ...headers, ...(bodyStr ? { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(bodyStr) } : {}) },
      rejectUnauthorized: false,
    };
    const req = https.request(opts, res => {
      let data = '';
      res.on('data', d => data += d);
      res.on('end', () => resolve({ status: res.statusCode, body: data }));
    });
    req.on('error', reject);
    if (bodyStr) req.write(bodyStr);
    req.end();
  });
}

async function main() {
  // Fetch session cookies from service config
  const cfgRes = await request('GET', 'https://api.bagile.co.uk/api/admin/service-config', null, { 'X-Api-Key': API_KEY });
  const cfg = JSON.parse(cfgRes.body);
  const cookies = JSON.parse(cfg['scrumorg_session_cookies'] || '[]');
  if (!cookies.length) throw new Error('No session cookies in service_config');
  console.log(`Using ${cookies.length} session cookies`);

  for (const c of courses) {
    process.stdout.write(`ID${c.id} ${c.courseType} ${c.startDate}... `);
    try {
      const args = {
        courseType: c.courseType,
        startDate: c.startDate,
        endDate: c.endDate,
        trainerName: 'Alex Brown',
        registrationUrl: c.regUrl,
        username: 'alexbrown@bagile.co.uk',
        password: '',
        cookies,
        templateNodeId: TEMPLATE[c.courseType],
      };
      // Write args to a temp file to avoid shell escaping issues with cookies JSON
      const tmpFile = path.join(os.tmpdir(), `scrumorg-args-${c.id}.json`);
      fs.writeFileSync(tmpFile, JSON.stringify(args), 'utf-8');
      try {
        var result = execSync(
          `node "${SCRIPT}" @${tmpFile}`,
          { encoding: 'utf-8', timeout: 120000, stdio: ['pipe', 'pipe', 'pipe'] }
        );
      } finally {
        fs.unlinkSync(tmpFile);
      }
      const out = JSON.parse(result.trim());
      const listingUrl = out.listingUrl;

      // Record publication in the API (externalUrl = pre-created listing, skip automation)
      const pubRes = await request(
        'POST',
        `https://api.bagile.co.uk/api/planned-courses/${c.id}/publish/scrumorg`,
        { externalUrl: listingUrl },
        { 'X-Api-Key': API_KEY }
      );
      if (pubRes.status === 200 || pubRes.status === 201) {
        console.log(`OK -> ${listingUrl}`);
      } else {
        console.log(`scrumorg-ok but record failed (HTTP ${pubRes.status}): ${pubRes.body.slice(0, 100)}`);
      }
    } catch (err) {
      const msg = err.stderr ? err.stderr.toString().slice(-600) : err.message.slice(0, 600);
      console.log(`FAILED: ${msg}`);
    }
  }
  console.log('Done');
}

main().catch(e => { console.error(e.message); process.exit(1); });

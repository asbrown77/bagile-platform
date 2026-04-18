import { chromium as chromiumExtra } from 'playwright-extra';
import StealthPlugin from 'puppeteer-extra-plugin-stealth';
import { fileURLToPath, pathToFileURL } from 'url';
import { dirname, resolve as resolvePath } from 'path';
import type {
  IPlaywrightRunnerPort,
  PlaywrightRunOptions,
  PlaywrightRunResult,
} from '../../../domain/ports/IPlaywrightRunnerPort.js';

// Stealth plugin patches navigator.webdriver, plugins, languages, WebGL vendor, etc.
// so Cloudflare's JS challenge cannot distinguish headless Chrome from a real browser.
chromiumExtra.use(StealthPlugin());

// Absolute path to the scripts directory, resolved relative to THIS file's location.
// Using import.meta.url avoids any ambiguity when this module is loaded dynamically
// or cached across process restarts.
const __dirname = dirname(fileURLToPath(import.meta.url));
const SCRIPTS_DIR = resolvePath(__dirname, '../../../scripts');

export class PlaywrightRunner implements IPlaywrightRunnerPort {
  async run(options: PlaywrightRunOptions): Promise<PlaywrightRunResult> {
    const start = Date.now();
    const { scriptName, input, headless = true } = options;

    // Use system Chromium if PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH is set (e.g. in Docker).
    // Also pass --no-sandbox which is required when running as root in a container.
    const executablePath = process.env['PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH'] || undefined;
    // --disable-blink-features=AutomationControlled removes navigator.webdriver=true,
    // which Cloudflare and similar WAFs use to fingerprint headless/automated browsers.
    // The stealth plugin handles deeper fingerprinting; this arg is a belt-and-suspenders addition.
    const launchArgs = [
      '--disable-blink-features=AutomationControlled',
      ...(executablePath ? ['--no-sandbox', '--disable-setuid-sandbox'] : []),
    ];

    let browser: Awaited<ReturnType<typeof chromiumExtra.launch>> | undefined;

    try {
      browser = await chromiumExtra.launch({ headless, executablePath, args: launchArgs });
      // Realistic browser context so user-agent and viewport don't reveal automation.
      // The stealth plugin already patches many fingerprints at launch time; these context
      // options provide an additional realistic layer.
      const context = await browser.newContext({
        userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36',
        viewport: { width: 1280, height: 800 },
        locale: 'en-GB',
      });
      const page = await context.newPage();
      const scriptPath = pathToFileURL(
        resolvePath(SCRIPTS_DIR, scriptName, `${scriptName}.script.js`)
      ).href;
      const scriptModule = await import(scriptPath);
      const runFn = scriptModule[toCamelCase(scriptName)];

      const resolve = options.credentialResolver;
      const resolveOrEnv = async (key: string, envKey: string): Promise<string> => {
        if (resolve) {
          const val = await resolve(key);
          if (val !== undefined) return val;
        }
        return process.env[envKey] ?? '';
      };

      const wpAdminUrl = process.env['WP_ADMIN_URL'] ?? 'https://www.bagile.co.uk/wp-admin';
      const wpUsername = await resolveOrEnv('wp_username', 'WP_USERNAME');
      const wpPassword = await resolveOrEnv('wp_app_password', 'WP_APP_PASSWORD');
      const scrumorgUsername = await resolveOrEnv('scrumorg_username', 'SCRUMORG_USERNAME');
      const scrumorgPassword = await resolveOrEnv('scrumorg_password', 'SCRUMORG_PASSWORD');

      const output = await runFn(page, {
        ...input,
        wpAdminUrl,
        wpUsername,
        wpPassword,
        scrumorgUsername,
        scrumorgPassword,
        headless,
      });

      return {
        success: output.success,
        output: output as unknown as Record<string, unknown>,
        errorMessage: output.errorMessage,
        screenshotPath: output.screenshotPath,
        durationMs: Date.now() - start,
      };
    } catch (err) {
      return {
        success: false,
        errorMessage: (err as Error).message,
        durationMs: Date.now() - start,
      };
    } finally {
      await browser?.close();
    }
  }
}

function toCamelCase(scriptName: string): string {
  return (
    'run' +
    scriptName
      .split('-')
      .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
      .join('')
  );
}

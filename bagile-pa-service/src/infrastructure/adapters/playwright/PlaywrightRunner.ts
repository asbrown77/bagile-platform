import { chromium } from 'playwright';
import type {
  IPlaywrightRunnerPort,
  PlaywrightRunOptions,
  PlaywrightRunResult,
} from '../../../domain/ports/IPlaywrightRunnerPort.js';

export class PlaywrightRunner implements IPlaywrightRunnerPort {
  async run(options: PlaywrightRunOptions): Promise<PlaywrightRunResult> {
    const start = Date.now();
    const { scriptName, input, headless = true } = options;

    const browser = await chromium.launch({ headless });
    const page = await browser.newPage();

    try {
      const scriptModule = await import(
        `../../../scripts/${scriptName}/${scriptName}.script.js`
      );
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
      await browser.close();
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

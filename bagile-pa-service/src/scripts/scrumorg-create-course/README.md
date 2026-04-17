# scrumorg-create-course

Playwright script that automates creating a new course listing on scrum.org by copying the most recent course of the same type for the same trainer, then updating dates and registration URL.

## Steps

1. Log in at `https://www.scrum.org/user/login`
2. Navigate to `https://www.scrum.org/admin/courses/manage`
3. Find the most recent course row matching `courseType` (e.g. PSM) AND `trainerName` (e.g. Chris Bexon)
4. Click the Copy action on that row — waits for the edit form to load
5. Clear and fill: start date, end date, registration URL
6. Click Save — waits for page settle, returns new course URL

## Inputs (injected by PlaywrightRunner)

| Field | Source |
|-------|--------|
| `scrumorgUsername` | `SCRUMORG_USERNAME` env var |
| `scrumorgPassword` | `SCRUMORG_PASSWORD` env var |
| `courseType` | MCP tool argument (e.g. PSM, PSPO) |
| `trainerName` | MCP tool argument (e.g. Chris Bexon) |
| `startDate` | MCP tool argument (YYYY-MM-DD) |
| `endDate` | MCP tool argument (YYYY-MM-DD) |
| `registrationUrl` | MCP tool argument (WooCommerce product URL) |

## Selector status

**UNVERIFIED — Last verified: UNVERIFIED against live scrum.org**

All selectors are best-guess based on common Drupal/scrum.org patterns. They must be verified against live scrum.org before production use. Look for `// TODO: verify selector against live scrum.org` comments in the script.

Key selectors to verify:

| Selector | Purpose |
|----------|---------|
| `input[name="name"]` | Username field on login page |
| `input[name="pass"]` | Password field on login page |
| `input[type="submit"]` | Login submit button |
| `table tbody tr` | Course list rows |
| `a:has-text("Copy")` | Copy link per course row |
| `input[name="start_date"]` | Start date field on edit form |
| `input[name="end_date"]` | End date field on edit form |
| `input[name="field_registration_url[und][0][url]"]` | Registration URL field |
| `input[type="submit"][value="Save"]` | Save button |

## Error handling

On any step failure, the script screenshots to `screenshots/scrumorg-{timestamp}.png` and returns `{ success: false, errorMessage, screenshotPath }`. It never throws.

## Health check

`scrumorg-create-course.health.ts` exports `checkScrumOrgHealth(username, password)` which verifies login page reachability, authentication, and course management page access without creating any data.

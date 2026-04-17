# FooEvents Transfer Script

Automates ticket transfers in wp-admin: cancels the old ticket, creates a new one, and resends it to the attendee.

Last verified: 2026-04-16

---

## Steps performed

1. **Login** — navigates to `/wp-login.php`, fills credentials, waits for wp-admin redirect
2. **Cancel old ticket** — opens the old ticket post editor, writes a transfer note into the Designation field, sets all Ticket Status dropdowns to "Canceled", publishes
3. **Create new ticket** — opens the new ticket form, selects the event and purchaser via jQuery, fills attendee fields, publishes
4. **Resend** — clicks the Resend button on the new ticket page to email the attendee
5. **Return** — parses the new ticket post ID from the URL and returns it

---

## Selectors relied on

| Selector | Purpose |
|---|---|
| `#user_login` | WP login username field |
| `#user_pass` | WP login password field |
| `#wp-submit` | WP login submit button |
| `#WooCommerceEventsEvent` | Select2 event dropdown (new ticket form) |
| `#WooCommerceEventsClientID` | Select2 purchaser dropdown (new ticket form) |
| `#WooCommerceEventsAttendeeName` | First Name field |
| `#WooCommerceEventsAttendeeName1` | Last Name field |
| `#WooCommerceEventsAttendeeEmail` | Email field |
| `#WooCommerceEventsAttendeeName2` | Designation field (used on both old cancel and new ticket) |
| `#WooCommerceEventsAttendeeName3` | Company field |
| `select[name*="WooCommerceEventsTicketStatus"]` | Status dropdown(s) — matches multiple for multi-day events |
| `#publish` | WordPress publish/update button |
| `#WooCommerceEventsResendTicket` | Resend ticket button |
| `.notice-success, .updated` | WP success notice (waited on after resend) |

---

## Select2 jQuery approach

The Event and Purchaser dropdowns use jQuery Select2. Playwright's native `fill()` and `selectOption()` interact with the underlying `<select>` element, but Select2 intercepts DOM events and does not respond to synthetic Playwright input.

The fix is to set the value directly on the underlying `<select>` via jQuery and trigger a `change` event, which Select2 listens to:

```typescript
await page.evaluate(
  (id) => { (window as any).jQuery('#WooCommerceEventsEvent').val(String(id)).trigger('change'); },
  productId
);
```

This bypasses the Select2 UI but produces the same result as selecting from the dropdown.

---

## Known fragile points

- **Selector stability** — FooEvents field IDs (`WooCommerceEventsAttendeeName`, `WooCommerceEventsAttendeeName1`, etc.) are numbered sequentially and could change on plugin update. Verify after any FooEvents upgrade.
- **Multi-day events** — The cancel step uses `locator('select[name*="WooCommerceEventsTicketStatus"]')` and sets all matches to "Canceled". If a single-day event has a different structure, this still works. If FooEvents changes the name attribute pattern, the selector will miss.
- **Publish URL pattern** — After cancelling, the script waits for `updated=1` in the URL. After creating a new ticket, it waits for `post=\d+.*action=edit`. If WP changes these redirect patterns, the waits will time out.
- **Resend success indicator** — Waits for `.notice-success` or `.updated`. Some WP theme customisations hide these elements. If resend appears to hang, check whether the success notice is rendered in the DOM at all.
- **jQuery availability** — `page.evaluate` accesses `window.jQuery`. If a WP update or plugin conflict removes jQuery from the global scope, the Select2 injection will fail with a runtime error.
- **Purchaser user ID** — Hardcoded to `3` (Alex Brown admin). If the WP user ID changes (e.g. on a site migration), this must be updated.

---

## Error handling

Any step failure captures a screenshot to `screenshots/fooevent-transfer-{timestamp}.png` relative to the working directory, then returns `{ success: false, errorMessage, screenshotPath }`. The browser is always closed in the `finally` block of the runner.

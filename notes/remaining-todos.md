# Remaining TODOs

This checklist compares the current implementation against the product spec and our build decisions so far.

## Completed

- Auth with signup, login, refresh, logout, and forgot/reset password
- Protected app shell with responsive navigation
- Accounts, categories management UI, transactions CRUD, and transfer-safe balances
- Dashboard summary plus polished visual hierarchy
- Budgets and reports basics
- Goals and recurring transactions
- CSV exports
- In-app notifications, notification history page, and recurring automation loop
- Settings with profile, security, preferences, notifications, financial defaults, and theme selection
- Theme swatches and cleaner settings workspace navigation
- Scheduler visibility on the recurring page
- Containerized Podman-first local deployment artifacts
- Forgot-password and settings/theme tests

## Still Missing Or Incomplete

### Product / UX

- Final dark-theme pass on a few edge-case screens after the latest settings/dashboard/notifications polish
- Optional richer notification actions or archived notification grouping
- Optional deeper recurring automation controls such as retry count visibility or manual rerun history

### Testing

- Notification page coverage
- Category management UI coverage
- Email-enabled forgot-password path coverage
- Automation status/controller coverage

### Project Hygiene

- Sync the long milestone notes file with the newest settings, notifications, scheduler-status, and Podman updates
- Clean tracked generated artifacts already present in git history (`bin`, `obj`, `tsbuildinfo`) without disturbing source work
- Group and commit the current feature work cleanly

## Out Of Scope And Still Intentionally Missing

- Bank sync
- Investment tracking
- Tax filing support
- AI advice
- Shared household permissions
- Receipt scanning
- Mobile app

## Recommended Next Order

1. Add notification and category UI tests
2. Add automation status and forgot-password email-enabled backend coverage
3. Sync milestone notes and commit the current polish/hardening work
4. Do one final dark-theme consistency pass
5. Then move to optional growth items such as PDF export, richer reports, account detail screens, or data import

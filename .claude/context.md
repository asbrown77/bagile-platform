# BAgile Platform — Context

## Server Access
- **Staging:** ssh -i ~/.ssh/n8n-key root@142.132.227.7
- **Production:** ssh -i ~/.ssh/n8n-key root@5.75.167.51
- **BAgile app:** /opt/bagile/ (staging only)
- **Config:** /opt/bagile/config/api.env, etl.env, bagile.env
- **Docker:** docker-compose.yml at /opt/bagile/

## Credentials
- Local env file: C:\Users\alexa\OneDrive\Companies\b-agile\agent\.credentials\.env
- Xero tokens: C:\Users\alexa\OneDrive\Companies\b-agile\agent\.credentials\xero_tokens.json
- All API keys stored there (Trello, WooCommerce, MailChimp, n8n, Stripe x2, Xero, Scrum.org)

## Production Database
- Host: bagile-postgres (internal Docker network)
- External access: localhost:55432 via pg-proxy on staging server
- Database: bagile_prod
- User: bagile_admin
- Schema: bagile

## API
- URL: https://api.bagile.co.uk
- Key header: X-Api-Key
- Key: read from .credentials/.env (BAGILE_API_KEY)
- Swagger: https://api.bagile.co.uk/swagger

## Key Business Rules
- "Sold out" on website = cancelled course, not actually full
- QA partner rate: PTN33 (33%) since Sep 2025
- Course minimum: 3 (standard), 4 (interactive: PSM-A, PSFS, APS, APS-SD)
- Trainers: Alex Brown (AB), Chris Bexon (CB). Paul Ralton left.
- FooEvents transfers require order "completed" (paid) before ticket operations

## Wiki Export
- Full Coda wiki exported to: C:\Users\alexa\OneDrive\Companies\b-agile\agent\b-agile-wiki-export.pdf

## Memory
- Session notes and all business context in: C:\Users\alexa\.claude\projects\C--Users-alexa\memory\

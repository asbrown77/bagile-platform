param(
    [string]$DbPassword = "Dilbert01`$db"
)

$DbUrl = "jdbc:postgresql://bagile-db1.postgres.database.azure.com:5432/bagile_prod"
$DbUser = "bagile_admin"

Write-Host ""
Write-Host "🚀 Deploying database migrations to PRODUCTION (bagile_prod)" -ForegroundColor Yellow
$confirm = Read-Host "Continue? Type 'DEPLOY' to proceed"

if ($confirm -ne "DEPLOY") {
    Write-Host "Cancelled. No changes applied." -ForegroundColor Green
    exit 0
}

Write-Host "Applying migrations..." -ForegroundColor Yellow

# Run migrations only (no clean!)
docker run --rm `
  -v ${PWD}/../migrations:/flyway/sql `
  flyway/flyway:latest `
  -url="$DbUrl" `
  -user="$DbUser" `
  -password="$DbPassword" `
  -schemas=bagile `
  -locations=filesystem:/flyway/sql `
  -baselineOnMigrate=true `
  -validateOnMigrate=true `
  migrate
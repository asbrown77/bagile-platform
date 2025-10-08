param(
    [string]$DbPassword = "Dilbert01`$db"
)

$DbUrl = "jdbc:postgresql://bagile-db1.postgres.database.azure.com:5432/bagile_prod"
$DbUser = "bagile_admin"

Write-Host ""
Write-Host "⚠️  WARNING: You are about to DROP and RECREATE the entire 'bagile' schema in bagile_prod." -ForegroundColor Yellow
Write-Host "All tables, data, and schema history will be permanently deleted." -ForegroundColor Red
Write-Host ""
$confirm = Read-Host "Type 'NUKE' (all caps) to confirm you really want to do this"

if ($confirm -ne "NUKE") {
    Write-Host "Aborted. No changes made." -ForegroundColor Green
    exit 0
}

Write-Host "Proceeding with full reset..." -ForegroundColor Red
Start-Sleep -Seconds 2


# Drop everything and rebuild clean
docker run --rm `
  -v ${PWD}/../migrations:/flyway/sql `
  flyway/flyway:latest `
  -url="$DbUrl" `
  -user="$DbUser" `
  -password="$DbPassword" `
  -schemas=bagile `
  -cleanDisabled=false `
  clean

# Optional sanity pause
Start-Sleep -Seconds 2

# Re-migrate from scratch
docker run --rm `
  -v ${PWD}/migrations:/flyway/sql `
  flyway/flyway:latest `
  -url="$DbUrl" `
  -user="$DbUser" `
  -password="$DbPassword" `
  -schemas=bagile `
  migrate
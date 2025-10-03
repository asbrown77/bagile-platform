param(
    [string]$DbPassword = "Dilbert01`$db"
)

$DbUrl = "jdbc:postgresql://bagile-db1.postgres.database.azure.com:5432/bagile_prod"
$DbUser = "bagile_admin"

docker run --rm `
  -v ${PWD}/migrations:/flyway/sql `
  flyway/flyway:latest `
  -url="$DbUrl" `
  -user="$DbUser" `
  -password="$DbPassword" `
  migrate

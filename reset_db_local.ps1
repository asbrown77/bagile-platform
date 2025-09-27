param(
    [string]$DbHost = "localhost",
    [string]$Port = "5432",
    [string]$User = "postgres",
    [string]$Password = "postgres",
    [string]$DbName = "bagile"
)

# Set password for psql
$env:PGPASSWORD = $Password

Write-Host "Dropping database $DbName if exists..."
psql -h $DbHost -p $Port -U $User -d postgres -c "DROP DATABASE IF EXISTS $DbName;"

Write-Host "Creating database $DbName..."
psql -h $DbHost -p $Port -U $User -d postgres -c "CREATE DATABASE $DbName;"

Write-Host "Applying migrations..."
Get-ChildItem -Path "db/migrations" -Filter *.sql | Sort-Object Name | ForEach-Object {
    if ($_.Name -ne "999_drop_all.sql") {
        Write-Host "Running $($_.Name)..."
        psql -h $DbHost -p $Port -U $User -d $DbName -f $_.FullName
    }
}


Write-Host "Database reset complete."

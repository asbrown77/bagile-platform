param(
    [string]$Host = "localhost",
    [string]$Port = "5432",
    [string]$User = "postgres",
    [string]$Password = "postgres",
    [string]$DbName = "bagile"
)

# Set password for psql
$env:PGPASSWORD = $Password

Write-Host "Dropping database $DbName if exists..."
psql -h $Host -p $Port -U $User -d postgres -c "DROP DATABASE IF EXISTS $DbName;"

Write-Host "Creating database $DbName..."
psql -h $Host -p $Port -U $User -d postgres -c "CREATE DATABASE $DbName;"

Write-Host "Applying migrations..."
Get-ChildItem -Path "db/migrations" -Filter *.sql | Sort-Object Name | ForEach-Object {
    Write-Host "Running $($_.Name)..."
    psql -h $Host -p $Port -U $User -d $DbName -f $_.FullName
}

Write-Host "Database reset complete."

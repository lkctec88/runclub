$ErrorActionPreference = 'Stop'

Write-Host 'Reading Azure Postgres connection from Key Vault (secret value is not printed)...'
$cs = az keyvault secret show --vault-name kvrunclubk8p3jaxx7d --name postgres-connection --query value -o tsv
if ([string]::IsNullOrWhiteSpace($cs)) {
  throw 'Could not read secret postgres-connection. Sign in with az login as mollypepperpot@hotmail.com and confirm Key Vault secret access.'
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Seed__Enabled = 'false'
$env:Database__ApplyMigrations = 'false'
$env:ConnectionStrings__Default = $cs

Write-Host 'API will use Azure Postgres. Seed and EF migrations are off.'
Write-Host 'If the connection times out, add this PC public IP on the Flexible Server firewall.'

try {
  $repo = Split-Path -Parent $PSScriptRoot
  Set-Location $repo
  dotnet run --project api/RunClub.Api --launch-profile http
}
finally {
  Remove-Item Env:ConnectionStrings__Default -ErrorAction SilentlyContinue
}

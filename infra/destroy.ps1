# Apaga tudo o que o deploy.ps1 criou no Azure, inclusive o banco e os dados.
#
#   .\infra\destroy.ps1
param(
    [string]$ResourceGroup = "url-shortener"
)

$ErrorActionPreference = "Stop"

Write-Host "Deleting resource group '$ResourceGroup' (database included)..." -ForegroundColor Yellow
az group delete --name $ResourceGroup --yes
if ($LASTEXITCODE -ne 0) { throw "Failed to delete resource group." }
Write-Host "Deleted." -ForegroundColor Green

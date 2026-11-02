# Deploy completo no Azure: infraestrutura + API com o front + worker.
# Pré-requisitos: Azure CLI logada (az login), .NET 10 SDK, Node.js e uv.
#
#   .\infra\deploy.ps1                              # brazilsouth
#   .\infra\deploy.ps1 -Location eastus2
param(
    [string]$Location = "brazilsouth",
    [string]$ResourceGroup = "url-shortener"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$build = "$PSScriptRoot/.build"

function Invoke-Checked {
    param([scriptblock]$Command)
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): $Command" }
}

# No PowerShell 5, tanto o Compress-Archive quanto o ZipFile.CreateFromDirectory gravam caminhos
# com "\", que viram nomes de arquivo estranhos no Linux do Azure. Por isso as entradas são criadas à mão.
function New-Zip {
    param([string]$Source, [string]$Destination)
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $Source = (Resolve-Path $Source).Path
    if (Test-Path $Destination) { Remove-Item $Destination }
    $zip = [IO.Compression.ZipFile]::Open($Destination, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -Recurse -File $Source) {
            $entry = $file.FullName.Substring($Source.Length + 1).Replace("\", "/")
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $entry) | Out-Null
        }
    } finally {
        $zip.Dispose()
    }
}

Write-Host "==> Subscription" -ForegroundColor Cyan
Invoke-Checked { az account show --query "{name:name, id:id}" --output table }

# Assinaturas novas vêm com poucos provedores registrados. Se já estiver registrado, é instantâneo.
Write-Host "==> Resource providers" -ForegroundColor Cyan
foreach ($provider in "Microsoft.Web", "Microsoft.DBforPostgreSQL", "Microsoft.Storage") {
    Invoke-Checked { az provider register --namespace $provider --wait }
}

Write-Host "==> Resource group" -ForegroundColor Cyan
Invoke-Checked { az group create --name $ResourceGroup --location $Location --output none }

# Senha nova a cada deploy: o mesmo deploy atualiza o banco, a API e o worker juntos.
# Só letras e números, para poder ir direto na URL de conexão do worker.
$chars = [char[]]"abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789"
$password = "Aa1" + (-join (1..29 | ForEach-Object { $chars | Get-Random }))

Write-Host "==> Infrastructure (the first run creates the database and takes ~10 minutes)" -ForegroundColor Cyan
$outputs = az deployment group create `
    --resource-group $ResourceGroup `
    --template-file "$PSScriptRoot/main.bicep" `
    --parameters "dbAdminPassword=$password" `
    --query properties.outputs `
    --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Infrastructure deployment failed." }
$webApp = $outputs.webAppName.value
$functionApp = $outputs.functionAppName.value

if (Test-Path $build) { Remove-Item -Recurse -Force $build }
New-Item -ItemType Directory $build | Out-Null

Write-Host "==> Front end" -ForegroundColor Cyan
Push-Location "$root/web"
Invoke-Checked { npm ci --silent }
Invoke-Checked { npm run build --silent }
Pop-Location

Write-Host "==> API" -ForegroundColor Cyan
Invoke-Checked { dotnet publish "$root/src/UrlShortener.Api" --configuration Release --output "$build/api" }
# O front vai junto, em wwwroot: a API serve os arquivos estáticos (ver Program.cs).
Copy-Item -Recurse "$root/web/dist" "$build/api/wwwroot"
New-Zip "$build/api" "$build/api.zip"
Invoke-Checked { az webapp deploy --resource-group $ResourceGroup --name $webApp --src-path "$build/api.zip" --type zip --output none }

Write-Host "==> Worker" -ForegroundColor Cyan
New-Item -ItemType Directory "$build/worker" | Out-Null
Copy-Item -Recurse "$root/worker/src/stats_worker" "$build/worker/stats_worker"
Copy-Item "$root/worker/src/function_app.py", "$root/worker/src/host.json" "$build/worker"
Push-Location "$root/worker"
Invoke-Checked { uv export --no-dev --no-hashes --no-emit-project --output-file "$build/worker/requirements.txt" --quiet }
Pop-Location
# azure-functions só existe no Azure, não é dependência do worker local.
Add-Content "$build/worker/requirements.txt" "azure-functions"
Get-ChildItem -Recurse -Directory -Filter __pycache__ "$build/worker" | Remove-Item -Recurse -Force
New-Zip "$build/worker" "$build/worker.zip"
# O build remoto instala as dependências no Linux do Azure (o psycopg tem partes nativas).
Invoke-Checked { az functionapp deployment source config-zip --resource-group $ResourceGroup --name $functionApp --src "$build/worker.zip" --build-remote true --output none }

Write-Host ""
Write-Host "Deployed: $($outputs.siteUrl.value)" -ForegroundColor Green
Write-Host "The first request after a while takes a few seconds: the free plan puts the app to sleep." -ForegroundColor DarkGray

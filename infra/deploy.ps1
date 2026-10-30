# Deploy completo na AWS: infraestrutura + API + worker + front.
# Pré-requisitos: AWS CLI configurada (aws configure), SAM CLI, Docker, .NET 10 SDK, Node.js e uv.
#
#   .\infra\deploy.ps1                      # us-east-1
#   .\infra\deploy.ps1 -Region sa-east-1
param(
    [string]$Region = "us-east-1",
    [string]$StackName = "url-shortener"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$env:SAM_CLI_TELEMETRY = "0"

function Invoke-Checked {
    param([scriptblock]$Command)
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): $Command" }
}

Write-Host "==> Account" -ForegroundColor Cyan
Invoke-Checked { aws sts get-caller-identity --query Arn --output text }

# A VPC padrão já existe em toda conta e tem subnets em várias zonas de disponibilidade.
Write-Host "==> Default VPC" -ForegroundColor Cyan
$vpcId = aws ec2 describe-vpcs --region $Region --filters Name=is-default,Values=true --query "Vpcs[0].VpcId" --output text
if (-not $vpcId -or $vpcId -eq "None") { throw "No default VPC in $Region." }
$subnetIds = (aws ec2 describe-subnets --region $Region --filters Name=vpc-id,Values=$vpcId --query "Subnets[].SubnetId" --output text) -split "\s+" -join ","
Write-Host "$vpcId ($subnetIds)"

Write-Host "==> Worker requirements" -ForegroundColor Cyan
Push-Location "$root/worker"
Invoke-Checked { uv export --no-dev --no-hashes --no-emit-project --output-file src/requirements.txt --quiet }
Pop-Location

# --use-container: compila dentro de containers Linux iguais aos da Lambda.
# Necessário porque o psycopg tem partes nativas que precisam ser de Linux, não de Windows.
Write-Host "==> Build" -ForegroundColor Cyan
Invoke-Checked { sam build --template-file "$root/infra/template.yaml" --build-dir "$root/infra/.aws-sam/build" --use-container }

Write-Host "==> Deploy stack (the first run creates the database and takes ~10 minutes)" -ForegroundColor Cyan
Invoke-Checked {
    sam deploy `
        --template-file "$root/infra/.aws-sam/build/template.yaml" `
        --stack-name $StackName `
        --region $Region `
        --resolve-s3 `
        --capabilities CAPABILITY_IAM `
        --no-confirm-changeset `
        --no-fail-on-empty-changeset `
        --parameter-overrides "VpcId=$vpcId" "SubnetIds=$subnetIds"
}

$outputs = aws cloudformation describe-stacks --region $Region --stack-name $StackName --query "Stacks[0].Outputs" --output json | ConvertFrom-Json
$bucket = ($outputs | Where-Object OutputKey -eq "FrontBucketName").OutputValue
$distributionId = ($outputs | Where-Object OutputKey -eq "DistributionId").OutputValue
$siteUrl = ($outputs | Where-Object OutputKey -eq "SiteUrl").OutputValue

Write-Host "==> Front end" -ForegroundColor Cyan
Push-Location "$root/web"
Invoke-Checked { npm ci --silent }
Invoke-Checked { npm run build --silent }
Pop-Location
Invoke-Checked { aws s3 sync "$root/web/dist" "s3://$bucket" --delete --region $Region }
# Limpa o cache do CloudFront para servir a versão nova do front.
Invoke-Checked { aws cloudfront create-invalidation --distribution-id $distributionId --paths "/*" --query "Invalidation.Id" --output text }

Write-Host ""
Write-Host "Deployed: $siteUrl" -ForegroundColor Green

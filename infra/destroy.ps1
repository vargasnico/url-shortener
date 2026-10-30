# Apaga tudo o que o deploy criou, para parar a cobrança.
# O banco deixa um snapshot final (DeletionPolicy: Snapshot no template); apague-o no console
# do RDS se não quiser guardar os dados.
param(
    [string]$Region = "us-east-1",
    [string]$StackName = "url-shortener"
)

$ErrorActionPreference = "Stop"

# O CloudFormation não apaga bucket com arquivos: esvazia antes.
$bucket = aws cloudformation describe-stacks --region $Region --stack-name $StackName `
    --query "Stacks[0].Outputs[?OutputKey=='FrontBucketName'].OutputValue" --output text
if ($bucket -and $bucket -ne "None") {
    aws s3 rm "s3://$bucket" --recursive --region $Region
}

sam delete --stack-name $StackName --region $Region --no-prompts

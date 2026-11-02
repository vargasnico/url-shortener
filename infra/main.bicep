// URL shortener no Azure, pensado para a assinatura Azure for Students (sem cartão de crédito).
//
// Visão geral do tráfego:
//   navegador ─► App Service F1 (API .NET + front React em wwwroot) ─► PostgreSQL Flexible Server
//   timer (1/min) ─► Function App Flex Consumption (Python) ─────────────────┘
//
// Custos: App Service F1 e Functions Flex Consumption ficam dentro das cotas grátis.
// O PostgreSQL B1MS é o único recurso que consome crédito quando não está coberto pela oferta grátis.

@description('Região dos recursos. Assinaturas de estudante podem restringir as regiões permitidas.')
param location string = resourceGroup().location

@description('Usuário administrador do PostgreSQL.')
param dbAdminUser string = 'urlshortener'

@secure()
@description('Senha do administrador do PostgreSQL. O deploy.ps1 gera uma nova a cada execução.')
param dbAdminPassword string

// Sufixo estável por resource group: os nomes de App Service, PostgreSQL e Storage são globais.
var suffix = uniqueString(resourceGroup().id)
var dbName = 'urlshortener'
var deploymentContainer = 'function-deployments'

// ---------- Banco ----------
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: 'urlshortener-db-${suffix}'
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '17'
    administratorLogin: dbAdminUser
    administratorLoginPassword: dbAdminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: dbName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// 0.0.0.0 é a regra especial "permitir serviços do Azure": libera o App Service e a Function
// sem precisar de VNet (que não existe no plano F1). O acesso continua exigindo usuário e senha.
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgres
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

var dbHost = postgres.properties.fullyQualifiedDomainName

// ---------- API + front ----------
resource appPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'urlshortener-plan-${suffix}'
  location: location
  kind: 'linux'
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  properties: {
    reserved: true // Linux
  }
}

resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: 'urlshortener-${suffix}'
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appCommandLine: 'dotnet UrlShortener.Api.dll'
      alwaysOn: false // não disponível no F1: a app "dorme" depois de ~20 min sem acesso
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'Database__MigrateOnStartup', value: 'true' }
        {
          name: 'ConnectionStrings__Default'
          value: 'Host=${dbHost};Port=5432;Database=${dbName};Username=${dbAdminUser};Password=${dbAdminPassword};SSL Mode=Require;Maximum Pool Size=5'
        }
      ]
    }
  }
}

// ---------- Worker ----------
// A Function precisa de uma conta de armazenamento: estado do timer e pacote do deploy.
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'urlshort${suffix}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource deploymentBlobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: deploymentContainer
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource functionPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'urlshortener-worker-plan-${suffix}'
  location: location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: 'urlshortener-worker-${suffix}'
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: functionPlan.id
    httpsOnly: true
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentContainer}'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 40
        instanceMemoryMB: 512
      }
      runtime: {
        name: 'python'
        version: '3.13'
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'AzureWebJobsStorage', value: storageConnectionString }
        { name: 'DEPLOYMENT_STORAGE_CONNECTION_STRING', value: storageConnectionString }
        // Formato NCRONTAB com segundos: a cada minuto.
        { name: 'TIMER_SCHEDULE', value: '0 * * * * *' }
        {
          name: 'DATABASE_URL'
          value: 'postgresql://${dbAdminUser}:${dbAdminPassword}@${dbHost}:5432/${dbName}?sslmode=require'
        }
      ]
    }
  }
  dependsOn: [
    deploymentBlobContainer
  ]
}

output siteUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppName string = webApp.name
output functionAppName string = functionApp.name
output postgresHost string = dbHost

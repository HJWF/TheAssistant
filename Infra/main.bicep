metadata name = 'Bicep for the Assistant'
metadata owner = 'Wilrico Feenstra'

targetScope = 'subscription'

import * as types from './Common/types.bicep'
import * as names from './Common/nameConventionFunctions.bicep'

//MARK: Parameters

@description('Required. Logged in user details. Passed in from parent "deployNow.ps1" script.')
param deployedBy string

@description('Optional. Azure Region to deploy the resources in, default westeurope.')
@allowed([
  'westeurope'
  'northeurope'
])
param location string = 'westeurope'

@description('Required. The name of the project, this will be used as a unique name for the resources.')
param projectName string

@description('Optional. Location shortcode. Used for end of resource names.')
param locationShortCode string = 'weu'

@description('Add tags as required as Name:Value.')
param tags object = {
  Project: projectName
  DeployedOn: utcNow('u')
  DeployedBy: deployedBy
}

//MARK: Variables
var coreParameters = types.newCoreParams(locationShortCode, projectName)

var aiResourceGroupName = 'AI'
var appsResourceGroupName = 'Apps'
var messagingResourceGroupName = 'Messaging'
var aiFoundryName = names.resource('foundry', coreParameters)
var serviceBusName = names.resource('serviceBus', coreParameters)
var applicationInsightsName = names.resource('appi', coreParameters)
var logAnalyticsWorkspaceName = names.resource('law', coreParameters)
var keyVaultName = names.resource('kv', coreParameters)
var storageAccountName = names.storageAccountName('st', coreParameters)
var appServicePlanName = names.resource('asp', coreParameters)
var loginApiFunctionName = names.resourceWithContext('func','loginapi', coreParameters)
var apiFunctionName = names.resourceWithContext('func', 'api', coreParameters)
var managedIdentityName = names.resource('mi', coreParameters)

var ukSouthLocation = 'uksouth'
var wilricoObjectId = '7a00fd3f-3e99-42ac-aa7c-9081b437c4ca'
var functionContentShareName = 'function-content-share'

var appSettingKeyValuePairs = {
  WEBSITE_RUN_FROM_PACKAGE: '1'
  FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
  DOTNET_ISOLATION_VERSION: '8.0'
  FUNCTIONS_EXTENSION_VERSION: '~4'
  AzureWebJobsStorage__accountName: storageAccountName
  AzureWebJobsStorage__shareName: functionContentShareName
  KeyVaultName: keyVaultName
  ApplicationInsightsName: applicationInsightsName
  LogAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  ServiceBusNamespace: serviceBusName
  keyVaultUri: 'https://${keyVaultName}.vault.azure.net/'
  WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED: '1'
  WEBSITE_SKIP_CONTENTSHARE_VALIDATION: '1'
  WEBSITE_TIME_ZONE: 'Europe/Brussels'
  // 'AIFoundryEndpoint': aiFoundry.outputs.properties.endpoint
  // 'AIFoundryDeployment': 'gpt-4o-mini'
}

//MARK: Modules
module aiResourceGroup 'br/public:avm/res/resources/resource-group:0.4.1' = {
  name: 'create-${aiResourceGroupName}'
  params: {
    name: aiResourceGroupName
    location: ukSouthLocation
    tags: tags
  }
}

module appsResourceGroup 'br/public:avm/res/resources/resource-group:0.4.1' = {
  name: 'create-${appsResourceGroupName}'
  params: {
    name: appsResourceGroupName
    location: location
    tags: tags
  }
}

module messagingResourceGroup 'br/public:avm/res/resources/resource-group:0.4.1' = {
  name: 'create-${messagingResourceGroupName}'
  params: {
    name: messagingResourceGroupName
    location: location
    tags: tags
  }
}

//MARK: Managed Identity
module managedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.1' = {
  name: 'create-${managedIdentityName}'
  scope: resourceGroup(appsResourceGroupName)
  params: {
    name: managedIdentityName
    location: location
    tags: tags
  }
  dependsOn: [
    appsResourceGroup
  ]
}

// //MARK: AI Foundry
// module aiFoundry 'br/public:avm/res/cognitive-services/account:0.13.2' = {
//   name: 'create-${aiFoundryName}'
//   scope: resourceGroup(aiResourceGroupName)
//   params: {
//     name: aiFoundryName
//     location: ukSouthLocation
//     kind: 'AIServices'
//     managedIdentities: {
//       systemAssigned: true
//       userAssignedResourceIds: [
//         managedIdentity.outputs.resourceId
//       ]
//     }
//     sku: 'S0'
//     allowProjectManagement: true
//     tags: tags
//     deployments: [
//       {
//         model: {
//           format: 'OpenAI'
//           name: 'gpt-4o-mini'
//           version: '2024-07-18'
//         }
//         versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
//         raiPolicyName: 'Microsoft.DefaultV2'
//       }
//     ]
//     roleAssignments: [
//       {
//         principalId: wilricoObjectId
//         roleDefinitionIdOrName: 'Azure AI Account Owner'
//       }
//       {
//         principalId: wilricoObjectId
//         roleDefinitionIdOrName: 'Azure AI User'
//       }
//     ]
//   }
//   dependsOn: [
//     aiResourceGroup
//   ]
// }

// //MARK: AI Foundry Project
// module aiFoundryProject 'Modules/cognitiveServicesAccountProject.bicep' = {
//   name: 'create-${aiFoundryName}-TheAssistant'
//   scope: resourceGroup(aiResourceGroupName)
//   params: {
//     foundryName: aiFoundryName
//   }
//   dependsOn: [
//     aiFoundry
//   ]
// }

//MARK: Service Bus Namespace
module serviceBus 'br/public:avm/res/service-bus/namespace:0.15.0' = {
  name: 'create-${serviceBusName}'
  scope: resourceGroup(messagingResourceGroupName)
  params: {
    name: serviceBusName
    location: location
    skuObject: {
      name: 'Basic'
    }
    tags: tags
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
    managedIdentities: {
      systemAssigned: true
      userAssignedResourceIds: [
        managedIdentity.outputs.resourceId
      ]
    }
    queues: [
      { name: 'receivedmessages' }
    ]
    roleAssignments: [
      {
        principalId: wilricoObjectId
        roleDefinitionIdOrName: 'Azure Service Bus Data Owner'
      }
    ]
  }
  dependsOn: [
    messagingResourceGroup
  ]
}

//MARK: Log Analytics Workspace
module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.12.0' = {
  name: 'create-${logAnalyticsWorkspaceName}'
  scope: resourceGroup(appsResourceGroupName)
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    skuName: 'PerGB2018'
    dataRetention: 30
    tags: tags
  }
  dependsOn: [
    appsResourceGroup
  ]
}

//MARK: Application Insights
module applicationInsights 'br/public:avm/res/insights/component:0.6.0' = {
  name: 'create-${applicationInsightsName}'
  scope: resourceGroup(appsResourceGroupName)
  params: {
    name: applicationInsightsName
    location: location
    kind: 'web'
    applicationType: 'web'
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
    tags: tags
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    disableLocalAuth: true
    roleAssignments: [
      {
        principalId: wilricoObjectId
        roleDefinitionIdOrName: 'Monitoring Contributor'
      }
      {
        principalId: wilricoObjectId
        roleDefinitionIdOrName: '92aaf0da-9dab-42b6-94a3-d43ce8d16293'
      }
    ]
  }
  dependsOn: [
    appsResourceGroup
  ]
}

//MARK: Key Vault
module keyVault 'br/public:avm/res/key-vault/vault:0.12.0' = {
  name: 'create-${keyVaultName}'
  scope: resourceGroup(appsResourceGroupName)
  params: {
    name: keyVaultName
    location: location
    sku: 'standard'
    enablePurgeProtection: false
    enableSoftDelete: true
    enableRbacAuthorization: false
    enableVaultForDeployment: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
      ipRules: []
      virtualNetworkRules: []
    }
    accessPolicies: [
      {
        objectId: wilricoObjectId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        objectId: managedIdentity.outputs.principalId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
    ]
    tags: tags
  }
  dependsOn: [
    appsResourceGroup
  ]
}

//MARK: Storage Account
module storageAccount 'br/public:avm/res/storage/storage-account:0.26.0' = {
  name: 'create-${storageAccountName}'
  scope: resourceGroup(appsResourceGroupName)
  params: {
    name: storageAccountName
    location: location
    skuName: 'Standard_GZRS'
    kind: 'StorageV2'
    tags: tags
    publicNetworkAccess: 'Enabled'
    managedIdentities: {
      systemAssigned: true
      userAssignedResourceIds: [
        managedIdentity.outputs.resourceId
      ]
    }
    roleAssignments: [
      {
        principalId: wilricoObjectId
        roleDefinitionIdOrName: 'Storage Blob Data Owner'
      }
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
      }
    ]
    fileServices: {
      shares: [
        {
          name: functionContentShareName
        }
      ]
    }
  }
  dependsOn: [
    appsResourceGroup
  ]
}

//MARK: App Service Plan
module appServicePlan 'br/public:avm/res/web/serverfarm:0.5.0' = {
  name: 'create-${appServicePlanName}'
  scope: resourceGroup(appsResourceGroupName)
  params: {
    name: appServicePlanName
    location: location
    skuName: 'B1'
    kind: 'linux'
    tags: tags
    reserved: true
  }
  dependsOn: [
    appsResourceGroup
  ]
}

//MARK: Login API Function
module loginApiFunction 'br/public:avm/res/web/site:0.19.2' = {
  name: 'create-${loginApiFunctionName}'
  scope: resourceGroup(appsResourceGroupName)
  params: {
    name: loginApiFunctionName
    location: location
    kind: 'functionapp,linux'
    serverFarmResourceId: appServicePlan.outputs.resourceId
    keyVaultAccessIdentityResourceId: managedIdentity.outputs.resourceId
    configs: [
      {
        name: 'appsettings'
        applicationInsightResourceId: applicationInsights.outputs.resourceId
        storageAccountResourceId: storageAccount.outputs.resourceId
        properties: appSettingKeyValuePairs
      }
      {
        name: 'web'
        properties: {
          alwaysOn: true
          use32BitWorkerProcess: false
          linuxFxVersion: 'DOTNET-ISOLATED|8.0'
          minTlsVersion: '1.3'
        }
      }
    ]
    tags: tags
    managedIdentities: {
      systemAssigned: false
      userAssignedResourceIds: [
        managedIdentity.outputs.resourceId
      ]
    }
  }
  dependsOn: [
    appsResourceGroup
  ]
}

//MARK: API Function
module apiFunction 'br/public:avm/res/web/site:0.19.0' = {
  name: 'create-${apiFunctionName}'
  scope: resourceGroup(appsResourceGroupName)
  params: {
    name: apiFunctionName
    location: location
    kind: 'functionapp,linux'
    serverFarmResourceId: appServicePlan.outputs.resourceId
    keyVaultAccessIdentityResourceId: managedIdentity.outputs.resourceId
    configs: [
      {
        name: 'appsettings'
        applicationInsightResourceId: applicationInsights.outputs.resourceId
        storageAccountResourceId: storageAccount.outputs.resourceId
        properties: appSettingKeyValuePairs
      }
      {
        name: 'web'
        properties: {
          alwaysOn: true
          use32BitWorkerProcess: false
          linuxFxVersion: 'DOTNET-ISOLATED|8.0'
          minTlsVersion: '1.3'
        }
      }
    ]
    tags: tags
    managedIdentities: {
      systemAssigned: false
      userAssignedResourceIds: [
        managedIdentity.outputs.resourceId
      ]
    }
  }
  dependsOn: [
    appsResourceGroup
  ]
}

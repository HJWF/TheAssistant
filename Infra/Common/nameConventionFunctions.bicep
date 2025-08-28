import { CoreParams } from './types.bicep'

// Resource Group
@export()
@description('Generates a Resource Group name based on the provided parameters')
func resourceGroup(contextName string, coreParameters CoreParams) string =>
  'rg-${contextName}-${coreParameters.projectPrefix}-${coreParameters.locationShortName}'

// Basic resource
@export()
@description('Generates a basic resource name based on the provided parameters')
func resource(resourceAbbreviation string, coreParameters CoreParams) string =>
  '${resourceAbbreviation}-${coreParameters.projectPrefix}-${coreParameters.locationShortName}'

// Basic resource with context
@export()
@description('Generates a basic resource name with context based on the provided parameters')
func resourceWithContext(resourceAbbreviation string, contextName string, coreParameters CoreParams) string =>
  '${resourceAbbreviation}-${contextName}-${coreParameters.projectPrefix}-${coreParameters.locationShortName}'

// Resource with postfix
@export()
@description('Generates a resource name with a postfix based on the provided parameters')
func resourceWithPostfix(resourceAbbreviation string, postfix string, coreParameters CoreParams) string =>
  '${resourceAbbreviation}-${coreParameters.projectPrefix}-${coreParameters.locationShortName}-${postfix}'

// Storage Account
@export()
@description('Generates a Storage Account name based on the provided parameters. The name is limited to 24 characters and must be lowercase.')
func storageAccountName(contextName string?, coreParameters CoreParams) string =>
  empty(contextName)
    ? take(sanitizeName('st${coreParameters.projectPrefix}${coreParameters.locationShortName}'), 24)
    : take(sanitizeName('st${contextName}${coreParameters.projectPrefix}${coreParameters.locationShortName}'), 24)

// Sanitizes a string by removing '-', '_', and ' ' characters and converting to lowercase
@description('Sanitizes a string by removing "-", "_", and " " characters and converting to lowercase')
func sanitizeName(name string) string =>
  toLower(replace(replace(replace(name, '-', ''), '_', ''), ' ', ''))

param foundryName string

resource foundryName_TheAssistant 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  name: '${foundryName}/TheAssistant'
  location: 'uksouth'
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    description: 'Default project created with the resource'
    displayName: 'TheAssistant'
  }
}

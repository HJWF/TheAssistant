@export()
@description('Core Parameters Definition for Bicep Templates')
@sealed()
type CoreParams = {
  @description('Location Short Name for resource naming')
  locationShortName: string
  @description('Project Prefix for resource naming')
  projectPrefix: string
}

@export()
@description('Core Parameters Constructor')
func newCoreParams(
  locationShortName string,
  projectPrefix string
) CoreParams => {
  locationShortName: locationShortName
  projectPrefix: projectPrefix
}

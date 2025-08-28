[CmdletBinding(DefaultParameterSetName = 'Default')]
param (
    [Parameter()] 
    [string] $subscriptionId = '6e3989a8-f17c-46a2-b3c4-944f5e8b3a60',
 
    [Parameter()] 
    [string] $location = "westeurope",
  
    [Parameter()] 
    [string] $projectName = "TheAssistant",

    <# Deploy switches #>
    [switch] $deploy
)

<# Deployment scripts (PowerShell) to load #>
. ./loadFunctions.ps1

<# Set Variables #>
az account set --subscription $subscriptionId --output none
if (!$?) {
    Write-Host "Something went wrong while setting the correct subscription. Please check and try again." -ForegroundColor Red
    exit $LASTEXITCODE
}

$deployedBy = (az account show | ConvertFrom-Json).user.name 
$formattedProjectName = ToLowerCaseNoSpaces -source $projectName
$locationShortCode = GetLocationShortCode -location $location

$deploymentID = (New-Guid).Guid

$starttime = [System.DateTime]::Now

if ($deploy) {
    Write-Host "  Running a Bicep deployment with ID: '$deploymentID' for Project: '$formattedProjectName'." -ForegroundColor Green
    
    az deployment sub create `
        --name $deploymentID `
        --location $location `
        --template-file ./main.bicep `
        --parameters ./main.bicepparam `
        --parameters deployedBy=$deployedBy projectName=$formattedProjectName location=$location locationShortCode=$LocationShortCode `

    $endtime = [System.DateTime]::Now
    $duration = $endtime - $starttime
    
    Write-Host ('This deployment took : {0:mm} minutes {0:ss} seconds' -f $duration) -BackgroundColor Yellow -ForegroundColor Magenta
}
else {
    Write-Host "  Running a Bicep validation with ID: '$deploymentID' for Project: '$formattedProjectName'." -ForegroundColor Green
    
    az deployment sub validate `
        --name $deploymentID `
        --location $location `
        --template-file ./main.bicep `
        --parameters ./main.bicepparam `
        --parameters deployedBy=$deployedBy projectName=$formattedProjectName location=$location locationShortCode=$LocationShortCode `

    $endtime = [System.DateTime]::Now
    $duration = $endtime - $starttime
    
    Write-Host ('This validation took : {0:mm} minutes {0:ss} seconds' -f $duration) -BackgroundColor Yellow -ForegroundColor Magenta
}

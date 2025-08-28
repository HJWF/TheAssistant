function GetLocationShortCode {
    Param(
        [Parameter(Mandatory=$true)]
        [String] $location
    )

    $formattedLocation = ToLowerCaseNoSpaces -source $location

    $locationShortCodeMap = @{
        "westeurope" = "weu";
        "northeurope" = "neu";
    }
    
    return $locationShortCodeMap.$formattedLocation
}
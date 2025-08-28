function ToLowerCaseNoSpaces {
    Param(
        [Parameter(Mandatory=$true)]
        [String] $source
    )
    
    return $source.ToLower() -replace " ", ""
}
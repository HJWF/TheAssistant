$CurrentPath = Get-Location
$Path = "$CurrentPath\PowerShell\"
Get-ChildItem -Path $Path -Filter *.ps1 | ForEach-Object {
    . $_.FullName
}
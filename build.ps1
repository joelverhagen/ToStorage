param ( [string] $Version, [switch] $SkipTests, [switch] $StartEmulator, [switch] $SkipPack )

$repositoryRoot = $PSScriptRoot
$buildScript = [io.path]::Combine($repositoryRoot, "build", "build.ps1")

& $buildScript -Version $Version -SkipTests:$SkipTests -StartEmulator:$StartEmulator -SkipPack:$SkipPack

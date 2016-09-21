param (
    [string] $Configuration,
    [string] $Version,
    [switch] $SkipRestore,
    [switch] $SkipBuild,
    [switch] $SkipEmulator,
    [switch] $SkipPack,
    [switch] $SkipTests
)

$repositoryRoot = $PSScriptRoot
$buildScript = [io.path]::Combine($repositoryRoot, "build", "build.ps1")

& $buildScript `
    -Configuration $Configuration `
    -Version $Version `
    -SkipRestore:$SkipRestore `
    -SkipBuild:$SkipBuild `
    -SkipEmulator:$SkipEmulator `
    -SkipPack:$SkipPack `
    -SkipTests:$SkipTests 

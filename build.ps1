param (
    [string] $Version,
    [string] $Configuration,
    [switch] $SkipRestore,
    [switch] $SkipBuild,
    [switch] $SkipEmulator,
    [switch] $SkipPack,
    [switch] $SkipTests
)

$repositoryRoot = $PSScriptRoot
$buildScript = [io.path]::Combine($repositoryRoot, "build", "build.ps1")

& $buildScript `
    -Version $Version `
    -Configuration $Configuration `
    -SkipRestore:$SkipRestore `
    -SkipBuild:$SkipBuild `
    -SkipEmulator:$SkipEmulator `
    -SkipPack:$SkipPack `
    -SkipTests:$SkipTests 

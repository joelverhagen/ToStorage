param ( [string] $Version, [switch] $SkipRestore, [switch] $SkipBuild, [switch] $SkipEmulator, [switch] $SkipTests, [switch] $SkipPack )

$repositoryRoot = $PSScriptRoot
$buildScript = [io.path]::Combine($repositoryRoot, "build", "build.ps1")

& $buildScript -Version $Version -SkipRestore:$SkipRestore -SkipBuild:$SkipBuild -SkipEmulator:$SkipEmulator -SkipTests:$SkipTests -SkipPack:$SkipPack

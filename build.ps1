$repositoryRoot = $PSScriptRoot
$buildScript = [io.path]::Combine($repositoryRoot, "build", "build.ps1")
& $buildScript

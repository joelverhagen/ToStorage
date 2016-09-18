# constants
$version = "0.9.0"
$msbuildPath = Join-Path ${env:ProgramFiles(x86)} "MSBuild\14.0\Bin\msbuild.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

# build paths
$buildPath = $PSScriptRoot
$rootPath = Join-Path $buildPath ".."
$nugetPath = Join-Path $buildPath "nuget.exe"
$solutionPath = Join-Path $rootPath "ToStorage.sln"
$artifactsPath = Join-Path $rootPath "artifacts"

# download nuget.exe
if (-Not (Test-Path $nugetPath)) {
	Invoke-WebRequest $nugetUrl -OutFile $nugetPath
}

# install packages
& $nugetPath restore $solutionPath -SolutionDirectory $rootPath
& $nugetPath restore (Join-Path $buildPath "packages.config") -SolutionDirectory $rootPath

# build
& $msbuildPath $solutionPath /t:Build /p:Configuration=Release

if (-Not (Test-Path $artifactsPath)) {
    New-Item -Path $artifactsPath -ItemType directory
}

# find xunit console runner
$xunitPath = Get-ChildItem (Join-Path $rootPath "packages\**\xunit.console.exe") -Recurse
if (!$xunitPath) {
    throw "The build script could not find ilmerge.exe"
}

$xunitPath = $xunitPath.FullName

# test
$testProjects = Get-ChildItem (Join-Path $rootPath "test")
foreach ($testProject in $testProjects) {
    $name = $testProject.Name
    $testAssembly = [io.path]::Combine($testProject.FullName, "bin", "Release", $testProject.Name + ".dll")
    
    & $xunitPath $testAssembly -verbose -diagnostics -parallel all

    if (-Not $?) {
        throw "Test assembly for project $name failed."
    }
}

# find ilmerge
$ilmergePath = Get-ChildItem (Join-Path $rootPath "packages\**\ilmerge.exe") -Recurse
if (!$ilmergePath) {
    throw "The build script could not find ilmerge.exe"
}

$ilmergePath = $ilmergePath.FullName

# ilmerge
$originalTool = "src\Knapcode.ToStorage.Tool\bin\Release"
$toolPath = Join-Path $artifactsPath "ToStorage.exe"
$unmergedExePath = (Get-ChildItem (Join-Path $rootPath (Join-Path $originalTool "*.exe")) | Select-Object -First 1).FullName
$dependencies = Get-ChildItem (Join-Path $rootPath (Join-Path $originalTool "*.dll")) | Select-Object -ExpandProperty FullName
$ilmergeArguments = "/ndebug", ("/ver:" + $version + ".0"), ("/out:" + $toolPath), $unmergedExePath
$ilmergeArguments += $dependencies

& $ilmergePath $ilmergeArguments

# NuGet pack core
& $nugetPath pack (Join-Path $rootPath "src\Knapcode.ToStorage.Core\Knapcode.ToStorage.Core.csproj") -OutputDirectory $artifactsPath -Version $version -Prop Configuration=Release

# NuGet pack tool
& $nugetPath pack (Join-Path $rootPath "src\Knapcode.ToStorage.Tool\Knapcode.ToStorage.Tool.nuspec") -OutputDirectory $artifactsPath -Version $version -BasePath $rootPath

# zip tool
Compress-Archive -Path $toolPath -DestinationPath (Join-Path $artifactsPath ("ToStorage." + $version + ".zip")) -CompressionLevel Optimal -Force

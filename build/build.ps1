# constants
$msbuildPath = Join-Path ${env:ProgramFiles(x86)} "MSBuild\14.0\Bin\msbuild.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$ilmergePattern = "packages\**\ilmerge.exe"
$solutionFileName = "ToStorage.sln"

# build paths
$buildPath = $PSScriptRoot
$rootPath = Join-Path $buildPath ".."
$nugetPath = Join-Path $buildPath "nuget.exe"
$solutionPath = Join-Path $rootPath $solutionFileName

# download nuget.exe
If (-Not (Test-Path $nugetPath)) {
	Invoke-WebRequest $nugetUrl -OutFile $nugetPath
}

# install packages
& $nugetPath restore $solutionPath -SolutionDirectory $rootPath
& $nugetPath restore (Join-Path $buildPath "packages.config") -SolutionDirectory $rootPath

# find ilmerge
$ilmergePath = Get-ChildItem (Join-Path $rootPath $ilmergePattern) -Recurse
if (!$ilmergePath) {
    throw "The build script could not find ilmerge.exe"
}

$ilmergePath = $ilmergePath.FullName

# build
& $msbuildPath $solutionPath /t:Rebuild /p:Configuration=Debug
& $msbuildPath $solutionPath /t:Rebuild /p:Configuration=Release


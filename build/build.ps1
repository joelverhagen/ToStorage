# constants
$version = "0.1.0"
$msbuildPath = Join-Path ${env:ProgramFiles(x86)} "MSBuild\14.0\Bin\msbuild.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

# build paths
$buildPath = $PSScriptRoot
$rootPath = Join-Path $buildPath ".."
$nugetPath = Join-Path $buildPath "nuget.exe"
$solutionPath = Join-Path $rootPath "ToStorage.sln"
$artifactsPath = Join-Path $rootPath "artifacts"

# download nuget.exe
If (-Not (Test-Path $nugetPath)) {
	Invoke-WebRequest $nugetUrl -OutFile $nugetPath
}

# install packages
& $nugetPath restore $solutionPath -SolutionDirectory $rootPath
& $nugetPath restore (Join-Path $buildPath "packages.config") -SolutionDirectory $rootPath

# find ilmerge
$ilmergePath = Get-ChildItem (Join-Path $rootPath "packages\**\ilmerge.exe") -Recurse
if (!$ilmergePath) {
    throw "The build script could not find ilmerge.exe"
}

$ilmergePath = $ilmergePath.FullName

# build
& $msbuildPath $solutionPath /t:Build /p:Configuration=Release

if (-Not (Test-Path $artifactsPath)) {
    New-Item -Path $artifactsPath -ItemType directory
}

# ilmerge
$toolOutput = "ToStorage.Tool\bin\Release"
$unmergedExePath = (Get-ChildItem (Join-Path $rootPath (Join-Path $toolOutput "*.exe")) | Select-Object -First 1).FullName
$dependencies = Get-ChildItem (Join-Path $rootPath (Join-Path $toolOutput "*.dll")) | Select-Object -ExpandProperty FullName
$ilmergeArguments = "/ndebug", ("/ver:" + $version + ".0"), ("/out:" + (Join-Path $artifactsPath "ToStorage.exe")), $unmergedExePath
$ilmergeArguments += $dependencies

& $ilmergePath $ilmergeArguments

# NuGet pack Core
& $nugetPath pack (Join-Path $rootPath "ToStorage.Core\ToStorage.Core.csproj") -OutputDirectory $artifactsPath -Version $version -Prop Configuration=Release

# NuGet pack Tool
& $nugetPath pack (Join-Path $rootPath "ToStorage.Tool\Knapcode.ToStorage.Tool.nuspec") -OutputDirectory $artifactsPath -Version $version -BasePath $rootPath

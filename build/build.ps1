param ( [string] $Version, [switch] $SkipTests, [switch] $StartEmulator, [switch] $SkipPack )

function Show-ErrorExitCode
{
    param ([int[]] $SuccessCodes = @(0))
    if ($SuccessCodes -NotContains $LastExitCode)
    {
        throw "Exit code $LastExitCode."
    }
}

# constants
$msbuild = Join-Path ${env:ProgramFiles(x86)} "MSBuild\14.0\Bin\msbuild.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$emulatorMsiUrl = "http://download.microsoft.com/download/7/B/5/7B53AA52-9519-467C-8DC7-1A1FF72500D9/MicrosoftAzureStorageEmulator.msi"
$emulatorMsiPath = [io.path]::Combine($env:APPVEYOR_BUILD_FOLDER, "packages", "MicrosoftAzureStorageEmulator.msi")
$emulatorPath = "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"
$dotnetCliUrl = "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0-preview2/scripts/obtain/dotnet-install.ps1"

# build paths
$buildPath = $PSScriptRoot
$rootPath = [io.path]::GetFullPath((Join-Path $buildPath ".."))
$nuget = Join-Path $buildPath "nuget.exe"
$solutionPath = Join-Path $rootPath "ToStorage.sln"
$artifactsPath = Join-Path $rootPath "artifacts"
$dotnetCliPath = Join-Path $rootPath "cli"
$dotnet = Join-Path $dotnetCliPath "dotnet.exe"
$dotnetCliInstallScript = Join-Path $dotnetCliPath "dotnet-install.ps1"

# set the default version, if necessary
if (-Not $Version) {
    $Version = Get-Content (Join-Path $rootPath "appveyor.yml") | `
        ? { $_.StartsWith("version:") } | `
        % { $_.Substring("version:".Length).Trim() } | `
        % { $_.Replace("{build}", "0") }
        Select-Object -First 1
}

# set the NuGet package version
$parsedVersion = [version]$version
$packVersion = ([version]::new($parsedVersion.Major, $parsedVersion.Minor, $parsedVersion.Build)).ToString();

# download nuget.exe
if (-Not (Test-Path $nuget)) {
	Invoke-WebRequest $nugetUrl -OutFile $nuget
}

# install .NET CLI
New-Item $dotnetCliPath -Force -Type Directory | Out-Null
Invoke-WebRequest $dotnetCliUrl -OutFile $dotnetCliInstallScript
& $dotnetCliInstallScript -InstallDir $dotnetCliPath -Version 1.0.0-preview2-003121
Show-ErrorExitCode

# restore
& $dotnet restore $rootPath
& $nuget restore (Join-Path $buildPath "packages.config") -SolutionDirectory $rootPath

# build
$projects = Get-ChildItem $rootPath -Include project.json -Recurse
foreach ($project in $projects) {
	& $dotnet build $project.FullName --configuration Release
}

if (-Not (Test-Path $artifactsPath)) {
    New-Item -Path $artifactsPath -ItemType directory
}

if ($StartEmulator) {
    # start Azure Storage emulator
    Invoke-WebRequest -Uri $emulatorMsiUrl -OutFile $emulatorMsiPath
    cmd /c start /wait msiexec /i $msiPath /quiet
    & $emulatorPath start
    & $emulatorPath status
}

if (-Not $SkipTests) {
    # test
    $testProjects = Get-ChildItem (Join-Path $rootPath "test")
    foreach ($testProject in $testProjects) {
	    $name = $testProject.Name
        & $dotnet test $testProject.FullName -verbose -diagnostics -parallel all
		Show-ErrorExitCode
    }
}

if (-Not $SkipPack) {
	Get-ChildItem $artifactsPath -Recurse | Remove-Item -Force -Recurse

    # find ilmerge
    $ilmerge = Get-ChildItem (Join-Path $rootPath "packages\**\ilmerge.exe") -Recurse
    if (!$ilmerge) {
        throw "The build script could not find ilmerge.exe"
    }

    $ilmerge = $ilmerge.FullName

	# publish
	& $dotnet publish (Join-Path $rootPath "src\Knapcode.ToStorage.Tool\project.json") --framework net45 --configuration Release
	$originalTool = Join-Path $rootPath "src\Knapcode.ToStorage.Tool\bin\Release\net45\win7-x64\publish"

    # ilmerge
	Write-Host $originalTool
    $unmergedExePath = Get-ChildItem $originalTool\* -Include "*.exe" | Select-Object -ExpandProperty FullName | Select-Object -First 1
    $dependencies = Get-ChildItem $originalTool\* -Include "*.dll" | Select-Object -ExpandProperty FullName
    $toolPath = Join-Path $artifactsPath "ToStorage.exe"
	$ilmergeArguments = "/ndebug", ("/out:" + $toolPath), $unmergedExePath
    $ilmergeArguments += $dependencies

    & $ilmerge $ilmergeArguments

    # NuGet pack core
    & $nuget pack (Join-Path $rootPath "src\Knapcode.ToStorage.Core\Knapcode.ToStorage.Core.nuspec") -OutputDirectory $artifactsPath -Version $packVersion

    # NuGet pack tool
    & $nuget pack (Join-Path $rootPath "src\Knapcode.ToStorage.Tool\Knapcode.ToStorage.Tool.nuspec") -OutputDirectory $artifactsPath -Version $packVersion -BasePath $rootPath

    # zip tool
    Compress-Archive -Path $toolPath -DestinationPath (Join-Path $artifactsPath ("ToStorage." + $packVersion + ".zip")) -CompressionLevel Optimal -Force
}

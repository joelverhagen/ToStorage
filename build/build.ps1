param ( [string] $Version, [switch] $SkipTests, [switch] $StartEmulator, [switch] $SkipPack )

# constants
$msbuildPath = Join-Path ${env:ProgramFiles(x86)} "MSBuild\14.0\Bin\msbuild.exe"
$nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$emulatorMsiUrl = "http://download.microsoft.com/download/7/B/5/7B53AA52-9519-467C-8DC7-1A1FF72500D9/MicrosoftAzureStorageEmulator.msi"
$emulatorMsiPath = [io.path]::Combine($env:APPVEYOR_BUILD_FOLDER, "packages", "MicrosoftAzureStorageEmulator.msi")
$emulatorPath = "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"

# build paths
$buildPath = $PSScriptRoot
$rootPath = Join-Path $buildPath ".."
$nugetPath = Join-Path $buildPath "nuget.exe"
$solutionPath = Join-Path $rootPath "ToStorage.sln"
$artifactsPath = Join-Path $rootPath "artifacts"

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

if ($StartEmulator) {
    # start Azure Storage emulator
    Invoke-WebRequest -Uri $emulatorMsiUrl -OutFile $emulatorMsiPath
    cmd /c start /wait msiexec /i $msiPath /quiet
    & $emulatorPath start
    & $emulatorPath status
}

if (-Not $SkipTests) {
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
}

if (-Not $SkipPack) {
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
    $ilmergeArguments = "/ndebug", ("/out:" + $toolPath), $unmergedExePath
    $ilmergeArguments += $dependencies

    & $ilmergePath $ilmergeArguments

    # NuGet pack core
    & $nugetPath pack (Join-Path $rootPath "src\Knapcode.ToStorage.Core\Knapcode.ToStorage.Core.csproj") -OutputDirectory $artifactsPath -Version $packVersion -Prop Configuration=Release

    # NuGet pack tool
    & $nugetPath pack (Join-Path $rootPath "src\Knapcode.ToStorage.Tool\Knapcode.ToStorage.Tool.nuspec") -OutputDirectory $artifactsPath -Version $packVersion -BasePath $rootPath

    # zip tool
    Compress-Archive -Path $toolPath -DestinationPath (Join-Path $artifactsPath ("ToStorage." + $packVersion + ".zip")) -CompressionLevel Optimal -Force
}

function Get-CurrentFilePath {
    param([string]$fileName)
    $PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.ScriptBlock.File
	$targetPath = Join-Path -Path $PSScriptRoot -ChildPath $fileName 
	return $targetPath
}

$currentPath = Get-CurrentFilePath
$openCoverPath = Join-Path $currentPath "src\packages\OpenCover.4.6.166\tools\OpenCover.Console.exe"
$coverallsPath = Join-Path $currentPath "src\packages\coveralls.io.1.3.4\tools\coveralls.net.exe"
$testDllPath = Join-Path $currentPath "src\sendstorm.tests\bin\release\Sendstorm.Tests.dll"
$vsTestPath = "c:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"

& $openCoverPath -register:user -filter:"+[*]* -[sendstorm.tests]*" -target:$vsTestPath -targetargs:"$testDllPath" -output:coverage.xml
& $coverallsPath --opencover coverage.xml
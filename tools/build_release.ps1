# Builds the Play upload (build/NightCafe.aab) in a headless editor with the dev tooling
# package taken out of the project for the duration of the build.
#
#   powershell -File tools/build_release.ps1
#
# com.unity.pipeline (the `unity cmd` bridge) ships runtime assemblies - an IL interpreter and
# a player connection for code reload - that have no place in a release, so the manifest is
# copied aside, the package entry dropped, the build run through
# NightCafe.EditorTools.BuildAndroid.BuildAab, and the manifest put back whatever happens.
# Needs the project closed in the editor (a second instance cannot open it) and
# build/keystore.local.json (see BuildAndroid.cs).

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$unity = "C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe"
$manifest = Join-Path $root "Packages\manifest.json"
$lock = Join-Path $root "Packages\packages-lock.json"
$log = Join-Path $root "build\aab.log"

if (-not (Test-Path (Join-Path $root "build\keystore.local.json"))) {
    throw "build/keystore.local.json missing - the upload key credentials (see BuildAndroid.cs)."
}
if (Test-Path (Join-Path $root "Temp\UnityLockfile")) {
    throw "The project is open in the editor - close it first (a headless build needs the project to itself)."
}

New-Item -ItemType Directory -Force (Join-Path $root "build") | Out-Null
Copy-Item $manifest "$manifest.release-backup" -Force
if (Test-Path $lock) { Copy-Item $lock "$lock.release-backup" -Force }

try {
    $json = Get-Content $manifest -Raw | ConvertFrom-Json
    if ($json.dependencies.PSObject.Properties.Name -contains "com.unity.pipeline") {
        $json.dependencies.PSObject.Properties.Remove("com.unity.pipeline")
        $json | ConvertTo-Json -Depth 10 | Set-Content $manifest -Encoding utf8
        Write-Host "com.unity.pipeline removed from the manifest for this build"
    }
    if (Test-Path $lock) { Remove-Item $lock }

    & $unity -batchmode -nographics -quit -projectPath $root -buildTarget Android `
        -executeMethod NightCafe.EditorTools.BuildAndroid.BuildAab -logFile $log
    $code = $LASTEXITCODE
    if ($code -ne 0) {
        Get-Content $log -Tail 40
        throw "Unity exited with $code - see $log"
    }
    Write-Host "OK: $(Join-Path $root 'build\NightCafe.aab')"
}
finally {
    Move-Item "$manifest.release-backup" $manifest -Force
    if (Test-Path "$lock.release-backup") { Move-Item "$lock.release-backup" $lock -Force }
    Write-Host "manifest restored"
}

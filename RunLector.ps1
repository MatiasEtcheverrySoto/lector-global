$pidFile = Join-Path $PSScriptRoot "lector.pid"
if (Test-Path $pidFile) {
    $oldPid = Get-Content $pidFile
    Stop-Process -Id $oldPid -Force -ErrorAction SilentlyContinue
}
$PID | Out-File $pidFile -Force
Start-Sleep -Milliseconds 500

try {
    if (-not $PSScriptRoot) {
        $PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
    }
    Set-Location $PSScriptRoot
    $source = Get-Content -Path ".\LectorGlobal.cs" -Raw
    Add-Type -TypeDefinition $source -ReferencedAssemblies "System.Windows.Forms", "System.Speech", "System.Drawing", "System"
    $host.ui.RawUI.WindowTitle = "LectorGlobal_Background"
    [LectorGlobal.Program]::Main()
} catch {
    Out-File -FilePath "C:\Antigravity_proyectos\Lector e IDE\debug_ps1.txt" -InputObject $_.Exception.Message -Append
    Out-File -FilePath "C:\Antigravity_proyectos\Lector e IDE\debug_ps1.txt" -InputObject $_.ScriptStackTrace -Append
}

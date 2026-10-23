param([switch]$ReloadBackup, [switch]$RenderPreview, [switch]$KeepOpenScene)
$ErrorActionPreference = 'Stop'
$unityEditor = Get-Process Unity | Where-Object { $_.MainWindowTitle -like 'Derby_Segundo_Tempo*' } | Select-Object -First 1
if (-not $unityEditor) { throw 'The Derby Unity editor is not open.' }
$automationShell = New-Object -ComObject WScript.Shell
[void]$automationShell.AppActivate($unityEditor.Id)
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class UnityPreviewFocus {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr info);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
}
'@
[void][UnityPreviewFocus]::BringWindowToTop($unityEditor.MainWindowHandle)
[void][UnityPreviewFocus]::SetForegroundWindow($unityEditor.MainWindowHandle)
Start-Sleep -Milliseconds 800
if ($ReloadBackup -or $KeepOpenScene) {
    if ($ReloadBackup -and -not $KeepOpenScene -and $unityEditor.MainWindowTitle.Contains('*')) {
        throw 'The Unity scene has unsaved changes. Save or retain those changes before reloading.'
    }
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    $processCondition = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $unityEditor.Id)
    $unityWindows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $processCondition)
    $reloaded = $false
    foreach ($unityWindow in $unityWindows) {
        Write-Output ('Unity window: ' + $unityWindow.Current.Name)
        $dialogButton = if ($KeepOpenScene) { 'Ignore' } else { 'Reload' }
        $buttonCondition = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, $dialogButton)
        $reloadButton = $unityWindow.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
        if ($reloadButton) {
            $reloadBounds = $reloadButton.Current.BoundingRectangle
            [void][UnityPreviewFocus]::SetCursorPos([int]($reloadBounds.X + $reloadBounds.Width / 2), [int]($reloadBounds.Y + $reloadBounds.Height / 2))
            [UnityPreviewFocus]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
            [UnityPreviewFocus]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
            $reloaded = $true
            Write-Output ('Selected ' + $dialogButton + ' for the external-change dialog.')
        }
    }
    if (-not $reloaded) { Write-Output 'No matching external-change dialog found.' }
    Start-Sleep -Seconds 2
}
$automationShell.SendKeys('^r')
Start-Sleep -Seconds 6
if ($RenderPreview) {
    $automationShell.SendKeys('^+{F9}')
    Start-Sleep -Seconds 3
}
Write-Output 'Unity asset refresh requested.'

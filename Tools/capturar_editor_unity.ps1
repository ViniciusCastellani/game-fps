$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class UnityWindowCapture {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
}
'@
$unityEditor = Get-Process Unity | Where-Object { $_.MainWindowTitle -like 'Derby_Segundo_Tempo*' } | Select-Object -First 1
$automationShell = New-Object -ComObject WScript.Shell
[void]$automationShell.AppActivate($unityEditor.Id)
[void][UnityWindowCapture]::SetForegroundWindow($unityEditor.MainWindowHandle)
Start-Sleep -Seconds 2
$bounds = New-Object UnityWindowCapture+RECT
[void][UnityWindowCapture]::GetWindowRect($unityEditor.MainWindowHandle, [ref]$bounds)
$image = New-Object System.Drawing.Bitmap ($bounds.Right - $bounds.Left), ($bounds.Bottom - $bounds.Top)
$graphics = [System.Drawing.Graphics]::FromImage($image)
try {
    $graphics.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $image.Size)
    $image.Save((Join-Path $PSScriptRoot '..\Library\CenarioTPSPreview\editor.png'), [System.Drawing.Imaging.ImageFormat]::Png)
} finally { $graphics.Dispose(); $image.Dispose() }

$ErrorActionPreference = 'Stop'
$unity = Get-Process Unity | Where-Object { $_.MainWindowTitle -like 'Derby_Segundo_Tempo*' } | Select-Object -First 1
if (-not $unity) { throw 'A janela do projeto Derby nao esta disponivel.' }
Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class DerbyUnityMenu {
    [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr menu, int position);
    [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr menu);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr menu, uint item, StringBuilder text, int count, uint flags);
    [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr menu, int position);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
'@
function Find-UnityRefresh([IntPtr]$menu, [string]$path) {
    $count = [DerbyUnityMenu]::GetMenuItemCount($menu)
    for ($index = 0; $index -lt $count; $index++) {
        $label = New-Object System.Text.StringBuilder 256
        [void][DerbyUnityMenu]::GetMenuString($menu, [uint32]$index, $label, 256, 0x400)
        $name = ($label.ToString() -replace '&', '' -split "`t")[0].Trim()
        $childPath = if ($path) { $path + '/' + $name } else { $name }
        if ($childPath -eq 'Assets/Refresh') {
            return [DerbyUnityMenu]::GetMenuItemID($menu, $index)
        }
        $subMenu = [DerbyUnityMenu]::GetSubMenu($menu, $index)
        if ($subMenu -ne [IntPtr]::Zero) {
            $found = Find-UnityRefresh $subMenu $childPath
            if ($null -ne $found) { return $found }
        }
    }
    return $null
}
$menu = [DerbyUnityMenu]::GetMenu($unity.MainWindowHandle)
if ($menu -eq [IntPtr]::Zero) { throw 'Menu nativo do Unity indisponivel.' }
$command = Find-UnityRefresh $menu ''
if ($null -eq $command) { throw 'Comando Assets/Refresh nao encontrado.' }
if (-not [DerbyUnityMenu]::PostMessage($unity.MainWindowHandle, 0x111, [IntPtr]([int64]$command), [IntPtr]::Zero)) {
    throw 'Nao foi possivel solicitar Assets/Refresh.'
}
Write-Output ('Unity Assets/Refresh solicitado pelo menu nativo. Command=' + $command)

param([ValidateSet('Instalar', 'Testar')][string]$Acao = 'Instalar')
$ErrorActionPreference = 'Stop'
$editor = Get-Process Unity | Where-Object { $_.MainWindowTitle -like 'Derby_Segundo_Tempo*' } | Select-Object -First 1
if (-not $editor) { throw 'O editor do projeto Derby nao esta aberto.' }
$caminhos = @{
    Instalar = 'Derby/Camera terceira pessoa/Corrigir colisao nas fases'
    Testar = 'Derby/Camera terceira pessoa/Testar colisao em Play Mode'
}
Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class DerbyCameraTPMenu {
    [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr menu, int position);
    [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr menu);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr menu, uint item, StringBuilder text, int count, uint flags);
    [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr menu, int position);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
'@
function Find-CameraTPCommand([IntPtr]$menu, [string]$pai) {
    for ($indice = 0; $indice -lt [DerbyCameraTPMenu]::GetMenuItemCount($menu); $indice++) {
        $texto = New-Object System.Text.StringBuilder 256
        [void][DerbyCameraTPMenu]::GetMenuString($menu, [uint32]$indice, $texto, 256, 0x400)
        $nome = ($texto.ToString() -replace '&', '' -split "`t")[0].Trim()
        $caminho = if ($pai) { $pai + '/' + $nome } else { $nome }
        if ($caminho -eq $caminhos[$Acao]) { return [DerbyCameraTPMenu]::GetMenuItemID($menu, $indice) }
        $filho = [DerbyCameraTPMenu]::GetSubMenu($menu, $indice)
        if ($filho -ne [IntPtr]::Zero) {
            $comando = Find-CameraTPCommand $filho $caminho
            if ($null -ne $comando) { return $comando }
        }
    }
    return $null
}
$comando = Find-CameraTPCommand ([DerbyCameraTPMenu]::GetMenu($editor.MainWindowHandle)) ''
if ($null -eq $comando) { throw ('Menu indisponivel: ' + $caminhos[$Acao] + '. Aguarde a compilacao no Unity.') }
if (-not [DerbyCameraTPMenu]::PostMessage($editor.MainWindowHandle, 0x111, [IntPtr]([int64]$comando), [IntPtr]::Zero)) {
    throw 'Nao foi possivel executar o comando do CameraTP.'
}
Write-Output ('Solicitado no Unity: ' + $caminhos[$Acao])

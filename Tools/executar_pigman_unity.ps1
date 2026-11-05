param([ValidateSet('Instalar', 'Previa', 'Testar')][string]$Acao = 'Previa')
$ErrorActionPreference = 'Stop'
$editor = Get-Process Unity | Where-Object { $_.MainWindowTitle -like 'Derby_Segundo_Tempo*' } | Select-Object -First 1
if (-not $editor) { throw 'O editor do projeto Derby nao esta aberto.' }
$caminhos = @{
    Instalar = 'Derby/PigMan/Criar mini-boss e configurar hordas'
    Previa = 'Derby/PigMan/Gerar previas das animacoes'
    Testar = 'Derby/PigMan/Testar mini-boss em Play Mode'
}
Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class DerbyPigManMenu {
    [DllImport("user32.dll")] public static extern IntPtr GetMenu(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetSubMenu(IntPtr menu, int position);
    [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr menu);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr menu, uint item, StringBuilder text, int count, uint flags);
    [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr menu, int position);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
'@
function Find-PigManCommand([IntPtr]$menu, [string]$pai) {
    for ($indice = 0; $indice -lt [DerbyPigManMenu]::GetMenuItemCount($menu); $indice++) {
        $texto = New-Object System.Text.StringBuilder 256
        [void][DerbyPigManMenu]::GetMenuString($menu, [uint32]$indice, $texto, 256, 0x400)
        $nome = ($texto.ToString() -replace '&', '' -split "`t")[0].Trim()
        $caminho = if ($pai) { $pai + '/' + $nome } else { $nome }
        if ($caminho -eq $caminhos[$Acao]) { return [DerbyPigManMenu]::GetMenuItemID($menu, $indice) }
        $filho = [DerbyPigManMenu]::GetSubMenu($menu, $indice)
        if ($filho -ne [IntPtr]::Zero) {
            $comando = Find-PigManCommand $filho $caminho
            if ($null -ne $comando) { return $comando }
        }
    }
    return $null
}
$comando = Find-PigManCommand ([DerbyPigManMenu]::GetMenu($editor.MainWindowHandle)) ''
if ($null -eq $comando) { throw ('Menu indisponivel: ' + $caminhos[$Acao] + '. Aguarde a compilacao no Unity.') }
if (-not [DerbyPigManMenu]::PostMessage($editor.MainWindowHandle, 0x111, [IntPtr]([int64]$comando), [IntPtr]::Zero)) {
    throw 'Nao foi possivel executar o comando do PigMan.'
}
Write-Output ('Solicitado no Unity: ' + $caminhos[$Acao])

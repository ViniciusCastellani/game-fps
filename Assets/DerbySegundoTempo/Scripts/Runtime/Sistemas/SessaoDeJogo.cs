using UnityEngine;

public enum ModoDeJogo
{
    Normal,
    Infinito
}

public static class SessaoDeJogo
{
    private static bool pedidoDeEscolhaDeCenario;

    public static ModoDeJogo Modo { get; private set; }
    public static string CenaDoModoInfinito { get; private set; }
    public static string UltimoCenarioInfinito { get; private set; } = GerenciadorDeCenas.PrimeiraFase;

    public static bool EhInfinitoNaCena(string nomeDaCena)
    {
        return Modo == ModoDeJogo.Infinito && CenaDoModoInfinito == nomeDaCena;
    }

    public static void DefinirNormal()
    {
        Modo = ModoDeJogo.Normal;
        CenaDoModoInfinito = null;
        pedidoDeEscolhaDeCenario = false;
    }

    public static void IniciarInfinito(string cena)
    {
        Modo = ModoDeJogo.Infinito;
        CenaDoModoInfinito = cena;
        UltimoCenarioInfinito = cena;
        pedidoDeEscolhaDeCenario = false;
    }

    public static void PedirEscolhaDeCenario()
    {
        pedidoDeEscolhaDeCenario = true;
    }

    public static bool ConsumirPedidoDeEscolhaDeCenario()
    {
        bool pedido = pedidoDeEscolhaDeCenario;
        pedidoDeEscolhaDeCenario = false;
        return pedido;
    }

    public static void Reiniciar()
    {
        DefinirNormal();
        UltimoCenarioInfinito = GerenciadorDeCenas.PrimeiraFase;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void AoIniciarAplicacao()
    {
        Reiniciar();
    }
}

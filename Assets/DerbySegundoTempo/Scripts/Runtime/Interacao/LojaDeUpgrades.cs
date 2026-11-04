using UnityEngine;

public class LojaDeUpgrades : MonoBehaviour
{
    public JogadorVida jogadorVida;
    public Arma arma;

    [Header("Custos (em pontos)")]
    public int custoCurarTotal = 50;
    public int custoAumentarDano = 100;
    public int custoAumentarCapacidadePente = 80;

    [Header("Valores dos upgrades")]
    public int aumentoDeDano = 5;
    public int aumentoDeCapacidadePente = 10;

    [Header("Teclas (upgrade rápido, sem precisar de UI)")]
    public KeyCode teclaCurar = KeyCode.Alpha1;
    public KeyCode teclaUpgradeDano = KeyCode.Alpha2;
    public KeyCode teclaUpgradePente = KeyCode.Alpha3;

    void Update()
    {
        if (Input.GetKeyDown(teclaCurar))
        {
            CurarTotalmente();
        }

        if (Input.GetKeyDown(teclaUpgradeDano))
        {
            AumentarDano();
        }

        if (Input.GetKeyDown(teclaUpgradePente))
        {
            AumentarCapacidadeDoPente();
        }
    }

    public void CurarTotalmente()
    {
        if (!SistemaDePontos.instancia.GastarPontos(custoCurarTotal))
        {
            return;
        }

        jogadorVida.RecuperarVida(jogadorVida.vidaMaxima);
        Debug.Log("Vida totalmente recuperada!");
    }

    public void AumentarDano()
    {
        if (!SistemaDePontos.instancia.GastarPontos(custoAumentarDano))
        {
            return;
        }

        arma.dano += aumentoDeDano;
        Debug.Log("Dano da arma aumentado para " + arma.dano);
    }

    public void AumentarCapacidadeDoPente()
    {
        if (!SistemaDePontos.instancia.GastarPontos(custoAumentarCapacidadePente))
        {
            return;
        }

        arma.AumentarCapacidadeDoPente(aumentoDeCapacidadePente);
        Debug.Log("Capacidade do pente aumentada para " + arma.capacidadePente);
    }
}
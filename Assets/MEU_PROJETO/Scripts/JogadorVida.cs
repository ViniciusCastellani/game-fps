using UnityEngine;

public class JogadorVida : MonoBehaviour
{
    public int vidaMaxima = 100;
    public int vidaAtual;

    void Start()
    {
        vidaAtual = vidaMaxima;
    }

    public void ReceberDano(int dano)
    {
        vidaAtual -= dano;

        if (vidaAtual < 0)
        {
            vidaAtual = 0;
        }

        Debug.Log("Vida do jogador: " + vidaAtual + "/" + vidaMaxima);

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    public void RecuperarVida(int quantidade)
    {
        vidaAtual += quantidade;

        if (vidaAtual > vidaMaxima)
        {
            vidaAtual = vidaMaxima;
        }

        Debug.Log("Vida do jogador: " + vidaAtual + "/" + vidaMaxima);
    }

    void Morrer()
    {
        Debug.Log("Jogador morreu.");
    }
}
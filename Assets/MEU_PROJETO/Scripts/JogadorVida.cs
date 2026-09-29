using UnityEngine;

public class JogadorVida : MonoBehaviour
{
    public int vidaMaxima = 100;
    public int vidaAtual;

    [SerializeField] private GerenciadorDeJogo gerenciadorDeJogo;

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

        // Pode ficar vazio em uma cena de teste sem GerenciadorDeJogo.
        if (gerenciadorDeJogo != null)
        {
            gerenciadorDeJogo.GameOver("SUA VIDA CHEGOU A ZERO!");
        }
    }
}
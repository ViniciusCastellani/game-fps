using UnityEngine;
using TMPro;

public class HUDJogador : MonoBehaviour
{
    public JogadorVida jogadorVida;
    public Arma arma;

    public TMP_Text textoVida;
    public TMP_Text textoMunicao;

    void Start()
    {
        if (jogadorVida == null)
            jogadorVida = FindFirstObjectByType<JogadorVida>();

        if (arma == null)
            arma = FindFirstObjectByType<Arma>();
    }

    void Update()
    {
        AtualizarVida();
        AtualizarMunicao();
    }

    void AtualizarVida()
    {
        if (textoVida == null || jogadorVida == null) return;

        textoVida.text =
            "VIDA: " + jogadorVida.vidaAtual + " / " + jogadorVida.vidaMaxima;
    }

    void AtualizarMunicao()
    {
        if (textoMunicao == null || arma == null) return;

        textoMunicao.text =
            "MUNIÇÃO: " + arma.municaoAtual + " / " + arma.municaoReserva;
    }
}
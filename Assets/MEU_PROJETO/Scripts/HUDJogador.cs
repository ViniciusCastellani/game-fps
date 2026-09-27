using UnityEngine;
using TMPro;

public class HUDJogador : MonoBehaviour
{
    public JogadorVida jogadorVida;
    public Arma arma;

    public TMP_Text textoVida;
    public TMP_Text textoMunicao;

    void Update()
    {
        AtualizarVida();
        AtualizarMunicao();
    }

    void AtualizarVida()
    {
        textoVida.text =
            "VIDA: "
            + jogadorVida.vidaAtual
            + " / "
            + jogadorVida.vidaMaxima;
    }

    void AtualizarMunicao()
    {
        textoMunicao.text =
            "MUNIÇÃO: "
            + arma.municaoAtual
            + " / "
            + arma.municaoReserva;
    }
}
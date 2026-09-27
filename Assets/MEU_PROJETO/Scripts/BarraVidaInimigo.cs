using UnityEngine;
using UnityEngine.UI;

public class BarraVidaInimigo : MonoBehaviour
{
    public Slider barra;

    // Arraste o objeto "Fill" (dentro de "Fill Area") do Slider aqui.
    // É ele que muda de cor conforme a vida do inimigo.
    public Image imagemDePreenchimento;

    public Color corVidaCheia = Color.green;
    public Color corVidaBaixa = Color.red;

    private InimigoVida inimigoVida;

    // Guardamos a vida máxima separadamente, capturada uma única vez.
    // Antes, o maxValue usava o valor ATUAL de "vida", então se algo
    // alterasse a vida antes desse Start rodar, a barra nascia errada
    // (por isso ela podia aparecer parcialmente "vermelha" já no início).
    private int vidaMaxima;

    void Start()
    {
        inimigoVida = GetComponentInParent<InimigoVida>();

        vidaMaxima = inimigoVida.vida;

        barra.minValue = 0;
        barra.maxValue = vidaMaxima;
        barra.value = vidaMaxima;

        AtualizarCor();
    }

    public void AtualizarBarra()
    {
        barra.value = inimigoVida.vida;
        AtualizarCor();
    }

    void AtualizarCor()
    {
        if (imagemDePreenchimento == null)
        {
            return;
        }

        float porcentagemDeVida = (float)inimigoVida.vida / vidaMaxima;

        imagemDePreenchimento.color = Color.Lerp(
            corVidaBaixa,
            corVidaCheia,
            porcentagemDeVida
        );
    }
}
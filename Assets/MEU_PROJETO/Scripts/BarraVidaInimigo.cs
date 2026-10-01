using UnityEngine;
using UnityEngine.UI;

public class BarraVidaInimigo : MonoBehaviour
{
    public Slider barra;

    private InimigoVida inimigoVida;

    private int vidaMaxima;

    void Start()
    {
        inimigoVida = GetComponentInParent<InimigoVida>();

        vidaMaxima = inimigoVida.vida;

        barra.minValue = 0;
        barra.maxValue = vidaMaxima;
        barra.value = vidaMaxima;

    }

    public void AtualizarBarra()
    {
        barra.value = inimigoVida.vida;
    }
}
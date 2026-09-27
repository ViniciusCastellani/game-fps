using UnityEngine;
using UnityEngine.UI;

public class BarraVidaInimigo : MonoBehaviour
{
    public Slider barra;

    private InimigoVida inimigoVida;

    void Start()
    {
        inimigoVida = GetComponentInParent<InimigoVida>();

        barra.maxValue = inimigoVida.vida;
        barra.value = inimigoVida.vida;
    }

    public void AtualizarBarra()
    {
        barra.value = inimigoVida.vida;
    }
}
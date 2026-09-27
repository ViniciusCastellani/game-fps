using UnityEngine;
using UnityEngine.UI;

public class BarraVidaInimigo : MonoBehaviour
{
    public Slider barra;

    private InimigoVida inimigoVida;

    void Start()
    {
        Debug.Log("Load Barra Inimigo");
        inimigoVida = GetComponentInParent<InimigoVida>();

        barra.maxValue = inimigoVida.vida;
        barra.value = inimigoVida.vida;
    }

    public void AtualizarBarra()
    {
        Debug.Log("Update Barra Inimigo");
        barra.value = inimigoVida.vida;
    }
}
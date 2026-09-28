using UnityEngine;
using UnityEngine.UI;

public class BarraVidaInimigo : MonoBehaviour
{
    public Slider barra;

    private InimigoVida inimigoVida;

    // Guardamos a vida máxima separadamente, capturada uma única vez.
    // Se não fizer isso, o maxValue usa o valor ATUAL de "vida", e se algo
    // alterar a vida antes desse Start rodar, a barra nasce errada.
    private int vidaMaxima;

    void Start()
    {
        inimigoVida = GetComponentInParent<InimigoVida>();

        vidaMaxima = inimigoVida.vida;

        barra.minValue = 0;
        barra.maxValue = vidaMaxima;
        barra.value = vidaMaxima;

        // As cores (verde no Fill, vermelho no Background) NÃO são definidas
        // aqui por código — configure elas direto no Inspector (veja as
        // instruções). Aqui só cuidamos do valor da barra.
    }

    public void AtualizarBarra()
    {
        barra.value = inimigoVida.vida;
    }
}
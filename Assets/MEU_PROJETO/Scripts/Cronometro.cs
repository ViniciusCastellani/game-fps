using UnityEngine;
using TMPro;

public class Cronometro : MonoBehaviour
{
    [SerializeField] private float tempoInicial = 120f;
    [SerializeField] private TMP_Text textoTempo;
    [SerializeField] private GerenciadorDeJogo gerenciadorDeJogo;

    private float tempoRestante;
    private bool contando = false;

    public void Iniciar()
    {
        tempoRestante = tempoInicial;
        contando = true;
        AtualizarTexto();
    }

    public void Parar()
    {
        contando = false;
    }

    void Update()
    {
        if (!contando || !gerenciadorDeJogo.EstaJogando)
        {
            return;
        }

        tempoRestante = Mathf.Max(0f, tempoRestante - Time.deltaTime);
        AtualizarTexto();

        if (tempoRestante <= 0f)
        {
            contando = false;
            gerenciadorDeJogo.GameOver("O TEMPO ACABOU!");
        }
    }

    void AtualizarTexto()
    {
        if (textoTempo == null)
        {
            return;
        }

        int segundosTotais = Mathf.CeilToInt(tempoRestante);
        int minutos = segundosTotais / 60;
        int segundos = segundosTotais % 60;

        textoTempo.text = "TEMPO: " + minutos.ToString("00") + ":" + segundos.ToString("00");
    }
}

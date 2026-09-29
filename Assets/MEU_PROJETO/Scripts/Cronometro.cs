using UnityEngine;
using TMPro;

// Cronômetro regressivo da fase. Coloque no mesmo objeto do GerenciadorDeJogo.
// Quem inicia/para a contagem é o GerenciadorDeOrdas (início e fim de cada orda).
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
        // Time.deltaTime já vale 0 com o jogo pausado, mas conferir o estado
        // deixa a regra explícita: só conta enquanto está "Jogando".
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

        // CeilToInt: só mostra 00:00 quando o tempo realmente acabou.
        int segundosTotais = Mathf.CeilToInt(tempoRestante);
        int minutos = segundosTotais / 60;
        int segundos = segundosTotais % 60;

        textoTempo.text = "TEMPO: " + minutos.ToString("00") + ":" + segundos.ToString("00");
    }
}

using UnityEngine;
using TMPro;

// Coloque em um Canvas próprio com Sort Order alto (ex: "CanvasFPS"), em cada cena,
// para o texto ficar por cima de tudo, inclusive das telas de Pause/Game Over/Vitória.
public class ContadorFPS : MonoBehaviour
{
    [SerializeField] private TMP_Text textoFPS;
    [SerializeField] private float intervaloDeAtualizacao = 0.5f;

    private int quadros = 0;
    private float tempoAcumulado = 0f;

    void Update()
    {
        // unscaledDeltaTime continua contando mesmo com Time.timeScale = 0
        // (pause, game over e vitória).
        quadros++;
        tempoAcumulado += Time.unscaledDeltaTime;

        if (tempoAcumulado >= intervaloDeAtualizacao)
        {
            textoFPS.text = "FPS: " + Mathf.RoundToInt(quadros / tempoAcumulado);

            quadros = 0;
            tempoAcumulado = 0f;
        }
    }
}

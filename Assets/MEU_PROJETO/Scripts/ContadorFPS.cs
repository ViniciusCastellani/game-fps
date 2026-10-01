using UnityEngine;
using TMPro;

public class ContadorFPS : MonoBehaviour
{
    [SerializeField] private TMP_Text textoFPS;
    [SerializeField] private float intervaloDeAtualizacao = 0.5f;

    private int quadros = 0;
    private float tempoAcumulado = 0f;

    void Update()
    {
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

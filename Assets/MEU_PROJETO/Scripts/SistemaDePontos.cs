using UnityEngine;
using TMPro;

public class SistemaDePontos : MonoBehaviour
{
    public static SistemaDePontos instancia;

    public int pontosAtuais;

    public TMP_Text textoPontos;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AtualizarTexto();
    }

    public void AdicionarPontos(int quantidade)
    {
        pontosAtuais += quantidade;
        AtualizarTexto();
    }

    public bool GastarPontos(int quantidade)
    {
        if (pontosAtuais < quantidade)
        {
            Debug.Log("Pontos insuficientes.");
            return false;
        }

        pontosAtuais -= quantidade;
        AtualizarTexto();
        return true;
    }

    void AtualizarTexto()
    {
        if (textoPontos != null)
        {
            textoPontos.text = "PONTOS: " + pontosAtuais;
        }
    }
}
using UnityEngine;
using TMPro;

// Coloque no prefab da estação de munição (fixa no mapa, NÃO é destruída ao usar).
// Precisa de um Collider marcado como "Is Trigger".
public class EstacaoDeMunicao : MonoBehaviour
{
    public int quantidadeDeMunicao = 30;
    public int custoEmPontos = 15;
    public KeyCode teclaDeUso = KeyCode.E;

    public GameObject painelDeTexto;
    public TMP_Text textoFlutuante;

    private Arma armaDoJogador;

    [Header("Sons")]
    public AudioClip somSemPontos;
    public AudioClip somDeCompra;
    private AudioSource fonteDeAudio;

    void Start()
    {
        fonteDeAudio = GetComponent<AudioSource>();

        if (fonteDeAudio == null)
        {
            fonteDeAudio = gameObject.AddComponent<AudioSource>();
        }

        fonteDeAudio.playOnAwake = false;
        fonteDeAudio.loop = false;
        fonteDeAudio.spatialBlend = 0f; // 2D: feedback sempre audível

        AtualizarTexto();

        if (painelDeTexto != null)
        {
            painelDeTexto.SetActive(false);
        }
    }

    void Update()
    {
        if (armaDoJogador == null)
        {
            return;
        }

        if (Input.GetKeyDown(teclaDeUso))
        {
            Usar();
        }
    }

    void OnTriggerEnter(Collider outro)
    {
        if (!outro.CompareTag("Jogador"))
        {
            return;
        }

        armaDoJogador = outro.GetComponent<Arma>();

        if (armaDoJogador == null)
        {
            armaDoJogador = outro.GetComponentInChildren<Arma>();
        }

        if (painelDeTexto != null)
        {
            painelDeTexto.SetActive(true);
        }
    }

    void OnTriggerExit(Collider outro)
    {
        if (!outro.CompareTag("Jogador"))
        {
            return;
        }

        armaDoJogador = null;

        if (painelDeTexto != null)
        {
            painelDeTexto.SetActive(false);
        }
    }

    void Usar()
    {
        if (!SistemaDePontos.instancia.GastarPontos(custoEmPontos))
        {
            Debug.Log("Pontos insuficientes pra usar a estação de munição.");
            TocarSom(somSemPontos);
            return;
        }

        armaDoJogador.AdicionarMunicao(quantidadeDeMunicao);
        TocarSom(somDeCompra);
    }

    void TocarSom(AudioClip som)
    {
        if (som != null && fonteDeAudio != null)
        {
            fonteDeAudio.PlayOneShot(som);
        }
    }

    void AtualizarTexto()
    {
        if (textoFlutuante == null)
        {
            return;
        }

        textoFlutuante.text =
            quantidadeDeMunicao + " de munição por " + custoEmPontos + " pontos\n[E] para usar";
    }
}
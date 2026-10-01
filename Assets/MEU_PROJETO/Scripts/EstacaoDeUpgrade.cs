using UnityEngine;
using TMPro;

// Coloque no prefab da estação de upgrade (fixa no mapa, NÃO é destruída ao usar).
// Precisa de um Collider marcado como "Is Trigger".
public class EstacaoDeUpgrade : MonoBehaviour
{
    [Header("Upgrade de Dano")]
    public int aumentoDeDano = 5;
    public int custoDoDano = 100;
    public KeyCode teclaDano = KeyCode.Alpha1;

    [Header("Upgrade de Capacidade do Pente")]
    public int aumentoDeCapacidade = 10;
    public int custoDaCapacidade = 80;
    public KeyCode teclaCapacidade = KeyCode.Alpha2;

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

        if (Input.GetKeyDown(teclaDano))
        {
            ComprarUpgradeDeDano();
        }

        if (Input.GetKeyDown(teclaCapacidade))
        {
            ComprarUpgradeDeCapacidade();
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

    void ComprarUpgradeDeDano()
    {
        if (!SistemaDePontos.instancia.GastarPontos(custoDoDano))
        {
            Debug.Log("Pontos insuficientes.");
            TocarSom(somSemPontos);
            return;
        }

        armaDoJogador.dano += aumentoDeDano;
        TocarSom(somDeCompra);
    }

    void ComprarUpgradeDeCapacidade()
    {
        if (!SistemaDePontos.instancia.GastarPontos(custoDaCapacidade))
        {
            Debug.Log("Pontos insuficientes.");
            TocarSom(somSemPontos);
            return;
        }

        armaDoJogador.AumentarCapacidadeDoPente(aumentoDeCapacidade);
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
            "[1] +" + aumentoDeDano + " de dano por " + custoDoDano + " pontos\n"
            + "[2] +" + aumentoDeCapacidade + " no pente por " + custoDaCapacidade + " pontos";
    }
}
using UnityEngine;
using TMPro;

// Coloque no prefab da estação de cura (fixa no mapa, NÃO é destruída ao usar).
// Precisa de um Collider marcado como "Is Trigger" (raio maior que o modelo,
// pra detectar quando o jogador chega perto).
public class EstacaoDeCura : MonoBehaviour
{
    public int quantidadeDeCura = 50;
    public int custoEmPontos = 20;
    public KeyCode teclaDeUso = KeyCode.E;

    // Arraste aqui o Canvas (World Space) filho que contém o texto.
    public GameObject painelDeTexto;
    public TMP_Text textoFlutuante;

    private JogadorVida jogadorPerto;

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
        if (jogadorPerto == null)
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

        jogadorPerto = outro.GetComponent<JogadorVida>();

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

        jogadorPerto = null;

        if (painelDeTexto != null)
        {
            painelDeTexto.SetActive(false);
        }
    }

    void Usar()
    {
        if (!SistemaDePontos.instancia.GastarPontos(custoEmPontos))
        {
            Debug.Log("Pontos insuficientes pra usar a estação de cura.");
            TocarSom(somSemPontos);
            return;
        }

        jogadorPerto.RecuperarVida(quantidadeDeCura);
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
            "Cura de " + quantidadeDeCura + " de vida por " + custoEmPontos + " pontos\n[E] para usar";
    }
}
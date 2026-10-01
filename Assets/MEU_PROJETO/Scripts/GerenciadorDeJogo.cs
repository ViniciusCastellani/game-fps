using UnityEngine;
using TMPro;

public class GerenciadorDeJogo : MonoBehaviour
{
    public enum EstadoDoJogo
    {
        Jogando,
        Pausado,
        Tutorial,
        GameOver,
        Vitoria
    }

    [Header("Telas")]
    [SerializeField] private GameObject telaDePause;
    [SerializeField] private GameObject telaDeGameOver;
    [SerializeField] private GameObject telaDeVitoria;
    [SerializeField] private GameObject telaDeTutorial;

    [Header("Textos que mudam durante o jogo")]
    [SerializeField] private TMP_Text textoMotivoGameOver;
    [SerializeField] private TMP_Text textoDescricaoVitoria;
    [SerializeField] private TMP_Text textoBotaoProximaFase;
    [SerializeField] private TMP_Text textoPontuacaoVitoria;
    [SerializeField] private TMP_Text textoPontuacaoGameOver;

    [Header("Tutorial")]
    [Tooltip("Abre a tela de comandos assim que a partida começa.")]
    [SerializeField] private bool mostrarTutorialAoIniciar = true;
    [Tooltip("Tecla que reabre a tela de comandos durante o jogo.")]
    [SerializeField] private KeyCode teclaDoTutorial = KeyCode.T;

    [Header("Referências")]
    [SerializeField] private GerenciadorDeOrdas gerenciadorDeOrdas;
    [SerializeField] private GerenciadorDeCenas gerenciadorDeCenas;

    [SerializeField] private Behaviour[] acoesDoJogador;

    [Header("Áudio (opcional)")]
    [SerializeField] private AudioSource fonteDeAudio;
    [SerializeField] private AudioClip somDeVitoria;
    [SerializeField] private AudioClip somDeGameOver;

    public EstadoDoJogo Estado { get; private set; }

    public bool EstaJogando
    {
        get { return Estado == EstadoDoJogo.Jogando; }
    }

    private EstadoDoJogo estadoAntesDoTutorial = EstadoDoJogo.Jogando;

    void Start()
    {
        bool comTutorial = mostrarTutorialAoIniciar && telaDeTutorial != null;

        MudarEstado(comTutorial ? EstadoDoJogo.Tutorial : EstadoDoJogo.Jogando);
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaDoTutorial))
        {
            AbrirTutorial();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (Estado == EstadoDoJogo.Jogando)
        {
            Pausar();
        }
        else if (Estado == EstadoDoJogo.Pausado)
        {
            Continuar();
        }
        else if (Estado == EstadoDoJogo.Tutorial)
        {
            FecharTutorial();
        }
    }

    public void AbrirTutorial()
    {
        if (telaDeTutorial == null)
        {
            return;
        }

        if (Estado != EstadoDoJogo.Jogando && Estado != EstadoDoJogo.Pausado)
        {
            return;
        }

        estadoAntesDoTutorial = Estado;
        MudarEstado(EstadoDoJogo.Tutorial);
    }

    public void FecharTutorial()
    {
        if (Estado == EstadoDoJogo.Tutorial)
        {
            MudarEstado(estadoAntesDoTutorial);
        }
    }

    public void Pausar()
    {
        if (Estado == EstadoDoJogo.Jogando)
        {
            MudarEstado(EstadoDoJogo.Pausado);
        }
    }

    public void Continuar()
    {
        if (Estado == EstadoDoJogo.Pausado)
        {
            MudarEstado(EstadoDoJogo.Jogando);
        }
    }

    public void GameOver(string motivo)
    {
        if (!EstaJogando)
        {
            return;
        }

        if (textoMotivoGameOver != null)
        {
            textoMotivoGameOver.text = motivo;
        }

        EscreverPontuacao(textoPontuacaoGameOver);

        MudarEstado(EstadoDoJogo.GameOver);
        TocarSom(somDeGameOver);

        if (MusicaDoJogo.instancia != null)
        {
            MusicaDoJogo.instancia.Parar();
        }
    }

    public void Vitoria()
    {
        if (!EstaJogando)
        {
            return;
        }

        if (gerenciadorDeOrdas.EhUltimaOrda)
        {
            textoDescricaoVitoria.text = "VOCÊ VENCEU!\nTodas as fases foram concluídas.";
            textoBotaoProximaFase.text = "Jogar Novamente";

            if (MusicaDoJogo.instancia != null)
            {
                MusicaDoJogo.instancia.Parar();
            }
        }
        else
        {
            textoDescricaoVitoria.text = "Fase " + gerenciadorDeOrdas.NumeroDaOrdaAtual + " concluída!";
            textoBotaoProximaFase.text = "Próxima Fase";
        }

        EscreverPontuacao(textoPontuacaoVitoria);

        MudarEstado(EstadoDoJogo.Vitoria);
        TocarSom(somDeVitoria);
    }

    public void ProximaFase()
    {
        if (Estado != EstadoDoJogo.Vitoria)
        {
            return;
        }

        PararSomDeFim();

        if (gerenciadorDeOrdas.EhUltimaOrda)
        {
            Time.timeScale = 1f;
            gerenciadorDeCenas.ReiniciarFase();
            return;
        }

        MudarEstado(EstadoDoJogo.Jogando);

        if (MusicaDoJogo.instancia != null)
        {
            MusicaDoJogo.instancia.GarantirTocando();
        }

        gerenciadorDeOrdas.IniciarProximaOrda();
    }

    public void ReiniciarFase()
    {
        PararSomDeFim();
        gerenciadorDeOrdas.ReiniciarNaOrdaAtual();
        Time.timeScale = 1f;
        gerenciadorDeCenas.ReiniciarFase();
    }
    public void VoltarAoMenu()
    {
        PararSomDeFim();
        Time.timeScale = 1f;
        gerenciadorDeCenas.VoltarParaTelaInicial();
    }

    void MudarEstado(EstadoDoJogo novoEstado)
    {
        Estado = novoEstado;

        bool jogando = novoEstado == EstadoDoJogo.Jogando;

        Time.timeScale = jogando ? 1f : 0f;

        Cursor.lockState = jogando ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !jogando;

        Mostrar(telaDePause, novoEstado == EstadoDoJogo.Pausado);
        Mostrar(telaDeGameOver, novoEstado == EstadoDoJogo.GameOver);
        Mostrar(telaDeVitoria, novoEstado == EstadoDoJogo.Vitoria);
        Mostrar(telaDeTutorial, novoEstado == EstadoDoJogo.Tutorial);

        foreach (Behaviour acao in acoesDoJogador)
        {
            if (acao != null)
            {
                acao.enabled = jogando;
            }
        }
    }
    void Mostrar(GameObject tela, bool visivel)
    {
        if (tela != null)
        {
            tela.SetActive(visivel);
        }
    }

    void EscreverPontuacao(TMP_Text destino)
    {
        if (destino == null)
        {
            return;
        }

        int pontos = SistemaDePontos.instancia != null ? SistemaDePontos.instancia.pontosAtuais : 0;

        destino.text = "PONTUAÇÃO FINAL: " + pontos;
    }

    void PararSomDeFim()
    {
        if (fonteDeAudio == null)
        {
            return;
        }

        if (MusicaDoJogo.instancia != null && MusicaDoJogo.instancia.fonteDeAudio == fonteDeAudio)
        {
            Debug.LogWarning("A fonte de áudio do GerenciadorDeJogo é a mesma da MusicaDoJogo. "
                + "Use um AudioSource separado para os sons de vitória/derrota.");
            return;
        }

        fonteDeAudio.Stop();
    }

    void TocarSom(AudioClip som)
    {
        if (fonteDeAudio != null && som != null)
        {
            fonteDeAudio.PlayOneShot(som);
        }
    }
}
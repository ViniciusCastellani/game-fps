using UnityEngine;
using TMPro;

// Coloque esse script em um GameObject vazio na cena de gameplay (ex: "GerenciadorDeJogo").
// É o ÚNICO script que altera Time.timeScale e o cursor durante o gameplay.
public class GerenciadorDeJogo : MonoBehaviour
{
    public enum EstadoDoJogo
    {
        Jogando,
        Pausado,
        GameOver,
        Vitoria
    }

    [Header("Telas")]
    [SerializeField] private GameObject telaDePause;
    [SerializeField] private GameObject telaDeGameOver;
    [SerializeField] private GameObject telaDeVitoria;

    [Header("Textos que mudam durante o jogo")]
    [SerializeField] private TMP_Text textoMotivoGameOver;
    [SerializeField] private TMP_Text textoDescricaoVitoria;
    [SerializeField] private TMP_Text textoBotaoProximaFase;

    [Header("Referências")]
    [SerializeField] private GerenciadorDeOrdas gerenciadorDeOrdas;
    [SerializeField] private GerenciadorDeCenas gerenciadorDeCenas;

    // Scripts do jogador (tiro, movimento, câmera, pulo, loja...) que ficam
    // desligados sempre que o estado não for "Jogando".
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

    void Start()
    {
        MudarEstado(EstadoDoJogo.Jogando);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        // Em Game Over ou Vitória o Escape não faz nada.
        if (Estado == EstadoDoJogo.Jogando)
        {
            Pausar();
        }
        else if (Estado == EstadoDoJogo.Pausado)
        {
            Continuar();
        }
    }

    public void Pausar()
    {
        if (Estado == EstadoDoJogo.Jogando)
        {
            MudarEstado(EstadoDoJogo.Pausado);
        }
    }

    // Botão "Continuar" da tela de pause.
    public void Continuar()
    {
        if (Estado == EstadoDoJogo.Pausado)
        {
            MudarEstado(EstadoDoJogo.Jogando);
        }
    }

    // Chamado pelo JogadorVida (vida zerada) e pelo Cronometro (tempo zerado).
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

        MudarEstado(EstadoDoJogo.GameOver);
        TocarSom(somDeGameOver);
    }

    // Chamado pelo GerenciadorDeOrdas quando todos os inimigos da orda morrem.
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
        }
        else
        {
            textoDescricaoVitoria.text = "Fase " + gerenciadorDeOrdas.NumeroDaOrdaAtual + " concluída!";
            textoBotaoProximaFase.text = "Próxima Fase";
        }

        MudarEstado(EstadoDoJogo.Vitoria);
        TocarSom(somDeVitoria);
    }

    // Botão "Próxima Fase" da tela de vitória.
    public void ProximaFase()
    {
        if (Estado != EstadoDoJogo.Vitoria)
        {
            return;
        }

        if (gerenciadorDeOrdas.EhUltimaOrda)
        {
            // Depois da última fase, recomeça da Fase 1.
            Time.timeScale = 1f;
            gerenciadorDeCenas.ReiniciarFase();
            return;
        }

        MudarEstado(EstadoDoJogo.Jogando);
        gerenciadorDeOrdas.IniciarProximaOrda();
    }

    // Botão "Reiniciar" da tela de Game Over: recarrega a cena na mesma fase.
    public void ReiniciarFase()
    {
        gerenciadorDeOrdas.ReiniciarNaOrdaAtual();
        Time.timeScale = 1f;
        gerenciadorDeCenas.ReiniciarFase();
    }

    // Botões "Sair para o Menu" / "Menu Principal".
    public void VoltarAoMenu()
    {
        Time.timeScale = 1f;
        gerenciadorDeCenas.VoltarParaTelaInicial();
    }

    void MudarEstado(EstadoDoJogo novoEstado)
    {
        Estado = novoEstado;

        bool jogando = novoEstado == EstadoDoJogo.Jogando;

        // Com timeScale = 0 a física, o NavMesh dos inimigos, o Time.deltaTime
        // e os WaitForSeconds param.
        Time.timeScale = jogando ? 1f : 0f;

        Cursor.lockState = jogando ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !jogando;

        telaDePause.SetActive(novoEstado == EstadoDoJogo.Pausado);
        telaDeGameOver.SetActive(novoEstado == EstadoDoJogo.GameOver);
        telaDeVitoria.SetActive(novoEstado == EstadoDoJogo.Vitoria);

        // timeScale = 0 não bloqueia Input.GetButtonDown, então desligamos
        // os scripts do jogador para ele não atirar/pular com o jogo parado.
        foreach (Behaviour acao in acoesDoJogador)
        {
            if (acao != null)
            {
                acao.enabled = jogando;
            }
        }
    }

    void TocarSom(AudioClip som)
    {
        if (fonteDeAudio != null && som != null)
        {
            fonteDeAudio.PlayOneShot(som);
        }
    }
}

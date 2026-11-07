using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class GerenciadorModoInfinito : MonoBehaviour
{
    public const string TextoObjetivoInfinito = "<b><color=#FFC629>OBJETIVO</color></b>   Elimine todos os inimigos de cada horda para avançar. As hordas não têm limite; a partida termina quando sua vida chega a zero.\nGaste pontos nas estações. O resultado soma todos os pontos conquistados, mesmo os gastos, os abates e as hordas concluídas.";

    [SerializeField] private string nomeDoCenario = "Estádio";

    [Header("Referências")]
    [SerializeField] private GerenciadorDeJogo gerenciadorDeJogo;
    [SerializeField] private GerenciadorDeOrdas gerenciadorDeOrdas;
    [SerializeField] private Cronometro cronometro;
    [SerializeField] private GerenciadorDeCenas gerenciadorDeCenas;
    [SerializeField] private GeradorDeInimigosInfinito gerador;

    [Header("HUD")]
    [SerializeField] private GameObject hudInfinito;
    [SerializeField] private TMP_Text textoTempoSobrevivencia;
    [SerializeField] private TMP_Text textoAbates;
    [SerializeField] private TMP_Text textoHorda;
    [SerializeField] private GameObject[] objetosSomenteModoNormal;

    [Header("Resultado")]
    [SerializeField] private GameObject telaDeResultado;
    [SerializeField] private TMP_Text textoResultadoCenario;
    [SerializeField] private TMP_Text textoResultadoPontos;
    [SerializeField] private TMP_Text textoResultadoAbates;
    [SerializeField] private TMP_Text textoResultadoTempo;
    [SerializeField] private TMP_Text textoResultadoHordas;
    [SerializeField] private TMP_Text textoResultadoSaldo;
    [SerializeField] private Button botaoTentarNovamente;

    [Header("Tutorial")]
    [SerializeField] private TMP_Text textoObjetivoTutorial;

    public bool Ativo { get; private set; }
    public bool SobrevivenciaIniciada { get; private set; }
    public bool Terminou { get; private set; }
    public float TempoDeSobrevivencia { get; private set; }
    public int InimigosAbatidos { get; private set; }
    public int HordasSobrevividas => gerador != null ? gerador.HordasSobrevividas : 0;
    public int PontosDisponiveis => SistemaDePontos.instancia != null ? SistemaDePontos.instancia.pontosAtuais : 0;

    public int PontosConquistados
    {
        get { return SistemaDePontos.instancia != null ? SistemaDePontos.instancia.PontosTotaisConquistados : 0; }
    }

    public string NomeDoCenario
    {
        get { return nomeDoCenario; }
    }

    private int ultimoSegundoExibido = -1;
    private int ultimoAbateExibido = -1;
    private string ultimaHordaExibida;

    public static string FormatarTempo(float segundos)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(segundos));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    void Awake()
    {
        Ativo = SessaoDeJogo.EhInfinitoNaCena(gameObject.scene.name);

        if (Ativo)
        {
            string campoAusente = PrimeiroCampoAusente();
            if (campoAusente != null)
            {
                Debug.LogError("Modo infinito sem referencia: " + campoAusente, this);
                Ativo = false;
            }
        }

        if (!Ativo)
        {
            if (hudInfinito != null) hudInfinito.SetActive(false);
            if (telaDeResultado != null) telaDeResultado.SetActive(false);
            if (gerador != null) gerador.enabled = false;
            enabled = false;
            return;
        }

        gerenciadorDeOrdas.enabled = false;
        cronometro.enabled = false;

        foreach (GameObject objeto in objetosSomenteModoNormal)
        {
            if (objeto != null) objeto.SetActive(false);
        }

        gerenciadorDeJogo.DefinirTutorialAoIniciar(true);
        gerenciadorDeJogo.SubstituirTelaDeGameOver(telaDeResultado);

        if (textoObjetivoTutorial != null)
        {
            textoObjetivoTutorial.text = TextoObjetivoInfinito;
        }

        hudInfinito.SetActive(true);
        telaDeResultado.SetActive(false);

        gerador.AoAbaterInimigo += RegistrarAbate;
        gerenciadorDeJogo.AoMudarEstado += AoMudarEstado;

        AtualizarHUD(true);
    }

    void OnDestroy()
    {
        if (gerador != null) gerador.AoAbaterInimigo -= RegistrarAbate;
        if (gerenciadorDeJogo != null) gerenciadorDeJogo.AoMudarEstado -= AoMudarEstado;
    }

    void Update()
    {
        if (Terminou)
        {
            ManterFocoNoBotao();
            return;
        }

        if (!gerenciadorDeJogo.EstaJogando)
        {
            return;
        }

        if (!SobrevivenciaIniciada)
        {
            SobrevivenciaIniciada = true;
            gerador.Iniciar();
        }

        TempoDeSobrevivencia += Time.deltaTime;
        gerador.Avancar(Time.deltaTime, TempoDeSobrevivencia);
        AtualizarHUD(false);
    }

    public void TentarNovamente()
    {
        GerenciadorDeOrdas.LimparReinicio();
        Time.timeScale = 1f;
        gerenciadorDeCenas.ReiniciarFase();
    }

    public void EscolherCenario()
    {
        gerenciadorDeCenas.VoltarParaEscolhaDeCenario();
    }

    public void VoltarAoMenuInicial()
    {
        gerenciadorDeCenas.VoltarParaTelaInicial();
    }

    void RegistrarAbate()
    {
        if (Terminou)
        {
            return;
        }

        InimigosAbatidos++;
        AtualizarHUD(false);
    }

    void AoMudarEstado(GerenciadorDeJogo.EstadoDoJogo estado)
    {
        if (estado != GerenciadorDeJogo.EstadoDoJogo.GameOver || Terminou)
        {
            return;
        }

        Terminou = true;
        gerador.Parar();

        textoResultadoCenario.text = nomeDoCenario;
        textoResultadoPontos.text = PontosConquistados.ToString();
        textoResultadoAbates.text = InimigosAbatidos.ToString();
        textoResultadoTempo.text = FormatarTempo(TempoDeSobrevivencia);
        textoResultadoHordas.text = HordasSobrevividas.ToString();
        textoResultadoSaldo.text = PontosDisponiveis.ToString();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(botaoTentarNovamente.gameObject);
        }
    }

    void ManterFocoNoBotao()
    {
        EventSystem sistema = EventSystem.current;
        if (sistema == null)
        {
            return;
        }

        GameObject selecionado = sistema.currentSelectedGameObject;
        if (selecionado == null || !selecionado.activeInHierarchy)
        {
            sistema.SetSelectedGameObject(botaoTentarNovamente.gameObject);
        }
    }

    void AtualizarHUD(bool forcar)
    {
        int segundo = Mathf.FloorToInt(TempoDeSobrevivencia);
        string horda = gerador.EmIntervalo
            ? "HORDA CONCLUÍDA! PRÓXIMA EM " + Mathf.CeilToInt(gerador.IntervaloRestante) + "s"
            : "HORDA " + Mathf.Max(1, gerador.NumeroDaHordaAtual) + "  |  RESTANTES: " + gerador.InimigosRestantesNaHorda;
        if (!forcar && segundo == ultimoSegundoExibido && InimigosAbatidos == ultimoAbateExibido && horda == ultimaHordaExibida)
        {
            return;
        }

        ultimoSegundoExibido = segundo;
        ultimoAbateExibido = InimigosAbatidos;
        ultimaHordaExibida = horda;

        textoTempoSobrevivencia.text = "TEMPO: " + FormatarTempo(TempoDeSobrevivencia);
        textoAbates.text = "ABATES: " + InimigosAbatidos;
        textoHorda.text = horda;
    }

    string PrimeiroCampoAusente()
    {
        if (gerenciadorDeJogo == null) return "gerenciadorDeJogo";
        if (gerenciadorDeOrdas == null) return "gerenciadorDeOrdas";
        if (cronometro == null) return "cronometro";
        if (gerenciadorDeCenas == null) return "gerenciadorDeCenas";
        if (gerador == null) return "gerador";
        if (hudInfinito == null) return "hudInfinito";
        if (telaDeResultado == null) return "telaDeResultado";
        if (textoResultadoCenario == null) return "textoResultadoCenario";
        if (textoResultadoPontos == null) return "textoResultadoPontos";
        if (textoResultadoAbates == null) return "textoResultadoAbates";
        if (textoResultadoTempo == null) return "textoResultadoTempo";
        if (textoResultadoHordas == null) return "textoResultadoHordas";
        if (textoResultadoSaldo == null) return "textoResultadoSaldo";
        if (textoTempoSobrevivencia == null) return "textoTempoSobrevivencia";
        if (textoAbates == null) return "textoAbates";
        if (textoHorda == null) return "textoHorda";
        if (botaoTentarNovamente == null) return "botaoTentarNovamente";
        return null;
    }
}

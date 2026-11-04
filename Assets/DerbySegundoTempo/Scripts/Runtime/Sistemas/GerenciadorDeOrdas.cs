using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.AI;

public class GerenciadorDeOrdas : MonoBehaviour
{
    [System.Serializable]
    public class Orda
    {
        public GameObject[] prefabsInimigos;
    }

    public Orda[] ordas;

    public Transform[] pontosDeSpawn;

    [Tooltip("Agrupa os inimigos criados durante a partida na hierarquia.")]
    public Transform inimigosAtivos;

    public TMP_Text textoOrda;

    public float tempoEntreOrdas = 5f;

    [Header("Iluminacao (opcional)")]
    public IluminacaoPorOrda iluminacao;
    private bool iniciouAlgumaOrda;

    [SerializeField] private GerenciadorDeJogo gerenciadorDeJogo;
    [SerializeField] private Cronometro cronometro;

    private static int ordaParaIniciar = 0;
    private static string cenaParaReiniciar;

    private int ordaAtual = 0;
    private int inimigosVivos = 0;

    public int NumeroDaOrdaAtual
    {
        get { return ordaAtual + 1; }
    }

    public bool EhUltimaOrda
    {
        get { return ordaAtual >= ordas.Length - 1; }
    }

    void Start()
    {
        ordaAtual = cenaParaReiniciar == gameObject.scene.path
            ? Mathf.Clamp(ordaParaIniciar, 0, ordas.Length - 1) : 0;
        LimparReinicio();

        IniciarOrda(ordaAtual);
    }

    public void ReiniciarNaOrdaAtual()
    {
        ordaParaIniciar = ordaAtual;
        cenaParaReiniciar = gameObject.scene.path;
    }

    public static void LimparReinicio()
    {
        ordaParaIniciar = 0;
        cenaParaReiniciar = null;
    }

    public void IniciarProximaOrda()
    {
        ordaAtual++;
        if (iluminacao != null) iluminacao.AplicarOrda(NumeroDaOrdaAtual);
        StartCoroutine(EsperarProximaOrda());
    }

    void IniciarOrda(int indiceOrda)
    {
        if (iluminacao != null) iluminacao.AplicarOrda(indiceOrda + 1, !iniciouAlgumaOrda);
        iniciouAlgumaOrda = true;
        AtualizarTexto("ORDA " + (indiceOrda + 1) + " / " + ordas.Length);
        cronometro.Iniciar();

        Orda orda = ordas[indiceOrda];
        inimigosVivos = orda.prefabsInimigos.Length;

        foreach (GameObject prefabInimigo in orda.prefabsInimigos)
        {
            Transform pontoDeSpawn = pontosDeSpawn[Random.Range(0, pontosDeSpawn.Length)];
            Vector3 posicao = pontoDeSpawn.position;
            var agenteDoPrefab = prefabInimigo.GetComponent<NavMeshAgent>();
            if (agenteDoPrefab != null)
            {
                var filtro = new NavMeshQueryFilter
                {
                    agentTypeID = agenteDoPrefab.agentTypeID,
                    areaMask = agenteDoPrefab.areaMask
                };
                if (NavMesh.SamplePosition(posicao, out var localNavegavel, 2f, filtro))
                    posicao = localNavegavel.position;
                else
                {
                    Debug.LogError("Spawn sem navegacao para este inimigo: " + pontoDeSpawn.name, pontoDeSpawn);
                    continue;
                }
            }

            GameObject inimigoInstanciado = Instantiate(
                prefabInimigo,
                posicao,
                pontoDeSpawn.rotation,
                inimigosAtivos
            );

            InimigoVida vidaDoInimigo = inimigoInstanciado.GetComponent<InimigoVida>();

            if (vidaDoInimigo != null)
            {
                vidaDoInimigo.gerenciadorDeOrdas = this;
            }
        }
    }

    public void InimigoMorreu()
    {
        inimigosVivos--;

        if (inimigosVivos <= 0)
        {
            cronometro.Parar();
            gerenciadorDeJogo.Vitoria();
        }
    }

    IEnumerator EsperarProximaOrda()
    {
        AtualizarTexto("PREPARE-SE! ORDA " + (ordaAtual + 1) + " em " + tempoEntreOrdas + "s...");

        yield return new WaitForSeconds(tempoEntreOrdas);

        IniciarOrda(ordaAtual);
    }

    void AtualizarTexto(string texto)
    {
        if (textoOrda != null)
        {
            textoOrda.text = texto;
        }
    }
}

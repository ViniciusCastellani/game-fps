using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GeradorDeInimigosInfinito : MonoBehaviour
{
    [Header("Inimigos")]
    [SerializeField] private GameObject[] prefabsComuns;
    [SerializeField] private GameObject prefabMiniBoss;

    [Header("Ritmo")]
    [SerializeField, Min(0.25f)] private float intervaloInicial = 3f;
    [SerializeField, Min(0.25f)] private float intervaloMinimo = 1.2f;
    [SerializeField, Min(0f)] private float reducaoDoIntervaloPorMinuto = 0.6f;
    [SerializeField, Min(0)] private int inimigosNoInicio = 3;

    [Header("Hordas infinitas")]
    [SerializeField, Min(1)] private int inimigosPrimeiraHorda = 6;
    [SerializeField, Min(0)] private int aumentoPorHorda = 2;
    [SerializeField, Min(0f)] private float tempoEntreHordas = 5f;

    [Header("Limites")]
    [SerializeField, Range(1, 30)] private int maximoDeInimigosVivos = 12;
    [SerializeField, Min(1f)] private float intervaloDoMiniBoss = 90f;

    [Header("Spawn")]
    [SerializeField] private GerenciadorDeOrdas origemDosPontos;
    [SerializeField] private Transform jogador;
    [SerializeField, Min(0f)] private float distanciaMinimaDoJogador = 8f;
    [SerializeField, Min(0.1f)] private float raioDeBuscaNaNavMesh = 2f;

    public event System.Action AoAbaterInimigo;

    public bool Ativo { get; private set; }
    public int NumeroDaHordaAtual { get; private set; }
    public int HordasSobrevividas { get; private set; }
    public bool EmIntervalo { get; private set; }
    public float IntervaloRestante { get; private set; }
    public int InimigosRestantesNaHorda => Mathf.Max(0, quantidadeDaHorda - criadosNaHorda) + InimigosVivos;

    public int InimigosVivos
    {
        get
        {
            PodarInimigos();
            return inimigos.Count;
        }
    }

    public bool MiniBossVivo
    {
        get { return miniBoss != null; }
    }

    private readonly List<InimigoVida> inimigos = new List<InimigoVida>();
    private InimigoVida miniBoss;
    private float acumulador;
    private float proximoMiniBossEm;
    private int quantidadeDaHorda;
    private int criadosNaHorda;

    void OnValidate()
    {
        intervaloInicial = Mathf.Max(intervaloInicial, intervaloMinimo);
    }

    void Awake()
    {
        if (jogador == null)
        {
            GameObject objetoDoJogador = GameObject.FindWithTag("Jogador");
            if (objetoDoJogador != null) jogador = objetoDoJogador.transform;
        }
    }

    public float IntervaloAtual(float tempoDeSobrevivencia)
    {
        return Mathf.Max(intervaloMinimo, intervaloInicial - reducaoDoIntervaloPorMinuto * tempoDeSobrevivencia / 60f);
    }

    public void Iniciar()
    {
        if (Ativo) return;
        if (!TemPrefabComum())
        {
            Debug.LogError("GeradorDeInimigosInfinito sem prefabs comuns", this);
            Ativo = false;
            return;
        }

        Ativo = true;
        acumulador = 0f;
        proximoMiniBossEm = intervaloDoMiniBoss;
        HordasSobrevividas = 0;
        NumeroDaHordaAtual = 0;
        IniciarHorda();
    }

    void IniciarHorda()
    {
        NumeroDaHordaAtual++;
        quantidadeDaHorda = Mathf.Max(1, inimigosPrimeiraHorda + (NumeroDaHordaAtual - 1) * aumentoPorHorda);
        criadosNaHorda = 0;
        acumulador = 0f;
        EmIntervalo = false;
        IntervaloRestante = 0f;

        int quantidadeInicial = Mathf.Min(inimigosNoInicio, maximoDeInimigosVivos - InimigosVivos, quantidadeDaHorda);
        for (int i = 0; i < quantidadeInicial; i++)
        {
            TentarCriar(SortearPrefabComum());
        }
    }

    public void Parar()
    {
        Ativo = false;
    }

    public void Avancar(float deltaTime, float tempoDeSobrevivencia)
    {
        if (!Ativo || deltaTime <= 0f)
        {
            return;
        }

        PodarInimigos();

        if (EmIntervalo)
        {
            IntervaloRestante = Mathf.Max(0f, IntervaloRestante - deltaTime);
            if (IntervaloRestante <= 0f) IniciarHorda();
            return;
        }

        VerificarFimDaHorda();
        if (EmIntervalo || criadosNaHorda >= quantidadeDaHorda) return;

        if (prefabMiniBoss != null
            && tempoDeSobrevivencia >= proximoMiniBossEm
            && !MiniBossVivo
            && inimigos.Count < maximoDeInimigosVivos)
        {
            InimigoVida criado = TentarCriar(prefabMiniBoss);
            if (criado != null)
            {
                proximoMiniBossEm = tempoDeSobrevivencia + intervaloDoMiniBoss;
                miniBoss = criado;
            }
        }

        acumulador += deltaTime;
        if (acumulador >= IntervaloAtual(tempoDeSobrevivencia))
        {
            acumulador = 0f;
            if (inimigos.Count < maximoDeInimigosVivos && criadosNaHorda < quantidadeDaHorda)
            {
                TentarCriar(SortearPrefabComum());
            }
        }
    }

    void VerificarFimDaHorda()
    {
        if (!Ativo || EmIntervalo || criadosNaHorda < quantidadeDaHorda || inimigos.Count > 0) return;
        HordasSobrevividas++;
        EmIntervalo = true;
        IntervaloRestante = tempoEntreHordas;
    }

    bool TemPrefabComum()
    {
        if (prefabsComuns == null) return false;

        foreach (GameObject prefab in prefabsComuns)
        {
            if (prefab != null) return true;
        }

        return false;
    }

    GameObject SortearPrefabComum()
    {
        var validos = new List<GameObject>();
        foreach (GameObject prefab in prefabsComuns)
        {
            if (prefab != null) validos.Add(prefab);
        }

        return validos[Random.Range(0, validos.Count)];
    }

    void PodarInimigos()
    {
        inimigos.RemoveAll(inimigo => inimigo == null);
    }

    InimigoVida TentarCriar(GameObject prefab)
    {
        if (prefab == null || origemDosPontos == null || origemDosPontos.pontosDeSpawn == null)
        {
            return null;
        }

        List<Transform> candidatos = ListarCandidatos();
        if (candidatos.Count == 0)
        {
            Debug.LogWarning("Nenhum ponto de spawn navegavel para " + prefab.name, this);
            return null;
        }

        var agenteDoPrefab = prefab.GetComponent<NavMeshAgent>();
        int inicio = Random.Range(0, candidatos.Count);

        for (int i = 0; i < candidatos.Count; i++)
        {
            Transform ponto = candidatos[(inicio + i) % candidatos.Count];
            Vector3 posicao = ponto.position;

            if (agenteDoPrefab != null)
            {
                var filtro = new NavMeshQueryFilter
                {
                    agentTypeID = agenteDoPrefab.agentTypeID,
                    areaMask = agenteDoPrefab.areaMask
                };

                if (!NavMesh.SamplePosition(ponto.position, out NavMeshHit hit, raioDeBuscaNaNavMesh, filtro))
                {
                    continue;
                }

                posicao = hit.position;
            }

            return Criar(prefab, posicao, ponto.rotation);
        }

        Debug.LogWarning("Nenhum ponto de spawn navegavel para " + prefab.name, this);
        return null;
    }

    List<Transform> ListarCandidatos()
    {
        var todos = new List<Transform>();
        var distantes = new List<Transform>();

        foreach (Transform ponto in origemDosPontos.pontosDeSpawn)
        {
            if (ponto == null) continue;

            todos.Add(ponto);
            if (jogador != null
                && Vector3.Distance(ponto.position, jogador.position) >= distanciaMinimaDoJogador)
            {
                distantes.Add(ponto);
            }
        }

        return distantes.Count > 0 ? distantes : todos;
    }

    InimigoVida Criar(GameObject prefab, Vector3 posicao, Quaternion rotacao)
    {
        GameObject instancia = Instantiate(prefab, posicao, rotacao, origemDosPontos.inimigosAtivos);
        InimigoVida vida = instancia.GetComponent<InimigoVida>();

        if (vida == null)
        {
            Debug.LogWarning("Prefab sem InimigoVida: " + prefab.name, this);
            Destroy(instancia);
            return null;
        }

        vida.gerenciadorDeOrdas = null;
        vida.aoMorrer += () => AoInimigoMorrer(vida);
        inimigos.Add(vida);
        criadosNaHorda++;
        return vida;
    }

    void AoInimigoMorrer(InimigoVida vida)
    {
        inimigos.Remove(vida);
        if (miniBoss == vida) miniBoss = null;

        AoAbaterInimigo?.Invoke();
        VerificarFimDaHorda();
    }
}

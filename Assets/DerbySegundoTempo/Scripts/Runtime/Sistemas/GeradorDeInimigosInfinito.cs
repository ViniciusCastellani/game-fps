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
        if (!TemPrefabComum())
        {
            Debug.LogError("GeradorDeInimigosInfinito sem prefabs comuns", this);
            Ativo = false;
            return;
        }

        Ativo = true;
        acumulador = 0f;
        proximoMiniBossEm = intervaloDoMiniBoss;

        int quantidadeInicial = Mathf.Min(inimigosNoInicio, maximoDeInimigosVivos);
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
            if (inimigos.Count < maximoDeInimigosVivos)
            {
                TentarCriar(SortearPrefabComum());
            }
        }
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

        vida.aoMorrer = () => AoInimigoMorrer(vida);
        inimigos.Add(vida);
        return vida;
    }

    void AoInimigoMorrer(InimigoVida vida)
    {
        inimigos.Remove(vida);
        if (miniBoss == vida) miniBoss = null;

        AoAbaterInimigo?.Invoke();
    }
}

using UnityEngine;
using UnityEngine.AI;

public class InimigoSomDePassos : MonoBehaviour
{
    [Header("Sons")]
    [Tooltip("Um ou mais sons de passo. Se tiver vários, sorteia um a cada passo.")]
    public AudioClip[] sonsDePasso;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Ritmo")]
    [Tooltip("Tempo entre um passo e outro, em segundos.")]
    public float intervaloEntrePassos = 0.5f;
    [Tooltip("Abaixo dessa velocidade o inimigo é considerado parado.")]
    public float velocidadeMinima = 0.3f;

    [Header("Som 3D")]
    public float distanciaMinima = 2f;
    public float distanciaMaxima = 25f;

    private NavMeshAgent agente;
    private InimigoAtaque ataque;
    private AudioSource fonte;
    private float proximoPassoEm;
    private int ultimoSorteado = -1;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        ataque = GetComponent<InimigoAtaque>();

        GameObject filho = new GameObject("SomDePassos");
        filho.transform.SetParent(transform, false);

        fonte = filho.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 1f;
        fonte.rolloffMode = AudioRolloffMode.Linear;
        fonte.minDistance = distanciaMinima;
        fonte.maxDistance = distanciaMaxima;
    }

    void Update()
    {
        if (ataque != null && ataque.Atacando)
        {
            if (fonte.isPlaying)
            {
                fonte.Stop();
            }
            return;
        }

        if (!EstaAndando())
        {
            return;
        }

        if (Time.time >= proximoPassoEm)
        {
            TocarPasso();
            proximoPassoEm = Time.time + intervaloEntrePassos;
        }
    }

    bool EstaAndando()
    {
        if (agente == null || !agente.isActiveAndEnabled || !agente.isOnNavMesh)
        {
            return false;
        }

        if (agente.isStopped)
        {
            return false;
        }

        return agente.velocity.magnitude > velocidadeMinima;
    }

    void TocarPasso()
    {
        if (sonsDePasso == null || sonsDePasso.Length == 0)
        {
            return;
        }

        int indice = Random.Range(0, sonsDePasso.Length);

        if (sonsDePasso.Length > 1 && indice == ultimoSorteado)
        {
            indice = (indice + 1) % sonsDePasso.Length;
        }

        ultimoSorteado = indice;

        if (sonsDePasso[indice] != null)
        {
            fonte.PlayOneShot(sonsDePasso[indice], volume);
        }
    }
}

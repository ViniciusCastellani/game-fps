using UnityEngine;

// Coloque no Jogador (junto do Rigidbody, JogadorMovimento e JogadorPulo).
// Só LÊ os dados do movimento: não altera nenhuma lógica existente.
[RequireComponent(typeof(Rigidbody))]
public class JogadorSons : MonoBehaviour
{
    [Header("Sons")]
    public AudioClip[] passosAndando;
    public AudioClip[] passosCorrendo;
    public AudioClip somDePulo;
    [Range(0f, 1f)] public float volume = 0.7f;

    [Header("Ritmo dos passos (segundos)")]
    public float intervaloAndando = 0.5f;
    public float intervaloCorrendo = 0.32f;
    [Tooltip("Abaixo dessa velocidade horizontal o jogador é considerado parado.")]
    public float velocidadeMinima = 0.5f;

    private Rigidbody rb;
    private JogadorPulo pulo;
    private AudioSource fonte;
    private bool estavaNoChao = true;
    private float proximoPassoEm;
    private int ultimoSorteado = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pulo = GetComponent<JogadorPulo>();

        // Fonte própria (num filho) em 2D: som do próprio jogador, sem distância.
        GameObject filho = new GameObject("SomDoJogador");
        filho.transform.SetParent(transform, false);

        fonte = filho.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 0f;
    }

    void Update()
    {
        bool noChao = pulo != null ? pulo.estaNoChao : true;

        // PULO: apertou o botão enquanto estava no chão (no frame anterior).
        // Usa o estado do frame anterior porque o JogadorPulo já pode ter
        // marcado estaNoChao = false neste mesmo frame.
        if (Input.GetButtonDown("Jump") && estavaNoChao)
        {
            Tocar(somDePulo);
        }

        // PASSOS: só no chão e só se estiver realmente se movendo.
        Vector3 v = rb.linearVelocity;
        float velocidadeHorizontal = new Vector2(v.x, v.z).magnitude;

        if (noChao && velocidadeHorizontal > velocidadeMinima && Time.time >= proximoPassoEm)
        {
            bool correndo = Input.GetButton("Fire3"); // Shift, igual ao JogadorMovimento

            TocarPasso(correndo ? passosCorrendo : passosAndando);
            proximoPassoEm = Time.time + (correndo ? intervaloCorrendo : intervaloAndando);
        }

        estavaNoChao = noChao;
    }

    void TocarPasso(AudioClip[] sons)
    {
        if (sons == null || sons.Length == 0)
        {
            return;
        }

        int indice = Random.Range(0, sons.Length);

        if (sons.Length > 1 && indice == ultimoSorteado)
        {
            indice = (indice + 1) % sons.Length;
        }

        ultimoSorteado = indice;
        Tocar(sons[indice]);
    }

    void Tocar(AudioClip som)
    {
        if (som != null)
        {
            fonte.PlayOneShot(som, volume);
        }
    }
}

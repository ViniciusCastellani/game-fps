using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class BolaFutebol : MonoBehaviour
{
    [Header("Toque do jogador")]
    [Min(0f)] public float multiplicadorAndando = 0.85f;
    [Min(0f)] public float multiplicadorCorrendo = 1.3f;
    [Min(0.1f)] public float velocidadeMaxima = 12f;
    [Min(0.01f)] public float intervaloEntreToques = 0.12f;

    [Header("Rolagem no gramado")]
    [Min(0f)] public float desaceleracaoNoSolo = 0.8f;
    [Min(0f)] public float velocidadeMinimaDoJogador = 0.15f;

    private Rigidbody corpo;
    private SphereCollider esfera;
    private Vector3 posicaoInicial;
    private float proximoToque;
    private float ultimoContatoSolo = float.NegativeInfinity;
    public bool ComemorandoGol { get; private set; }

    public bool SuspenderParaGol()
    {
        if (ComemorandoGol) return false;
        ComemorandoGol = true;
        corpo.linearVelocity = Vector3.zero;
        corpo.angularVelocity = Vector3.zero;
        corpo.isKinematic = true;
        corpo.detectCollisions = false;
        return true;
    }

    public void Reiniciar(Vector3 posicao)
    {
        corpo.position = posicao;
        corpo.rotation = Quaternion.identity;
        corpo.isKinematic = false;
        corpo.detectCollisions = true;
        corpo.linearVelocity = Vector3.zero;
        corpo.angularVelocity = Vector3.zero;
        ultimoContatoSolo = float.NegativeInfinity;
        proximoToque = Time.fixedTime + intervaloEntreToques;
        ComemorandoGol = false;
        corpo.WakeUp();
    }

    void Awake()
    {
        corpo = GetComponent<Rigidbody>();
        esfera = GetComponent<SphereCollider>();
        corpo.maxAngularVelocity = 100f;
        posicaoInicial = corpo.position;
    }

    void FixedUpdate()
    {
        if (ComemorandoGol) return;
        // A bola volta ao ponto inicial caso caia para fora da geometria do estadio.
        if (corpo.position.y < posicaoInicial.y - 12f)
        {
            Reiniciar(posicaoInicial);
            return;
        }

        Vector3 velocidade = corpo.linearVelocity;
        Vector3 horizontal = Vector3.ProjectOnPlane(velocidade, Vector3.up);
        horizontal = Vector3.ClampMagnitude(horizontal, velocidadeMaxima);
        if (Time.fixedTime - ultimoContatoSolo <= Time.fixedDeltaTime * 2.5f)
        {
            // Atrito de rolamento: uma esfera ideal continuaria rolando por tempo demais.
            Vector3 antes = horizontal;
            horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, desaceleracaoNoSolo * Time.fixedDeltaTime);
            if (antes.sqrMagnitude > 0.0001f)
                corpo.angularVelocity *= horizontal.magnitude / antes.magnitude;
        }
        corpo.linearVelocity = new Vector3(horizontal.x, velocidade.y, horizontal.z);
    }

    void OnCollisionEnter(Collision contato) { ProcessarContato(contato); }
    void OnCollisionStay(Collision contato) { ProcessarContato(contato); }

    void ProcessarContato(Collision contato)
    {
        if (ComemorandoGol) return;
        Rigidbody jogador = contato.rigidbody;
        if (jogador == null || !jogador.CompareTag("Jogador"))
        {
            for (int i = 0; i < contato.contactCount; i++)
                if (contato.GetContact(i).normal.y > 0.55f) ultimoContatoSolo = Time.fixedTime;
            return;
        }

        if (Time.fixedTime < proximoToque ||
            !jogador.TryGetComponent<JogadorMovimento>(out var movimento)) return;

        Vector3 velocidadeJogador = Vector3.ProjectOnPlane(movimento.VelocidadeHorizontalPlanejada, Vector3.up);
        if (velocidadeJogador.magnitude < velocidadeMinimaDoJogador) return;

        Vector3 paraBola = Vector3.ProjectOnPlane(corpo.worldCenterOfMass - jogador.worldCenterOfMass, Vector3.up).normalized;
        // Encostar de costas enquanto se afasta nao deve puxar a bola junto do jogador.
        if (Vector3.Dot(velocidadeJogador, paraBola) <= velocidadeMinimaDoJogador) return;

        bool correndo = movimento.velocidade > movimento.velocidadeAndar + 0.01f;
        float multiplicador = correndo ? multiplicadorCorrendo : multiplicadorAndando;
        Vector3 direcao = Vector3.Slerp(paraBola, velocidadeJogador.normalized, 0.7f).normalized;
        float alvo = Mathf.Min(velocidadeJogador.magnitude * multiplicador, velocidadeMaxima);
        float atual = Vector3.Dot(corpo.linearVelocity, direcao);
        if (atual >= alvo) return;

        // Completa o impulso ate a velocidade desejada, sem acumular forca a cada contato.
        corpo.AddForce(direcao * (alvo - atual), ForceMode.VelocityChange);
        float raio = esfera.radius * Mathf.Abs(transform.lossyScale.x);
        Vector3 giro = Vector3.Cross(Vector3.up, direcao) * (alvo / Mathf.Max(raio, 0.01f));
        corpo.AddTorque((giro - corpo.angularVelocity) * 0.5f, ForceMode.VelocityChange);
        proximoToque = Time.fixedTime + intervaloEntreToques;
    }
}

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class InimigoAtaque : MonoBehaviour
{
    [Header("Alcance para iniciar o ataque")]
    public float alcanceDeAtaque = 2.5f;
    public float histerese = 0.5f;

    [Header("Zona de acerto (sem física)")]
    [Tooltip("Opcional: objeto filho do osso do braço. Se vazio, mede a partir do próprio inimigo.")]
    public Transform pontoDoGolpe;
    [Tooltip("Distância máxima do ponto do golpe até o jogador para o golpe acertar.")]
    public float alcanceDoGolpe = 2.0f;
    [Tooltip("Ângulo total do cone frontal (180 = meio círculo, 360 = ignora o ângulo).")]
    [Range(10f, 360f)] public float anguloDoGolpe = 120f;
    [Tooltip("Diferença máxima de altura entre o ponto do golpe e o jogador.")]
    public float toleranciaVertical = 1.5f;

    [Header("Ataque")]
    public int dano = 10;
    public float atrasoDoDano = 1.0f;
    public float duracaoDaAnimacao = 1.5f;
    public float tempoEntreAtaques = 0.5f;
    public float velocidadeDeGiro = 8f;

    [Header("Referências")]
    public AudioSource fonteDeAudio;
    public AudioClip somDeAtaque;
    public Animator animator;
    public string nomeDoTriggerDeAtaque = "Atacar";

    private Transform jogador;
    private JogadorVida vidaDoJogador;
    private NavMeshAgent agente;
    private float proximoAtaquePermitidoEm = 0f;
    private bool atacando = false;

    void Start()
    {
        GameObject objetoJogador = GameObject.FindWithTag("Jogador");

        if (objetoJogador != null)
        {
            jogador = objetoJogador.transform;
            vidaDoJogador = objetoJogador.GetComponent<JogadorVida>();
        }

        agente = GetComponent<NavMeshAgent>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (agente != null)
        {
            agente.stoppingDistance = Mathf.Max(0f, alcanceDeAtaque - 0.3f);
        }
    }

    void Update()
    {
        if (jogador == null || vidaDoJogador == null)
        {
            return;
        }

        float distancia = DistanciaHorizontal(transform.position, jogador.position);

        if (atacando)
        {
            ParaDeAndar(true);
            OlharParaJogador();
            return;
        }

        if (distancia <= alcanceDeAtaque)
        {
            ParaDeAndar(true);
            OlharParaJogador();

            if (Time.time >= proximoAtaquePermitidoEm)
            {
                StartCoroutine(Atacar());
            }
        }
        else if (distancia > alcanceDeAtaque + histerese)
        {
            ParaDeAndar(false);
        }
    }

    IEnumerator Atacar()
    {
        atacando = true;

        if (animator != null)
        {
            animator.ResetTrigger(nomeDoTriggerDeAtaque);
            animator.SetTrigger(nomeDoTriggerDeAtaque);
        }

        yield return new WaitForSeconds(atrasoDoDano);

        // Só agora confere se o jogador está dentro da zona do golpe.
        if (JogadorEstaNaZonaDeAcerto())
        {
            vidaDoJogador.ReceberDano(dano);
            Debug.Log(gameObject.name + " acertou o jogador causando " + dano + " de dano.");

            if (fonteDeAudio != null && somDeAtaque != null)
            {
                fonteDeAudio.PlayOneShot(somDeAtaque);
            }
        }

        float restante = Mathf.Max(0f, duracaoDaAnimacao - atrasoDoDano);
        yield return new WaitForSeconds(restante);

        atacando = false;
        proximoAtaquePermitidoEm = Time.time + tempoEntreAtaques;
    }

    bool JogadorEstaNaZonaDeAcerto()
    {
        Vector3 origem = pontoDoGolpe != null ? pontoDoGolpe.position : transform.position;

        // 1) Distância horizontal até o jogador.
        if (DistanciaHorizontal(origem, jogador.position) > alcanceDoGolpe)
        {
            return false;
        }

        // 2) Altura: evita acertar quem está muito acima ou abaixo.
        if (Mathf.Abs(jogador.position.y - origem.y) > toleranciaVertical)
        {
            return false;
        }

        // 3) Ângulo: o jogador precisa estar na frente do inimigo.
        Vector3 paraJogador = jogador.position - transform.position;
        paraJogador.y = 0f;

        if (paraJogador.sqrMagnitude > 0.001f)
        {
            float angulo = Vector3.Angle(transform.forward, paraJogador);

            if (angulo > anguloDoGolpe * 0.5f)
            {
                return false;
            }
        }

        return true;
    }

    float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void ParaDeAndar(bool parar)
    {
        if (agente != null && agente.isActiveAndEnabled && agente.isOnNavMesh)
        {
            agente.isStopped = parar;
        }
    }

    void OlharParaJogador()
    {
        Vector3 direcao = jogador.position - transform.position;
        direcao.y = 0f;

        if (direcao.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion alvo = Quaternion.LookRotation(direcao);
        transform.rotation = Quaternion.Slerp(transform.rotation, alvo, velocidadeDeGiro * Time.deltaTime);
    }

    // Mostra na Scene o alcance do golpe (vermelho) e o de início do ataque (amarelo).
    void OnDrawGizmosSelected()
    {
        Vector3 origem = pontoDoGolpe != null ? pontoDoGolpe.position : transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origem, alcanceDoGolpe);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceDeAtaque);

        // Linhas do cone frontal.
        float metade = anguloDoGolpe * 0.5f;
        Vector3 esquerda = Quaternion.Euler(0f, -metade, 0f) * transform.forward;
        Vector3 direita = Quaternion.Euler(0f, metade, 0f) * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + esquerda * alcanceDoGolpe);
        Gizmos.DrawLine(transform.position, transform.position + direita * alcanceDoGolpe);
    }
}
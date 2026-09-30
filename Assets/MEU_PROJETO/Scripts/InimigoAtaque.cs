using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// Coloque esse script no mesmo objeto que já tem InimigoPerseguidor ou
// InimigoPatrulheiroAtaque (funciona junto com qualquer um dos dois).
// Precisa de um NavMeshAgent no mesmo objeto (o inimigo já deve ter).
public class InimigoAtaque : MonoBehaviour
{
    public float alcanceDeAtaque = 4.5f;
    public int dano = 10;
    public float tempoEntreAtaques = 1.0f;

    // Tempo entre o INÍCIO da animação e o momento em que o golpe
    // realmente acerta (o "soco"/mordida). Ajuste pra bater com o clipe.
    public float atrasoDoDano = 1.0f;

    public AudioSource fonteDeAudio;
    public AudioClip somDeAtaque;

    // Arraste aqui o Animator do modelo (ex: o do porco).
    public Animator animator;
    public string nomeDoTriggerDeAtaque = "Atacar";

    private Transform jogador;
    private JogadorVida vidaDoJogador;
    private NavMeshAgent agente;
    private float proximoAtaquePermitidoEm = 0f;

    void Start()
    {
        GameObject objetoJogador = GameObject.FindWithTag("Jogador");

        if (objetoJogador != null)
        {
            jogador = objetoJogador.transform;
            vidaDoJogador = objetoJogador.GetComponent<JogadorVida>();
        }

        agente = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (jogador == null || vidaDoJogador == null)
        {
            return;
        }

        float distancia = Vector3.Distance(transform.position, jogador.position);

        if (distancia <= alcanceDeAtaque)
        {
            // Para de andar enquanto está no alcance de ataque,
            // pra não ficar "empurrando" o jogador ou tremendo no lugar.
            if (agente != null)
            {
                agente.isStopped = true;
            }

            TentarAtacar();
        }
        else if (agente != null)
        {
            agente.isStopped = false;
        }
    }

    void TentarAtacar()
    {
        if (Time.time < proximoAtaquePermitidoEm)
        {
            return;
        }

        proximoAtaquePermitidoEm = Time.time + tempoEntreAtaques;

        // Dispara a animação e o som IMEDIATAMENTE...
        if (animator != null)
        {
            animator.SetTrigger(nomeDoTriggerDeAtaque);
        }

        // ...mas o dano só é aplicado depois de "atrasoDoDano" segundos,
        // sincronizado com o momento do golpe na animação.
        StartCoroutine(CausarDanoComAtraso());
    }

    IEnumerator CausarDanoComAtraso()
    {
        yield return new WaitForSeconds(atrasoDoDano);

        if (vidaDoJogador == null || jogador == null)
        {
            yield break;
        }

        float distancia = Vector3.Distance(transform.position, jogador.position);

        // Confere de novo se o jogador ainda está por perto — ele pode ter
        // andado pra longe durante o tempo de espera do golpe.
        if (distancia <= alcanceDeAtaque)
        {
            vidaDoJogador.ReceberDano(dano);
            Debug.Log(gameObject.name + " atacou o jogador causando " + dano + " de dano.");

            if (fonteDeAudio != null && somDeAtaque != null)
            {
                fonteDeAudio.PlayOneShot(somDeAtaque);
            }
        }
    }
}
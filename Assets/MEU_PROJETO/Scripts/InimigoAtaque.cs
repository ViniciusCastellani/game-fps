using UnityEngine;
using UnityEngine.AI;

// Coloque esse script no mesmo objeto que já tem InimigoPerseguidor ou
// InimigoPatrulheiroAtaque (funciona junto com qualquer um dos dois).
// Precisa de um NavMeshAgent no mesmo objeto (o inimigo já deve ter).
public class InimigoAtaque : MonoBehaviour
{
    public float alcanceDeAtaque = 1.5f;
    public int dano = 10;
    public float tempoEntreAtaques = 1.5f;

    public AudioSource fonteDeAudio;
    public AudioClip somDeAtaque;

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

        vidaDoJogador.ReceberDano(dano);

        if (fonteDeAudio != null && somDeAtaque != null)
        {
            fonteDeAudio.PlayOneShot(somDeAtaque);
        }

        Debug.Log(gameObject.name + " atacou o jogador causando " + dano + " de dano.");
    }
}
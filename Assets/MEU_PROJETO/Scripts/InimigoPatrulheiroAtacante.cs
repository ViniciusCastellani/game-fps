using UnityEngine;
using UnityEngine.AI;
public class InimigoPatrulheiroAtaque : MonoBehaviour
{
    // variaveis de Patrulha
    public Transform[] pontosDePatrulha;
    private int indice;
    private NavMeshAgent agenteInimigo;
    // variaveis de Ataque
    private Transform jogador;
    private float distanciaDoJogador;
    private bool jogadorDetectado = false;
    public float visaoDoInimigo = 10f;
    // variaveis quando o Jogador for Detectado
    public GameObject iconeAlvoDetectado;
    private AudioSource audioPlayer;
    public AudioClip audioAlvoDetectado;
    private bool audioJaTocou;
    void Start()
    {
        jogador = GameObject.FindWithTag("Jogador").GetComponent<Transform>();
        agenteInimigo = GetComponent<NavMeshAgent>();
        agenteInimigo.SetDestination(pontosDePatrulha[0].position);
        jogadorDetectado = false;
        audioPlayer = GetComponent<AudioSource>();
        audioJaTocou = false;
    }
    void Update()
    {
        ProcurarJogador();
        if (jogadorDetectado)
            Atacar();
        else if (!jogadorDetectado)
            Patrulhar();
    }

    void ProcurarJogador()
    {
        distanciaDoJogador = Vector3.Distance(transform.position,
        jogador.position);

        // Se o jogador estiver no campo de visão
        if (distanciaDoJogador <= visaoDoInimigo)
        {
            jogadorDetectado = true;
            iconeAlvoDetectado.SetActive(true);
            if (audioJaTocou == false)
            {
                audioPlayer.PlayOneShot(audioAlvoDetectado);
                audioJaTocou = true;
            }
        }
        // Se o jogador estiver fora do campo de visão
        else if (distanciaDoJogador >= visaoDoInimigo)
        {
            jogadorDetectado = false;
            iconeAlvoDetectado.SetActive(false);
            audioJaTocou = false;
        }
    }
    void Atacar()
    {
        // Parte para cima do Jogador
        agenteInimigo.SetDestination(jogador.position);
    }
    void Patrulhar()
    {
        // Se a distancia do Inimigo ate o destino for menor q 1,
        // ou seja, ainda não chegou
        if (agenteInimigo.remainingDistance < 1)
        {
            // Se o indice do vetor de pontosDePatrulha for igual
            // ao total de pontosDePatrulha, ou seja terminou a ronda
            if (indice >= pontosDePatrulha.Length - 1)
                indice = 0; // reinicia a rota
            else
                indice++; // vai para o proximo pontoDePatrulha
                          // seta o destino como o proximo pontoDePatrulha
            agenteInimigo.SetDestination(pontosDePatrulha[indice].position);
        }
    }
}
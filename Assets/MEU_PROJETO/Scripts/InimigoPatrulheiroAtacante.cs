using UnityEngine;
using UnityEngine.AI;
public class InimigoPatrulheiroAtaque : MonoBehaviour
{
    public Transform[] pontosDePatrulha;
    private int indice;
    private NavMeshAgent agenteInimigo;
    private Transform jogador;
    private float distanciaDoJogador;
    private bool jogadorDetectado = false;
    public float visaoDoInimigo = 10f;
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
        else if (distanciaDoJogador >= visaoDoInimigo)
        {
            jogadorDetectado = false;
            iconeAlvoDetectado.SetActive(false);
            audioJaTocou = false;
        }
    }
    void Atacar()
    {
        agenteInimigo.SetDestination(jogador.position);
    }
    void Patrulhar()
    {
        if (agenteInimigo.remainingDistance < 1)
        {
            if (indice >= pontosDePatrulha.Length - 1)
                indice = 0;
            else
                indice++;
            agenteInimigo.SetDestination(pontosDePatrulha[indice].position);
        }
    }
}
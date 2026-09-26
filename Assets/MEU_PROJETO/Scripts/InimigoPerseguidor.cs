using UnityEngine;
using UnityEngine.AI;
public class InimigoPerseguidor : MonoBehaviour
{
    private NavMeshAgent inimigo;
    private Transform jogador;
    void Start()
    {
        inimigo = GetComponent<NavMeshAgent>();
        jogador = GameObject.FindWithTag("Jogador").GetComponent<Transform>();
    }
    // Update is called once per frame
    void Update()
    {
        PerseguirJogador();
    }
    void PerseguirJogador()
    {
        inimigo.SetDestination(jogador.transform.position);
    }
}
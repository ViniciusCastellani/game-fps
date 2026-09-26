using UnityEngine;
using UnityEngine.AI;
public class InimigoPatrulheiro : MonoBehaviour
{
    // variaveis de Patrulha
    public Transform[] pontosDePatrulha;
    private int indice;
    private NavMeshAgent inimigo;
    void Start()
    {
        inimigo = GetComponent<NavMeshAgent>();
        inimigo.SetDestination(pontosDePatrulha[0].position);
    }
    // Update is called once per frame
    void Update()
    {
        Patrulhar();
    }
    void Patrulhar()
    {
        // Se a distancia do Inimigo ate o destino for menor que 1,
        // ou seja, ainda não chegou
        if (inimigo.remainingDistance < 1)
        {
            // Se o indice do vetor de pontosDePatrulha for igual
            // ao total de pontosDePatrulha, ou seja terminou a ronda
            if (indice >= pontosDePatrulha.Length - 1)
                indice = 0; // reinicia a rota
            else
                indice++; // vai para o proximo pontoDePatrulha
                          // seta o destino como o proximo pontoDePatrulha
            inimigo.SetDestination(pontosDePatrulha[indice].position);
        }
    }
}
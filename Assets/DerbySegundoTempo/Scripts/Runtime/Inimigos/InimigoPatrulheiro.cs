using UnityEngine;
using UnityEngine.AI;
public class InimigoPatrulheiro : MonoBehaviour
{
    public Transform[] pontosDePatrulha;
    private int indice;
    private NavMeshAgent inimigo;
    void Start()
    {
        inimigo = GetComponent<NavMeshAgent>();
        inimigo.SetDestination(pontosDePatrulha[0].position);
    }
    void Update()
    {
        Patrulhar();
    }
    void Patrulhar()
    {
        if (inimigo.remainingDistance < 1)
        {
            if (indice >= pontosDePatrulha.Length - 1)
                indice = 0;
            else
                indice++;
            inimigo.SetDestination(pontosDePatrulha[indice].position);
        }
    }
}
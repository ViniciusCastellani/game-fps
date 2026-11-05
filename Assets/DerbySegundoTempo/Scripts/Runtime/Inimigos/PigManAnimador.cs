using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(InimigoAtaque))]
public class PigManAnimador : MonoBehaviour
{
    public Animator animator;

    NavMeshAgent agente;
    InimigoAtaque ataque;
    int camadaDaEspada;
    static readonly int Velocidade = Animator.StringToHash("Velocidade");

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        ataque = GetComponent<InimigoAtaque>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) return;
        animator.applyRootMotion = false;
        camadaDaEspada = animator.GetLayerIndex("PosturaEspada");
    }

    void LateUpdate()
    {
        if (animator == null) return;
        float velocidade = agente.isActiveAndEnabled && agente.isOnNavMesh && !agente.isStopped
            ? agente.velocity.magnitude : 0f;
        animator.SetFloat(Velocidade, velocidade, .12f, Time.deltaTime);
        // A postura segura a espada durante a locomocao. O golpe usa os dois bracos do clipe completo.
        if (camadaDaEspada >= 0)
            animator.SetLayerWeight(camadaDaEspada, ataque.Atacando ? 0f : 1f);
    }
}

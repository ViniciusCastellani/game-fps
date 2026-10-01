using UnityEngine;

// Alimenta o Animator do personagem com os dados de movimento do Rigidbody.
// Não mexe em nenhuma lógica de movimento/pulo: apenas LÊ os dados existentes.
[RequireComponent(typeof(Rigidbody))]
public class JogadorAnimador : MonoBehaviour
{
    public Animator animator;
    public JogadorPulo pulo;              // lê "estaNoChao"
    public float suavizacao = 0.08f;      // suaviza a troca entre parado/andando/correndo

    private Rigidbody rb;
    private bool estavaNoChao = true;
    private float velocidadeSuave;
    private float velocidadeRef;

    static readonly int pVelocidade = Animator.StringToHash("Velocidade");
    static readonly int pNoChao = Animator.StringToHash("NoChao");
    static readonly int pPular = Animator.StringToHash("Pular");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (pulo == null) pulo = GetComponent<JogadorPulo>();
    }

    // LateUpdate: roda depois do Update do JogadorPulo, evitando problema de ordem de execução
    void LateUpdate()
    {
        if (animator == null) return;

        Vector3 v = rb.linearVelocity;
        float horizontal = new Vector2(v.x, v.z).magnitude;
        velocidadeSuave = Mathf.SmoothDamp(velocidadeSuave, horizontal, ref velocidadeRef, suavizacao);
        animator.SetFloat(pVelocidade, velocidadeSuave);

        bool noChao = pulo != null ? pulo.estaNoChao : true;
        animator.SetBool(pNoChao, noChao);

        // saiu do chão subindo = pulo; (se só caiu de uma borda, vai direto para o estado "NoAr")
        if (estavaNoChao && !noChao && v.y > 0.5f) animator.SetTrigger(pPular);
        if (!estavaNoChao && noChao) animator.ResetTrigger(pPular);

        estavaNoChao = noChao;
    }
}

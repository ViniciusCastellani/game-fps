using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class JogadorAnimador : MonoBehaviour
{
    public Animator animator;
    public JogadorPulo pulo;
    public float suavizacao = 0.08f;

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

    void LateUpdate()
    {
        if (animator == null) return;

        Vector3 v = rb.linearVelocity;
        float horizontal = new Vector2(v.x, v.z).magnitude;
        velocidadeSuave = Mathf.SmoothDamp(velocidadeSuave, horizontal, ref velocidadeRef, suavizacao);
        animator.SetFloat(pVelocidade, velocidadeSuave);

        bool noChao = pulo != null ? pulo.estaNoChao : true;
        animator.SetBool(pNoChao, noChao);

        if (estavaNoChao && !noChao && v.y > 0.5f) animator.SetTrigger(pPular);
        if (!estavaNoChao && noChao) animator.ResetTrigger(pPular);

        estavaNoChao = noChao;
    }
}

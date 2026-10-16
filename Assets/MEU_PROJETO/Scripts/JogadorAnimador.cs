using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(10)]
public class JogadorAnimador : MonoBehaviour
{
    public Animator animator;
    public JogadorPulo pulo;
    public float suavizacao = 0.08f;

    private Rigidbody rb;
    private bool estavaNoChao = true;
    private float velocidadeSuave;
    private float velocidadeRef;
    private uint ultimoPulo;

    static readonly int pVelocidade = Animator.StringToHash("Velocidade");
    static readonly int pNoChao = Animator.StringToHash("NoChao");
    static readonly int pPular = Animator.StringToHash("Pular");
    static readonly int estadoPuloInicio = Animator.StringToHash("Base Layer.PuloInicio");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (pulo == null) pulo = GetComponent<JogadorPulo>();
        if (pulo != null) ultimoPulo = pulo.NumeroDoPulo;
    }

    void Update()
    {
        if (animator == null) return;

        Vector3 v = rb.linearVelocity;
        float horizontal = new Vector2(v.x, v.z).magnitude;
        velocidadeSuave = Mathf.SmoothDamp(velocidadeSuave, horizontal, ref velocidadeRef, suavizacao);
        animator.SetFloat(pVelocidade, velocidadeSuave);

        bool noChao = pulo != null ? pulo.estaNoChao : true;
        animator.SetBool(pNoChao, noChao);

        bool novoPulo = pulo != null && pulo.NumeroDoPulo != ultimoPulo;
        if (novoPulo)
        {
            ultimoPulo = pulo.NumeroDoPulo;
            animator.ResetTrigger(pPular);
            // Inicia no mesmo frame do input, inclusive durante uma transição de pouso.
            animator.Play(estadoPuloInicio, 0, 0f);
        }
        else if (!estavaNoChao && noChao)
            animator.ResetTrigger(pPular);

        estavaNoChao = noChao;
    }
}

using UnityEngine;
public class JogadorPulo : MonoBehaviour
{
    private Rigidbody jogadorRigidBody;
    public float alturaDoPulo;
    public bool estaNoChao;
    public uint NumeroDoPulo { get; private set; }
    [Tooltip("Tempo de antecipação da animação antes do impulso, em segundos.")]
    [Min(0f)] public float antecipacaoDoPulo = 0.1f;
    private bool impulsoPendente;
    private float instanteDoImpulso;
    void Start()
    {
        jogadorRigidBody = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (Time.timeScale > 0f && Input.GetButtonDown("Jump") && estaNoChao && !impulsoPendente)
        {
            Pular();
        }
    }
    void Pular()
    {
        impulsoPendente = true;
        instanteDoImpulso = Time.time + antecipacaoDoPulo;
        estaNoChao = false;
        // Registra o pulo aceito sem depender de quando a física aplica o impulso.
        NumeroDoPulo++;
    }
    void FixedUpdate()
    {
        if (!impulsoPendente || Time.time < instanteDoImpulso) return;

        impulsoPendente = false;
        jogadorRigidBody.AddForce(Vector3.up * alturaDoPulo, ForceMode.Impulse);
    }
    void OnDisable()
    {
        if (impulsoPendente)
        {
            impulsoPendente = false;
            estaNoChao = true;
        }
    }
    private void OnCollisionEnter(Collision objetoColidido)
    {
        if (impulsoPendente) return;
        if (objetoColidido.gameObject.CompareTag("Chão"))
        {
            estaNoChao = true;
        }
    }
}

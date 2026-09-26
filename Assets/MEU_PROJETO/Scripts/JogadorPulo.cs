using UnityEngine;
public class JogadorPulo : MonoBehaviour
{
    private Rigidbody jogadorRigidBody;
    public float alturaDoPulo;
    public bool estaNoChao;
    // Start is called before the first frame update
    void Start()
    {
        jogadorRigidBody = GetComponent<Rigidbody>();
    }
    // Update is called once per frame
    void Update()
    {
        // Ao precionar o "BOTÃO" de pulo (Barra de Espaço) e estiver no chão
        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            Pular();
        }
    }
    void Pular()
    {
        // Apliaca uma força vertical e faz o jogador pular
        jogadorRigidBody.AddForce(Vector3.up * alturaDoPulo, ForceMode.Impulse);
        //volta a variavel para false;
        estaNoChao = false;
    }
    private void OnCollisionEnter(Collision objetoColidido)
    {
        if (objetoColidido.gameObject.CompareTag("Chão"))
        {
            estaNoChao = true;
        }
    }
}
using UnityEngine;
public class JogadorPulo : MonoBehaviour
{
    private Rigidbody jogadorRigidBody;
    public float alturaDoPulo;
    public bool estaNoChao;
    void Start()
    {
        jogadorRigidBody = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            Pular();
        }
    }
    void Pular()
    {
        jogadorRigidBody.AddForce(Vector3.up * alturaDoPulo, ForceMode.Impulse);
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
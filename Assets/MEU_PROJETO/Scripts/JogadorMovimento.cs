using UnityEngine;
public class JogadorMovimento : MonoBehaviour
{
    public float velocidade;
    public float velocidadeAndar = 5;
    public float velocidadeCorrer = 10;
    private Vector3 direcao;
    private Vector3 velocidadeFinal;
    private Rigidbody jogador;
    void Start()
    {
        jogador = GetComponent<Rigidbody>();
        velocidade = velocidadeAndar;
    }
    void Update()
    {
        MonitorarControles();
    }
    void FixedUpdate()
    {
        Andar();
        Correr();
    }
    void MonitorarControles()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        direcao = transform.right * horizontal + transform.forward * vertical;
    }

    void Andar()
    {
        velocidadeFinal = direcao * velocidade;
        jogador.linearVelocity = new Vector3(velocidadeFinal.x,
        jogador.linearVelocity.y,
        velocidadeFinal.z);
    }
    void Correr()
    {
        if (Input.GetButton("Fire3")){
            velocidade = velocidadeCorrer;
        } else
        {
            velocidade = velocidadeAndar;
        }
    }
}
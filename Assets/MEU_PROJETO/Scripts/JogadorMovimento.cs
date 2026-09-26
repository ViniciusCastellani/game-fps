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
        // Ao iniciar a velocidade padrão é de Andar
        velocidade = velocidadeAndar;
    }
    // Update is called once per frame
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
        // captura o Input do teclado no eixo Horizontal (setas esquerda, direita, A,D)
        float horizontal = Input.GetAxis("Horizontal");
        // captura o Input do teclado no eixo Vertical (setas cima, baixo , W, S)
        float vertical = Input.GetAxis("Vertical");
        // direcao do jogador
        direcao = transform.right * horizontal + transform.forward * vertical;
    }

    void Andar()
    {
        //velocidade final
        velocidadeFinal = direcao * velocidade;
        // adiciona a velocidade final ao RigidBody
        // mantém a velocidade vertical atual (Será controlada pelo Pulo)
        jogador.linearVelocity = new Vector3(velocidadeFinal.x,
        jogador.linearVelocity.y,
        velocidadeFinal.z);
    }
    void Correr()
    {
        // se o jogfador apertou o Fire3 = Shift
        if (Input.GetButton("Fire3")){
            velocidade = velocidadeCorrer;
        } else
        {
            velocidade = velocidadeAndar;
        }
    }
}
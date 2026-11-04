using UnityEngine;
[DefaultExecutionOrder(-200)]
public class JogadorMovimentoCamera : MonoBehaviour
{
    public float mouseSensibilidade = 100;
    public Transform cameraAlvo;
    private float rotacaoVertical;
    private float mouseX;
    private float mouseY;
    private Rigidbody jogador;
    private float rotacaoHorizontal;
    private Vector3 posicaoAlvoNoJogador;
    public float RotacaoVertical => rotacaoVertical;
    public float RotacaoHorizontal => rotacaoHorizontal;
    void Start()
    {
        jogador = GetComponent<Rigidbody>();
        rotacaoHorizontal = transform.eulerAngles.y;
        if (cameraAlvo != null)
        {
            posicaoAlvoNoJogador = transform.InverseTransformPoint(cameraAlvo.position);
            rotacaoVertical = Mathf.DeltaAngle(0f, cameraAlvo.localEulerAngles.x);
        }
        // Trava e esconde o cursor do mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        if (Time.timeScale == 0f) return;
        MonitorarControles();
        Olhar();
    }
    void FixedUpdate()
    {
        if (jogador != null)
            jogador.MoveRotation(Quaternion.Euler(0f, rotacaoHorizontal, 0f));
    }
    void LateUpdate()
    {
        // Posição e orientação usam o mesmo yaw. Misturar o yaw interpolado do corpo
        // com o yaw atual da mira faz o offset do Cinemachine oscilar durante a rotação.
        if (cameraAlvo != null)
        {
            Quaternion yaw = Quaternion.Euler(0f, rotacaoHorizontal, 0f);
            Vector3 offset = Vector3.Scale(posicaoAlvoNoJogador, transform.lossyScale);
            cameraAlvo.SetPositionAndRotation(
                transform.position + yaw * offset,
                Quaternion.Euler(rotacaoVertical, rotacaoHorizontal, 0f));
        }
    }
    void MonitorarControles()
    {
        // Captura os inputs do mouse
        // Mouse X/Y já representam deslocamento por frame. Mantém a sensibilidade de 60 FPS.
        mouseX = Input.GetAxis("Mouse X") * mouseSensibilidade / 60f;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensibilidade / 60f;
    }
    void Olhar()
    {
        // Rotação vertical da câmera
        rotacaoVertical -= mouseY;
        // Limita a rotação para cima e para baixo
        rotacaoVertical = Mathf.Clamp(rotacaoVertical, -80f, 80f);
        // A rotação da mira é aplicada no LateUpdate, antes do Cinemachine.
        // Rotaciona o jogador horizontalmente
        rotacaoHorizontal = Mathf.Repeat(rotacaoHorizontal + mouseX, 360f);
        if (jogador == null)
            transform.rotation = Quaternion.Euler(0f, rotacaoHorizontal, 0f);
    }
}

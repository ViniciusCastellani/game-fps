using UnityEngine;
public class JogadorMovimentoCamera : MonoBehaviour
{
    public float mouseSensibilidade = 100;
    public Transform minhaCamera;
    private float rotacaoVertical;
    private float mouseX;
    private float mouseY;
    void Start()
    {
        // Trava e esconde o cursor do mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    // Update is called once per frame
    void Update()
    {
        MonitorarControles();
        Olhar();
    }
    void MonitorarControles()
    {
        // captura os inputs
        mouseX = Input.GetAxis("Mouse X") * mouseSensibilidade * Time.deltaTime;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensibilidade * Time.deltaTime;
    }
    void Olhar()
    {
        // rotacao da camera para cima e baixo é invertida na unity
        rotacaoVertical -= mouseY;
        // restringe a rotação da camera para cima e para baixo em um limite
        rotacaoVertical = Mathf.Clamp(rotacaoVertical, -80, 80);
        // aplica as rotações em quaternion somente no eixo X (cima e baixo)
        minhaCamera.transform.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
        // rotaciona o corpo do jogador
        transform.Rotate(Vector3.up * mouseX);
    }
}
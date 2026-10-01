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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        MonitorarControles();
        Olhar();
    }
    void MonitorarControles()
    {
        mouseX = Input.GetAxis("Mouse X") * mouseSensibilidade * Time.deltaTime;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensibilidade * Time.deltaTime;
    }
    void Olhar()
    {
        rotacaoVertical -= mouseY;
        rotacaoVertical = Mathf.Clamp(rotacaoVertical, -80, 80);
        minhaCamera.transform.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}
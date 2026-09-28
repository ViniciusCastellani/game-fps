using UnityEngine;

// Coloque esse script em qualquer Canvas (World Space) que deva sempre
// ficar de frente pra câmera (texto de estação, barra de vida, etc).
public class OlharParaCamera : MonoBehaviour
{
    private Camera camaraPrincipal;

    void Start()
    {
        camaraPrincipal = Camera.main;
    }

    void LateUpdate()
    {
        if (camaraPrincipal == null)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            transform.position - camaraPrincipal.transform.position
        );
    }
}
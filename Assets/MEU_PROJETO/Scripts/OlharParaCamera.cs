using UnityEngine;

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
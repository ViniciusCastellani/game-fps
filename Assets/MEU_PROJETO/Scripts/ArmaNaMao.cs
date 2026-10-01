using UnityEngine;

public class ArmaNaMao : MonoBehaviour
{
    public Transform arma;
    public Transform mao;
    public Transform cameraJogador;

    [Tooltip("Deslocamento da arma em relação à mão, no espaço da câmera (metros). Ajuste em Play Mode.")]
    public Vector3 deslocamento = new Vector3(0.02f, 0.04f, 0.12f);

    [Tooltip("Rotação extra (graus) caso o cano não esteja alinhado com a mira.")]
    public Vector3 rotacaoExtra = Vector3.zero;

    void LateUpdate()
    {
        if (arma == null || mao == null || cameraJogador == null) return;

        Quaternion rotCam = cameraJogador.rotation;
        arma.rotation = rotCam * Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(rotacaoExtra);
        arma.position = mao.position + rotCam * deslocamento;
    }
}

using UnityEngine;

// Depois do tronco (-100) e do CinemachineBrain (100), usa mão e câmera do mesmo frame.
[DefaultExecutionOrder(200)]
public class ArmaNaMao : MonoBehaviour
{
    public Transform arma;
    public Transform mao;
    public Transform cameraJogador;

    [Tooltip("Deslocamento da arma em relação à mão, no espaço da câmera (metros). Ajuste em Play Mode.")]
    public Vector3 deslocamento = new Vector3(0.02f, 0.04f, 0.12f);

    [Tooltip("Deslocamento lateral adicional em primeira pessoa, em metros no espaço da câmera. Negativo move para a esquerda.")]
    public float deslocamentoLateralPrimeiraPessoa = -0.12f;

    private AlternarCamera alternarCamera;

    void Awake()
    {
        alternarCamera = GetComponent<AlternarCamera>();
    }

    [Tooltip("Rotação extra (graus) caso o cano não esteja alinhado com a mira.")]
    public Vector3 rotacaoExtra = Vector3.zero;

    void LateUpdate()
    {
        if (arma == null || mao == null || cameraJogador == null) return;

        Quaternion rotCam = cameraJogador.rotation;
        arma.rotation = rotCam * Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(rotacaoExtra);
        Vector3 deslocamentoAtual = deslocamento;
        if (alternarCamera != null && alternarCamera.PrimeiraPessoa)
            deslocamentoAtual.x += deslocamentoLateralPrimeiraPessoa;

        arma.position = mao.position + rotCam * deslocamentoAtual;
    }
}

using UnityEngine;

// Mantém a arma_papelao na mão direita do personagem, com o cano sempre apontando
// para onde a câmera olha (o tiro já sai da câmera, ver Arma.cs).
public class ArmaNaMao : MonoBehaviour
{
    public Transform arma;            // arma_papelao
    public Transform mao;             // mixamorig:RightHand
    public Transform cameraJogador;   // Camera do jogador

    [Tooltip("Deslocamento da arma em relação à mão, no espaço da câmera (metros). Ajuste em Play Mode.")]
    public Vector3 deslocamento = new Vector3(0.02f, 0.04f, 0.12f);

    [Tooltip("Rotação extra (graus) caso o cano não esteja alinhado com a mira.")]
    public Vector3 rotacaoExtra = Vector3.zero;

    void LateUpdate()
    {
        if (arma == null || mao == null || cameraJogador == null) return;

        Quaternion rotCam = cameraJogador.rotation;
        // O cano da arma_papelao aponta para +X local; Euler(0,-90,0) leva +X para a frente (+Z)
        arma.rotation = rotCam * Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(rotacaoExtra);
        arma.position = mao.position + rotCam * deslocamento;
    }
}

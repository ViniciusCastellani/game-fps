using UnityEngine;

// Atualiza depois da mira (-200) e antes do CinemachineBrain (100).
[DefaultExecutionOrder(-150)]
public class AlvoCameraTerceiraPessoa : MonoBehaviour
{
    public Transform jogador;
    public Transform mira;
    [Tooltip("Ponto de partida da colisao, em coordenadas locais do jogador.")]
    public Vector3 deslocamentoLocal;

    void LateUpdate()
    {
        if (jogador == null || mira == null) return;
        Quaternion yaw = Quaternion.Euler(0, mira.eulerAngles.y, 0);
        transform.SetPositionAndRotation(
            jogador.position + yaw * Vector3.Scale(deslocamentoLocal, jogador.lossyScale),
            mira.rotation);
    }
}

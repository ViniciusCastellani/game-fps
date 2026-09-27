using UnityEngine;

// Coloque esse script no prefab da caixa de munição.
// O objeto precisa de um Collider marcado como "Is Trigger".
public class Municao : MonoBehaviour
{
    public int quantidadeDeMunicao = 30;
    public AudioClip somDeColeta;

    void OnTriggerEnter(Collider outro)
    {
        if (!outro.CompareTag("Jogador"))
        {
            return;
        }

        // a Arma normalmente está em um filho do jogador (a câmera/mão),
        // então procuramos nos filhos e, se não achar, no próprio objeto.
        Arma arma = outro.GetComponentInChildren<Arma>();

        if (arma == null)
        {
            arma = outro.GetComponentInParent<Arma>();
        }

        if (arma == null)
        {
            return;
        }

        arma.AdicionarMunicao(quantidadeDeMunicao);

        if (somDeColeta != null)
        {
            AudioSource.PlayClipAtPoint(somDeColeta, transform.position);
        }

        Destroy(gameObject);
    }
}
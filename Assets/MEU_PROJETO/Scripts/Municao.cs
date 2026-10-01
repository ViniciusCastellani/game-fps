using UnityEngine;

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
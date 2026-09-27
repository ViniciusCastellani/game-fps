using UnityEngine;

// Coloque esse script no prefab do medkit.
// O objeto precisa de um Collider marcado como "Is Trigger".
public class Medkit : MonoBehaviour
{
    public int quantidadeDeCura = 25;
    public AudioClip somDeColeta;

    void OnTriggerEnter(Collider outro)
    {
        // só reage se quem encostou for o jogador
        if (!outro.CompareTag("Jogador"))
        {
            return;
        }

        JogadorVida vidaDoJogador = outro.GetComponent<JogadorVida>();

        if (vidaDoJogador == null)
        {
            vidaDoJogador = outro.GetComponentInParent<JogadorVida>();
        }

        if (vidaDoJogador == null)
        {
            return;
        }

        vidaDoJogador.RecuperarVida(quantidadeDeCura);

        if (somDeColeta != null)
        {
            AudioSource.PlayClipAtPoint(somDeColeta, transform.position);
        }

        Destroy(gameObject);
    }
}
using UnityEngine;

public class Medkit : MonoBehaviour
{
    public int quantidadeDeCura = 25;
    public AudioClip somDeColeta;

    void OnTriggerEnter(Collider outro)
    {
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

        if (vidaDoJogador.vidaAtual >= vidaDoJogador.vidaMaxima)
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
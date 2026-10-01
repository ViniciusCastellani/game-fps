using UnityEngine;
using System.Collections;

public class Arma : MonoBehaviour
{
    public Camera cameraJogador;
    public float alcance = 100f;
    public int dano = 10;

    public AudioSource audioFonte;
    public AudioClip somDoTiro;
    public AudioClip somDePenteVazio;
    public AudioClip somDeRecarga;

    public int capacidadePente = 30;
    public int municaoAtual = 30;
    public int municaoReserva = 90;

    private bool recarregando = false;

    void Update()
    {
        if (recarregando)
        {
            return;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            Atirar();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Recarregar();
        }
    }

    void Atirar()
    {
        if (municaoAtual <= 0)
        {
            audioFonte.PlayOneShot(somDePenteVazio);
            Debug.Log("Sem munição!");
            return;
        }

        municaoAtual--;

        audioFonte.PlayOneShot(somDoTiro);
        Ray raio = new Ray(cameraJogador.transform.position, cameraJogador.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(raio, out hit, alcance))
        {
            Debug.Log("Acertou: " + hit.collider.name);
            if (hit.collider.gameObject.CompareTag("Inimigo"))
            {
                CausaDano(hit);
            }
        }
    }
    void CausaDano(RaycastHit hit)
    {
        InimigoVida vida = hit.collider.GetComponent<InimigoVida>();
        if (vida != null)
        {
            vida.ReceberDano(dano);
        }
    }

    void Recarregar()
    {
        if (municaoAtual >= capacidadePente)
        {
            return;
        }

        if (municaoReserva <= 0)
        {
            return;
        }

        StartCoroutine(RecarregarComCooldown());
    }

    IEnumerator RecarregarComCooldown()
    {
        recarregando = true;

        audioFonte.PlayOneShot(somDeRecarga);

        if (somDeRecarga != null)
        {
            yield return new WaitForSeconds(somDeRecarga.length);
        }

        int quantidadeNecessaria = capacidadePente - municaoAtual;

        int quantidadeRecarregada = Mathf.Min(
            quantidadeNecessaria,
            municaoReserva
        );

        municaoAtual += quantidadeRecarregada;
        municaoReserva -= quantidadeRecarregada;

        Debug.Log(
            "Munição: "
            + municaoAtual
            + "/"
            + municaoReserva
        );

        recarregando = false;
    }

    public void AdicionarMunicao(int quantidade)
    {
        municaoReserva += quantidade;

        Debug.Log("Munição reserva: " + municaoReserva);
    }

    public void AumentarCapacidadeDoPente(int quantidade)
    {
        capacidadePente += quantidade;
    }
}
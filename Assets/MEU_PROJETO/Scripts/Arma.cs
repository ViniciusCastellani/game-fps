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

    // Enquanto está recarregando, não pode atirar nem começar outra recarga.
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
        // cria um raio invisível que começa na posição da câmera; segue para a direção em que a câmera está olhando;
        Ray raio = new Ray(cameraJogador.transform.position, cameraJogador.transform.forward);
        RaycastHit hit; // variavel hit = acerto do tiro
                        // se o tiro, informações de impacto, alcance maximo
        if (Physics.Raycast(raio, out hit, alcance))
        {
            Debug.Log("Acertou: " + hit.collider.name);
            // se o tiro acertou um inimigo
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
        // Não recarrega se o pente já estiver cheio
        if (municaoAtual >= capacidadePente)
        {
            return;
        }

        // Não recarrega se não houver munição reserva
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

        // Espera o som de recarga terminar antes de somar a munição de verdade.
        if (somDeRecarga != null)
        {
            yield return new WaitForSeconds(somDeRecarga.length);
        }

        int quantidadeNecessaria = capacidadePente - municaoAtual;

        // Se a reserva tiver menos munição do que o necessário,
        // utiliza somente o que estiver disponível.
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

    // Chamado pelo script de pickup (Municao.cs) quando o jogador
    // coleta uma caixa de munição no chão.
    public void AdicionarMunicao(int quantidade)
    {
        municaoReserva += quantidade;

        Debug.Log("Munição reserva: " + municaoReserva);
    }

    // Chamado pela LojaDeUpgrades quando o jogador gasta pontos
    // para aumentar a capacidade do pente.
    public void AumentarCapacidadeDoPente(int quantidade)
    {
        capacidadePente += quantidade;
    }
}
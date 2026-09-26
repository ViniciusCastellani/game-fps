using UnityEngine;
public class Arma : MonoBehaviour
{
    public Camera cameraJogador;
    public float alcance = 100f;
    public int dano = 10;

    public AudioSource audioFonte;
    public AudioClip somDoTiro;

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Atirar();
        }
    }
    void Atirar()
    {
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
}
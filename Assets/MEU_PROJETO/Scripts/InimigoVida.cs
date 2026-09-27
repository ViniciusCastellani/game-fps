using UnityEngine;

public class InimigoVida : MonoBehaviour
{
    public int vida = 100;
    public GameObject efeitoParticulaMorte;
    public AudioSource fonteDeAudio;
    public AudioClip audioMorte;

    public Vector3 offsetParticulaMorte;

    public BarraVidaInimigo barraVida;

    public void ReceberDano(int dano)
    {
        vida -= dano;

        Debug.Log(gameObject.name + " recebeu " + dano + " de dano.");
        
        if (barraVida != null)
        {
            barraVida.AtualizarBarra();
        }

        if (vida <= 0)
        {
            Morrer();
        }
    }

    void Morrer()
    {
        Instantiate(efeitoParticulaMorte, transform.position + offsetParticulaMorte, Quaternion.identity);

        AudioSource.PlayClipAtPoint(audioMorte, transform.position);

        Destroy(gameObject);
    }
}
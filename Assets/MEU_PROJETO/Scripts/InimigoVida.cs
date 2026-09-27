using UnityEngine;

public class InimigoVida : MonoBehaviour
{
    public int vida = 100;

    public GameObject efeitoParticulaMorte;

    public AudioSource fonteDeAudio;
    public AudioClip audioMorte;

    public Vector3 offsetParticulaMorte;

    public BarraVidaInimigo barraVida;

    public GameObject prefabMedkit;
    public GameObject prefabMunicao;
    [Range(0f, 1f)]
    public float chanceDeDrop = 0.5f;

    // Quantos pontos esse inimigo dá ao morrer.
    public int pontosAoMorrer = 10;

    // Preenchido automaticamente pelo GerenciadorDeOrdas quando ele
    // instancia esse inimigo. Pode ficar vazio (null) sem problema,
    // por exemplo em uma cena de teste sem ordas.
    [HideInInspector]
    public GerenciadorDeOrdas gerenciadorDeOrdas;

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
        Instantiate(
            efeitoParticulaMorte,
            transform.position + offsetParticulaMorte,
            Quaternion.identity
        );

        AudioSource.PlayClipAtPoint(
            audioMorte,
            transform.position
        );

        DroparItem();

        if (SistemaDePontos.instancia != null)
        {
            SistemaDePontos.instancia.AdicionarPontos(pontosAoMorrer);
        }

        if (gerenciadorDeOrdas != null)
        {
            gerenciadorDeOrdas.InimigoMorreu();
        }

        Destroy(gameObject);
    }

    void DroparItem()
    {
        if (Random.value > chanceDeDrop)
        {
            return;
        }

        GameObject itemParaDropar;

        if (Random.value < 0.5f)
        {
            itemParaDropar = prefabMedkit;
        }
        else
        {
            itemParaDropar = prefabMunicao;
        }

        if (itemParaDropar == null)
        {
            Debug.LogWarning(
                "Prefab de drop não foi configurado no inimigo."
            );

            return;
        }

        Instantiate(
            itemParaDropar,
            transform.position,
            Quaternion.identity
        );
    }
}
using UnityEngine;
using System.Collections;
using TMPro;

// Coloque esse script em um GameObject vazio na cena (ex: "GerenciadorDeOrdas").
public class GerenciadorDeOrdas : MonoBehaviour
{
    [System.Serializable]
    public class Orda
    {
        // Um item por inimigo que deve nascer nessa orda.
        // Ex: orda 1 = [perseguidor, perseguidor], orda 2 = [perseguidor, perseguidor, perseguidor]
        public GameObject[] prefabsInimigos;
    }

    public Orda[] ordas;

    // Pontos de spawn espalhados pelo mapa.
    public Transform[] pontosDeSpawn;

    public TMP_Text textoOrda;
    public float tempoEntreOrdas = 5f;

    private int ordaAtual = 0;
    private int inimigosVivos = 0;

    void Start()
    {
        IniciarOrda(ordaAtual);
    }

    void IniciarOrda(int indiceOrda)
    {
        if (indiceOrda >= ordas.Length)
        {
            AtualizarTexto("VOCÊ VENCEU! Todas as ordas foram derrotadas.");
            Debug.Log("Todas as ordas foram concluídas!");
            return;
        }

        AtualizarTexto("ORDA " + (indiceOrda + 1) + " / " + ordas.Length);

        Orda orda = ordas[indiceOrda];
        inimigosVivos = orda.prefabsInimigos.Length;

        foreach (GameObject prefabInimigo in orda.prefabsInimigos)
        {
            Transform pontoDeSpawn = pontosDeSpawn[Random.Range(0, pontosDeSpawn.Length)];

            GameObject inimigoInstanciado = Instantiate(
                prefabInimigo,
                pontoDeSpawn.position,
                pontoDeSpawn.rotation
            );

            InimigoVida vidaDoInimigo = inimigoInstanciado.GetComponent<InimigoVida>();

            if (vidaDoInimigo != null)
            {
                vidaDoInimigo.gerenciadorDeOrdas = this;
            }
        }
    }

    // Chamado pelo InimigoVida.Morrer() de cada inimigo instanciado por essa orda.
    public void InimigoMorreu()
    {
        inimigosVivos--;

        if (inimigosVivos <= 0)
        {
            ordaAtual++;
            StartCoroutine(EsperarProximaOrda());
        }
    }

    IEnumerator EsperarProximaOrda()
    {
        AtualizarTexto("ORDA CONCLUÍDA! Próxima em " + tempoEntreOrdas + "s...");

        yield return new WaitForSeconds(tempoEntreOrdas);

        IniciarOrda(ordaAtual);
    }

    void AtualizarTexto(string texto)
    {
        if (textoOrda != null)
        {
            textoOrda.text = texto;
        }
    }
}
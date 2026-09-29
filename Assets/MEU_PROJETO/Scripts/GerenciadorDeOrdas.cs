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

    // Tempo de preparação depois de clicar em "Próxima Fase".
    public float tempoEntreOrdas = 5f;

    // Cada orda é uma fase do jogo.
    [SerializeField] private GerenciadorDeJogo gerenciadorDeJogo;
    [SerializeField] private Cronometro cronometro;

    // "static" sobrevive ao recarregamento da cena: é assim que o botão
    // "Reiniciar" do Game Over volta para a mesma orda em que o jogador perdeu.
    private static int ordaParaIniciar = 0;

    private int ordaAtual = 0;
    private int inimigosVivos = 0;

    public int NumeroDaOrdaAtual
    {
        get { return ordaAtual + 1; }
    }

    public bool EhUltimaOrda
    {
        get { return ordaAtual >= ordas.Length - 1; }
    }

    void Start()
    {
        ordaAtual = ordaParaIniciar;
        ordaParaIniciar = 0; // próxima vez que a cena abrir (ex: vindo do menu) começa na orda 1

        IniciarOrda(ordaAtual);
    }

    // Chamado pelo GerenciadorDeJogo antes de recarregar a cena no Game Over.
    public void ReiniciarNaOrdaAtual()
    {
        ordaParaIniciar = ordaAtual;
    }

    // Chamado pelo GerenciadorDeJogo no botão "Próxima Fase".
    public void IniciarProximaOrda()
    {
        ordaAtual++;
        StartCoroutine(EsperarProximaOrda());
    }

    void IniciarOrda(int indiceOrda)
    {
        AtualizarTexto("ORDA " + (indiceOrda + 1) + " / " + ordas.Length);
        cronometro.Iniciar();

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
            cronometro.Parar();
            gerenciadorDeJogo.Vitoria();
        }
    }

    IEnumerator EsperarProximaOrda()
    {
        AtualizarTexto("PREPARE-SE! ORDA " + (ordaAtual + 1) + " em " + tempoEntreOrdas + "s...");

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
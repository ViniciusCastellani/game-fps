using UnityEngine;
using System.Collections;
using TMPro;

public class GerenciadorDeOrdas : MonoBehaviour
{
    [System.Serializable]
    public class Orda
    {
        public GameObject[] prefabsInimigos;
    }

    public Orda[] ordas;

    public Transform[] pontosDeSpawn;

    public TMP_Text textoOrda;

    public float tempoEntreOrdas = 5f;

    [SerializeField] private GerenciadorDeJogo gerenciadorDeJogo;
    [SerializeField] private Cronometro cronometro;

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
        ordaParaIniciar = 0;

        IniciarOrda(ordaAtual);
    }

    public void ReiniciarNaOrdaAtual()
    {
        ordaParaIniciar = ordaAtual;
    }

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
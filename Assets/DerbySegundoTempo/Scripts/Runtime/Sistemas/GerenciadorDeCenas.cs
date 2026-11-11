using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorDeCenas : MonoBehaviour
{
    public const string PrimeiraFase = "Estadio_TPS";
    public const string SegundaFase = "Rua_TPS";
    public const string CenaDeModos = "SelecaoDeModo";
    public const string CenaDeCenarios = "SelecaoDeCenario";

    public string nomeDaProximaFase = PrimeiraFase;
    public string nomeDaTelaInicial = "TelaInicial";

    public bool TemProximaFase => !string.IsNullOrWhiteSpace(nomeDaProximaFase)
        && nomeDaProximaFase != SceneManager.GetActiveScene().name;

    public static bool EhCenaInfinitaValida(string cena)
    {
        return cena == PrimeiraFase || cena == SegundaFase;
    }

    public void ProximaFase()
    {
        SessaoDeJogo.DefinirNormal();
        Carregar(nomeDaProximaFase, true);
    }
    
    public void ReiniciarFase()
    {
        Carregar(SceneManager.GetActiveScene().name, false);
    }
    
    public void VoltarParaTelaInicial()
    {
        SessaoDeJogo.DefinirNormal();
        Carregar(nomeDaTelaInicial, true);
    }

    public void IniciarCampanha()
    {
        SessaoDeJogo.DefinirNormal();
        Carregar(PrimeiraFase, true);
    }

    public void IniciarModoInfinito(string cena)
    {
        if (!EhCenaInfinitaValida(cena))
        {
            Debug.LogError("Cenario infinito invalido: " + cena, this);
            return;
        }

        SessaoDeJogo.IniciarInfinito(cena);
        Carregar(cena, true);
    }

    public void VoltarParaEscolhaDeCenario()
    {
        SessaoDeJogo.DefinirNormal();
        Carregar(CenaDeCenarios, true);
    }

    public void AbrirSelecaoDeModo()
    {
        SessaoDeJogo.DefinirNormal();
        Carregar(CenaDeModos, true);
    }

    void Carregar(string cena, bool reiniciarHordas)
    {
        if (string.IsNullOrWhiteSpace(cena) || !Application.CanStreamedLevelBeLoaded(cena))
        {
            Debug.LogError("Cena ausente das configuracoes de build: " + cena, this);
            return;
        }
        if (reiniciarHordas) GerenciadorDeOrdas.LimparReinicio();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(cena);
    }

    public void Sair()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

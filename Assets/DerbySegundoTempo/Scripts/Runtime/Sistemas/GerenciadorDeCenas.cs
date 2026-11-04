using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorDeCenas : MonoBehaviour
{
    public const string PrimeiraFase = "Estadio_TPS_Backup_Visual";
    public const string SegundaFase = "Rua_TPS";

    public string nomeDaProximaFase = PrimeiraFase;
    public string nomeDaTelaInicial = "TelaInicial";

    public bool TemProximaFase => !string.IsNullOrWhiteSpace(nomeDaProximaFase)
        && nomeDaProximaFase != SceneManager.GetActiveScene().name;

    public void ProximaFase()
    {
        Carregar(nomeDaProximaFase, true);
    }
    
    public void ReiniciarFase()
    {
        Carregar(SceneManager.GetActiveScene().name, false);
    }
    
    public void VoltarParaTelaInicial()
    {
        Carregar(nomeDaTelaInicial, true);
    }

    public void IniciarCampanha()
    {
        Carregar(PrimeiraFase, true);
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

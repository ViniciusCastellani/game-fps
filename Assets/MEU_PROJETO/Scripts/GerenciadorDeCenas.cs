using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorDeCenas : MonoBehaviour
{
    public string nomeDaProximaFase;
    public string nomeDaTelaInicial;

    public void ProximaFase()
    {
        Debug.Log("Gerenciar Cenas Jogar" + nomeDaProximaFase);
        SceneManager.LoadScene(nomeDaProximaFase);
    }
    
    public void ReiniciarFase()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void VoltarParaTelaInicial()
    {
        SceneManager.LoadScene(nomeDaTelaInicial);
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
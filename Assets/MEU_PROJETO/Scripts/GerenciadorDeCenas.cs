using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorDeCenas : MonoBehaviour
{
    public string nomeDaProximaFase;
    public string nomeDaTelaInicial;

    // O pause agora é controlado pelo GerenciadorDeJogo (estados + Time.timeScale).
    // Este script cuida só da troca de cenas.

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
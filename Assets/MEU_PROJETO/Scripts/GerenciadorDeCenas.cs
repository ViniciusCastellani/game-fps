using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorDeCenas : MonoBehaviour
{
    public string nomeDaProximaFase;
    public string nomeDaTelaInicial;
    public GameObject telaDePause;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) | Input.GetKeyDown(KeyCode.P))
        {
            PausarJogo();
        }
    }

    public void ProximaFase()
    {
        Debug.Log("Gerenciar Cenas Jogar" + nomeDaProximaFase);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void ReiniciarFase()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void VoltarParaTelaInicial()
    {
        SceneManager.LoadScene(nomeDaTelaInicial);
    }

    public void PausarJogo()
    {
        telaDePause.SetActive(true);
        Time.timeScale = 0f; // para o tempo
    }
    
    public void ContinuarJogo()
    {
        telaDePause.SetActive(false);
        Time.timeScale = 1f; // volta pro tempo normal
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
#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BackupIluminacaoTests
{
    Scene cena;
    Scene anterior;
    float escala;
    RenderTexture alvo;
    Camera camera;
    string Pasta => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/IluminacaoPorOrda"));

    [UnityTest]
    public IEnumerator BackupPassaDaVitoriaParaSegundaHordaComMudancaVisivel()
    {
        anterior = SceneManager.GetActiveScene();
        escala = Time.timeScale;
        void Ativar(Scene carregada, LoadSceneMode modo)
        {
            if (carregada.path == "Assets/Scenes/Estadio_TPS_Backup_Visual.unity")
            {
                cena = carregada;
                SceneManager.SetActiveScene(cena);
            }
        }
        SceneManager.sceneLoaded += Ativar;
        try
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/Estadio_TPS_Backup_Visual.unity", new LoadSceneParameters(LoadSceneMode.Additive));
        }
        finally { SceneManager.sceneLoaded -= Ativar; }
        yield return null;
        var raizes = cena.GetRootGameObjects();
        Component Encontrar(string nome) => raizes.SelectMany(x => x.GetComponentsInChildren(Type.GetType(nome + ", Assembly-CSharp", true), true)).Single();
        var jogo = Encontrar("GerenciadorDeJogo");
        var hordas = Encontrar("GerenciadorDeOrdas");
        var luz = Encontrar("IluminacaoPorOrda");
        var tipoJogo = jogo.GetType();
        var tipoHordas = hordas.GetType();
        var tipoLuz = luz.GetType();
        var sol = (Light)tipoLuz.GetField("sol").GetValue(luz);
        Assert.AreSame(luz, tipoHordas.GetField("iluminacao").GetValue(hordas));
        tipoJogo.GetMethod("FecharTutorial").Invoke(jogo, null);
        Assert.AreEqual("Jogando", tipoJogo.GetProperty("Estado").GetValue(jogo).ToString());
        camera = raizes.SelectMany(x => x.GetComponentsInChildren<Camera>(true)).First(x => x.enabled);
        alvo = new RenderTexture(960, 540, 24);
        alvo.Create();
        camera.targetTexture = alvo;
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(1, tipoHordas.GetProperty("NumeroDaOrdaAtual").GetValue(hordas));
        Assert.That(sol.intensity, Is.EqualTo(2f).Within(.01f));
        Directory.CreateDirectory(Pasta);
        Color32[] Capturar(string nome)
        {
            var anteriorRT = RenderTexture.active;
            RenderTexture.active = alvo;
            var textura = new Texture2D(alvo.width, alvo.height, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, alvo.width, alvo.height), 0, 0);
            textura.Apply();
            File.WriteAllBytes(Pasta + "/" + nome + ".png", textura.EncodeToPNG());
            var pixels = textura.GetPixels32();
            UnityEngine.Object.DestroyImmediate(textura);
            RenderTexture.active = anteriorRT;
            return pixels;
        }
        var dia = Capturar("jogo_horda_1");
        // Percorre a mesma notificacao usada pelos inimigos ao morrer.
        for (int i = 0; i < 5; i++) tipoHordas.GetMethod("InimigoMorreu").Invoke(hordas, null);
        Assert.AreEqual("Vitoria", tipoJogo.GetProperty("Estado").GetValue(jogo).ToString());
        tipoHordas.GetField("tempoEntreOrdas").SetValue(hordas, 5f);
        tipoLuz.GetField("duracaoTransicao").SetValue(luz, .15f);
        tipoJogo.GetMethod("ProximaFase").Invoke(jogo, null);
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(.35f);
        Assert.AreEqual(2, tipoHordas.GetProperty("NumeroDaOrdaAtual").GetValue(hordas));
        Assert.That(sol.intensity, Is.EqualTo(1.5f).Within(.01f));
        Assert.That(sol.color.g, Is.EqualTo(.66f).Within(.01f));
        // A luz ja deve mudar durante a preparacao, antes dos inimigos da horda 2.
        var vivos = tipoHordas.GetField("inimigosVivos", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.AreEqual(0, vivos.GetValue(hordas));
        var tarde = Capturar("jogo_horda_2");
        double diferenca = 0;
        for (int i = 0; i < dia.Length; i++)
            diferenca += Math.Abs(dia[i].r - tarde[i].r) + Math.Abs(dia[i].g - tarde[i].g) + Math.Abs(dia[i].b - tarde[i].b);
        diferenca /= dia.Length * 3 * 255.0;
        File.WriteAllText(Pasta + "/jogo-report.txt", $"Cena real: horda 1 -> vitoria -> ProximaFase -> horda 2; diferenca visual={diferenca:F4}; sol={sol.color}, intensidade={sol.intensity}\n");
        Assert.Greater(diferenca, .01, "A imagem da camera do jogador precisa mudar entre as hordas.");
        Time.timeScale = 1f;
        // A sincronizacao continua correta quando o componente e reativado.
        ((Behaviour)luz).enabled = false;
        sol.intensity = 2f;
        ((Behaviour)luz).enabled = true;
        yield return null;
        yield return null;
        Assert.That(sol.intensity, Is.EqualTo(1.5f).Within(.01f));
        yield return new WaitForSeconds(5.1f);
        Assert.AreEqual(10, vivos.GetValue(hordas));
        Assert.That(sol.intensity, Is.EqualTo(1.5f).Within(.01f));
    }

    [UnityTearDown]
    public IEnumerator Limpar()
    {
        if (camera != null) camera.targetTexture = null;
        if (alvo != null) { alvo.Release(); UnityEngine.Object.DestroyImmediate(alvo); }
        if (cena.IsValid() && cena.isLoaded) yield return SceneManager.UnloadSceneAsync(cena);
        if (anterior.IsValid() && anterior.isLoaded) SceneManager.SetActiveScene(anterior);
        Time.timeScale = escala;
    }
}
#endif

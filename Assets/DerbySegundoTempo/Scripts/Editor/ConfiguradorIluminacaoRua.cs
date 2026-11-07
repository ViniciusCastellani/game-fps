using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ConfiguradorIluminacaoRua
{
    const string Cena = "Assets/DerbySegundoTempo/Cenas/Fases/Rua_TPS.unity";
    const string Materiais = "Assets/DerbySegundoTempo/Cenarios/Rua/Materiais/";
    const string Pasta = "Library/IluminacaoRua";
    static TestRunnerApi runner;

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Resultados());
        EditorApplication.update -= VerificarPedido;
        EditorApplication.update += VerificarPedido;
    }

    static void VerificarPedido()
    {
        if (!File.Exists(Pasta + ".request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string pedido = File.ReadAllText(Pasta + ".request").Trim();
        File.Delete(Pasta + ".request");
        Directory.CreateDirectory(Pasta);
        if (File.Exists(Pasta + "/erro.txt")) File.Delete(Pasta + "/erro.txt");
        try
        {
            if (pedido == "configurar") Configurar();
            Capturar();
            if (pedido != "previas") Testar();
        }
        catch (Exception erro) { File.WriteAllText(Pasta + "/erro.txt", erro.ToString()); Debug.LogException(erro); }
    }

    [MenuItem("Derby/Rua/Configurar tarde e noite nas hordas")]
    public static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Configure fora do Play Mode.");
        Directory.CreateDirectory(Pasta);
        File.Copy(Cena, Pasta + "/RuaAntesDaIluminacao.unity", true);
        Scene anterior = SceneManager.GetActiveScene();
        Scene cena = SceneManager.GetSceneByPath(Cena);
        bool abriu = !cena.isLoaded;
        if (abriu) cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(cena);
        try
        {
            var objetos = cena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var controle = objetos.Select(t => t.GetComponent<IluminacaoPorOrda>()).Single(c => c != null);
            controle.tardeENoite = true;
            controle.intensidadeDosPostes = 18f;
            var lampada = AssetDatabase.LoadAssetAtPath<Material>(Materiais + "LampadaNoturna.mat");
            if (lampada == null)
            {
                lampada = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(lampada, Materiais + "LampadaNoturna.mat");
            }
            lampada.SetColor("_BaseColor", new Color(1f, .84f, .58f));
            lampada.EnableKeyword("_EMISSION");
            lampada.SetColor("_EmissionColor", new Color(2.6f, 1.8f, .7f));
            var postes = objetos.Single(t => t.name == "Postes");
            var luzes = postes.Cast<Transform>().Select(poste =>
            {
                var luminaria = poste.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == "Luminaria");
                luminaria.sharedMaterial = lampada;
                Transform existente = poste.Find("LuzDoPoste");
                GameObject objeto = existente != null ? existente.gameObject : new GameObject("LuzDoPoste");
                objeto.transform.SetParent(poste, true);
                objeto.transform.position = luminaria.bounds.center + Vector3.down * .28f;
                Light luz = objeto.GetComponent<Light>();
                if (luz == null) luz = objeto.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = new Color(1f, .78f, .48f);
                luz.intensity = controle.intensidadeDosPostes;
                luz.range = 22f;
                luz.shadows = LightShadows.None;
                luz.lightmapBakeType = LightmapBakeType.Realtime;
                luz.enabled = false;
                return luz;
            }).ToArray();
            if (luzes.Length != 6) throw new InvalidOperationException("Esperados os seis postes existentes da rua.");
            controle.luzesDosPostes = luzes;
            controle.sol.color = controle.tarde.corSol;
            controle.sol.intensity = controle.tarde.intensidadeSol;
            controle.sol.transform.rotation = Quaternion.Euler(controle.tarde.rotacaoSol);
            RenderSettings.sun = controle.sol;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = controle.tarde.ceu;
            RenderSettings.ambientEquatorColor = controle.tarde.horizonte;
            RenderSettings.ambientGroundColor = controle.tarde.solo;
            var ceu = AssetDatabase.LoadAssetAtPath<Material>(Materiais + "CeuRua.mat");
            if (ceu == null)
            {
                ceu = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(ceu, Materiais + "CeuRua.mat");
            }
            ceu.SetColor("_SkyTint", controle.tarde.tonalidadeSkybox);
            ceu.SetColor("_GroundColor", controle.tarde.solo);
            ceu.SetFloat("_Exposure", controle.tarde.exposicaoSkybox);
            RenderSettings.skybox = ceu;
            EditorUtility.SetDirty(ceu);
            EditorUtility.SetDirty(lampada);
            EditorUtility.SetDirty(controle);
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Pasta + "/configuracao.txt", "Rua normal: primeira horda a tarde; ultima a noite; seis postes em tempo real; transicao gradual.\n");
        }
        finally
        {
            if (anterior.IsValid() && anterior.isLoaded) SceneManager.SetActiveScene(anterior);
            if (abriu) EditorSceneManager.CloseScene(cena, true);
        }
    }

    [MenuItem("Derby/Rua/Gerar previas de tarde e noite")]
    public static void Capturar()
    {
        Directory.CreateDirectory(Pasta);
        var ceuOriginal = RenderSettings.skybox;
        var solOriginal = RenderSettings.sun;
        var modoOriginal = RenderSettings.ambientMode;
        var cores = new[] { RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor };
        Scene cena = EditorSceneManager.OpenPreviewScene(Cena);
        RenderTexture alvo = null;
        try
        {
            var raizes = cena.GetRootGameObjects();
            foreach (var raiz in raizes)
            {
                foreach (var camera in raiz.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (var canvas in raiz.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
                foreach (var listener in raiz.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
            var controle = raizes.SelectMany(r => r.GetComponentsInChildren<IluminacaoPorOrda>(true)).Single();
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(Materiais + "CeuRua.mat");
            var objetoCamera = new GameObject("CameraPreviaIluminacaoRua");
            SceneManager.MoveGameObjectToScene(objetoCamera, cena);
            var captura = objetoCamera.AddComponent<Camera>();
            captura.enabled = false;
            captura.scene = cena;
            captura.fieldOfView = 65;
            captura.farClipPlane = 300;
            captura.clearFlags = CameraClearFlags.Skybox;
            captura.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            alvo = new RenderTexture(1440, 900, 24);
            alvo.Create();
            for (int horda = 1; horda <= 2; horda++)
            {
                controle.AplicarOrda(horda, true);
                captura.transform.position = new Vector3(0, 3.2f, -23);
                captura.transform.LookAt(new Vector3(0, 3, 20));
                Foto(captura, alvo, horda == 1 ? "tarde" : "noite");
                if (horda == 2)
                {
                    captura.transform.position = new Vector3(34, 38, -36);
                    captura.transform.LookAt(new Vector3(0, 0, 20));
                    Foto(captura, alvo, "noite_visao_geral");
                }
            }
        }
        finally
        {
            if (alvo != null) { alvo.Release(); Object.DestroyImmediate(alvo); }
            EditorSceneManager.ClosePreviewScene(cena);
            RenderSettings.skybox = ceuOriginal;
            RenderSettings.sun = solOriginal;
            RenderSettings.ambientMode = modoOriginal;
            RenderSettings.ambientSkyColor = cores[0];
            RenderSettings.ambientEquatorColor = cores[1];
            RenderSettings.ambientGroundColor = cores[2];
        }
    }

    static void Foto(Camera camera, RenderTexture alvo, string nome)
    {
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = alvo });
        var anterior = RenderTexture.active;
        var imagem = new Texture2D(alvo.width, alvo.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = alvo;
            imagem.ReadPixels(new Rect(0, 0, alvo.width, alvo.height), 0, 0);
            imagem.Apply();
            File.WriteAllBytes(Pasta + "/" + nome + ".png", imagem.EncodeToPNG());
        }
        finally { RenderTexture.active = anterior; Object.DestroyImmediate(imagem); }
    }

    [MenuItem("Derby/Rua/Testar iluminacao das hordas")]
    public static void Testar()
    {
        Directory.CreateDirectory(Pasta);
        SessionState.SetBool("IluminacaoRua.Testando", true);
        File.WriteAllText(Pasta + "/testes-status.txt", "Executando...\n");
        File.WriteAllText(Pasta + "/testes.txt", "");
        runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode,
            testNames = new[] { "IluminacaoRuaTests", "IluminacaoPorOrdaTests" } }));
    }

    class Resultados : ICallbacks
    {
        public void RunStarted(ITestAdaptor teste) { }
        public void TestStarted(ITestAdaptor teste) { }
        public void TestFinished(ITestResultAdaptor resultado)
        {
            if (SessionState.GetBool("IluminacaoRua.Testando", false) && !resultado.HasChildren)
                File.AppendAllText(Pasta + "/testes.txt", resultado.FullName + ": " + resultado.ResultState + "\n" + resultado.Message + "\n" + resultado.StackTrace + "\n");
        }
        public void RunFinished(ITestResultAdaptor resultado)
        {
            if (!SessionState.GetBool("IluminacaoRua.Testando", false)) return;
            File.WriteAllText(Pasta + "/testes-status.txt", $"{resultado.ResultState}; passou={resultado.PassCount}; falhou={resultado.FailCount}\n");
            TestRunnerApi.SaveResultToFile(resultado, Pasta + "/testes.xml");
            SessionState.SetBool("IluminacaoRua.Testando", false);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ValidadorPigMan
{
    static TestRunnerApi runner;
    static string Pasta => InstaladorPigMan.Relatorios;

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Resultados());
        if (File.Exists(Pasta + ".preview.request") || File.Exists(Pasta + ".tests.request"))
            EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || File.Exists(Pasta + ".request"))
        {
            EditorApplication.delayCall += Executar;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Pasta);
        try
        {
            if (File.Exists(Pasta + ".preview.request"))
            {
                File.Delete(Pasta + ".preview.request");
                Capturar();
            }
            if (File.Exists(Pasta + ".tests.request"))
            {
                File.Delete(Pasta + ".tests.request");
                Testar();
            }
        }
        catch (Exception erro) { File.WriteAllText(Pasta + "/validacao-error.txt", erro.ToString()); Debug.LogException(erro); }
    }

    [MenuItem("Derby/PigMan/Gerar previas das animacoes")]
    public static void Capturar()
    {
        Directory.CreateDirectory(Pasta);
        var cena = EditorSceneManager.NewPreviewScene();
        RenderTexture alvo = null;
        var texto = new StringBuilder();
        try
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(InstaladorPigMan.Prefab), cena);
            foreach (var canvas in go.GetComponentsInChildren<Canvas>()) canvas.enabled = false;
            var animator = go.GetComponentInChildren<Animator>();
            texto.AppendLine($"Avatar: humanoide={animator.avatar.isHuman}; valido={animator.avatar.isValid}; altura={animator.humanScale}");
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.shader.name != "Universal Render Pipeline/Lit")
                        throw new InvalidOperationException("Material nao configurado em " + r.name);
                    texto.AppendLine($"{r.name} | {m.name} | base={m.GetTexture("_BaseMap")?.name}");
                }
            var cameraGo = new GameObject("CameraPreviaPigMan");
            SceneManager.MoveGameObjectToScene(cameraGo, cena);
            var camera = cameraGo.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = cena;
            camera.fieldOfView = 40;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .145f, .17f);
            camera.transform.position = new Vector3(5, 2.7f, 7);
            camera.transform.LookAt(new Vector3(0, 1.7f, 0));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var luzGo = new GameObject("LuzPigMan");
            SceneManager.MoveGameObjectToScene(luzGo, cena);
            var luz = luzGo.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 2.5f;
            luz.transform.rotation = Quaternion.Euler(35, -145, 0);
            var apoioGo = new GameObject("LuzApoioPigMan");
            SceneManager.MoveGameObjectToScene(apoioGo, cena);
            var apoio = apoioGo.AddComponent<Light>();
            apoio.type = LightType.Directional;
            apoio.intensity = 1f;
            apoio.transform.rotation = Quaternion.Euler(20, 45, 0);
            var chao = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(chao, cena);
            Object.DestroyImmediate(chao.GetComponent<Collider>());
            chao.transform.position = new Vector3(0, -.035f, 0);
            chao.transform.localScale = new Vector3(12, .05f, 12);
            var materialChao = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            materialChao.SetColor("_BaseColor", new Color(.16f, .18f, .19f));
            chao.GetComponent<Renderer>().sharedMaterial = materialChao;
            alvo = new RenderTexture(1000, 1000, 24);
            alvo.Create();
            camera.targetTexture = alvo;
            animator.Rebind();
            foreach (var amostra in new[] {
                ("Combat/1H/HumanM@CombatIdle1H01", "01_pronto", .25f),
                ("Movement/Run/HumanM@Run01_Forward", "02_andando", .2f),
                ("Combat/1H/HumanM@Attack1H01_R", "03_ataque_preparo", .2f),
                ("Combat/1H/HumanM@Attack1H01_R", "04_ataque_golpe", .52f)
            })
            {
                animator.Rebind();
                var clip = InstaladorPigMan.Clipe(amostra.Item1);
                clip.SampleAnimation(animator.gameObject, amostra.Item3);
                foreach (var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen = true;
                camera.Render();
                var anterior = RenderTexture.active;
                RenderTexture.active = alvo;
                var imagem = new Texture2D(alvo.width, alvo.height, TextureFormat.RGB24, false);
                try
                {
                    imagem.ReadPixels(new Rect(0, 0, alvo.width, alvo.height), 0, 0);
                    imagem.Apply();
                    File.WriteAllBytes(Pasta + "/" + amostra.Item2 + ".png", imagem.EncodeToPNG());
                }
                finally { RenderTexture.active = anterior; Object.DestroyImmediate(imagem); }
                var mao = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var espada = go.GetComponentsInChildren<Transform>().Single(t => t.name == "EspadaPigMan");
                texto.AppendLine($"{amostra.Item2} | mao={mao.position:F3} | espada={espada.position:F3} | posicao={animator.transform.position:F3}");
            }
            File.WriteAllText(Pasta + "/preview.txt", texto.ToString());
            Object.DestroyImmediate(materialChao);
        }
        finally
        {
            if (alvo != null) { alvo.Release(); Object.DestroyImmediate(alvo); }
            EditorSceneManager.ClosePreviewScene(cena);
        }
    }

    [MenuItem("Derby/PigMan/Testar mini-boss em Play Mode")]
    public static void Testar()
    {
        Directory.CreateDirectory(Pasta);
        foreach (var cena in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
            if (cena.isDirty && !string.IsNullOrEmpty(cena.path))
            {
                File.Copy(cena.path, Pasta + "/" + Path.GetFileNameWithoutExtension(cena.path) + ".antes-dos-testes.unity", true);
                EditorSceneManager.SaveScene(cena);
            }
        SessionState.SetBool("PigMan.Testes", true);
        File.WriteAllText(Pasta + "/testes-status.txt", "Executando...");
        File.WriteAllText(Pasta + "/testes.txt", "");
        runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, testNames = new[] { "PigManTests" } }));
    }

    class Resultados : ICallbacks
    {
        public void RunStarted(ITestAdaptor testes) { }
        public void TestStarted(ITestAdaptor teste) { }
        public void TestFinished(ITestResultAdaptor resultado)
        {
            if (SessionState.GetBool("PigMan.Testes", false) && !resultado.HasChildren)
                File.AppendAllText(Pasta + "/testes.txt", resultado.Name + ": " + resultado.ResultState + "\n" + resultado.Message + "\n" + resultado.Output + "\n");
        }
        public void RunFinished(ITestResultAdaptor resultado)
        {
            if (!SessionState.GetBool("PigMan.Testes", false)) return;
            File.WriteAllText(Pasta + "/testes-status.txt", $"{resultado.ResultState}; passed={resultado.PassCount}; failed={resultado.FailCount}; skipped={resultado.SkipCount}\n");
            TestRunnerApi.SaveResultToFile(resultado, Pasta + "/testes.xml");
            SessionState.SetBool("PigMan.Testes", false);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfiguradorCameraTerceiraPessoa
{
    static readonly string[] Cenas = {
        "Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity",
        "Assets/DerbySegundoTempo/Cenas/Fases/Rua_TPS.unity"
    };
    static string Pasta => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/CameraTP"));
    static TestRunnerApi runner;

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Resultados());
        if (File.Exists(Pasta + ".request")) EditorApplication.delayCall += ExecutarPedido;
    }

    static void ExecutarPedido()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ExecutarPedido;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string pedido = File.ReadAllText(Pasta + ".request").Trim();
        File.Delete(Pasta + ".request");
        Directory.CreateDirectory(Pasta);
        try
        {
            if (pedido == "testar") Testar();
            else Instalar();
        }
        catch (Exception erro) { File.WriteAllText(Pasta + "/erro.txt", erro.ToString()); Debug.LogException(erro); }
    }

    [MenuItem("Derby/Camera terceira pessoa/Corrigir colisao nas fases")]
    public static void Instalar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Saia do Play Mode antes de configurar as cameras.");
        Directory.CreateDirectory(Pasta);
        foreach (string caminho in Cenas)
        {
            var cena = SceneManager.GetSceneByPath(caminho);
            bool abriu = !cena.isLoaded;
            if (abriu) cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive);
            try
            {
                string backup = Pasta + "/" + Path.GetFileNameWithoutExtension(caminho) + ".antes.unity";
                if (!File.Exists(backup)) File.Copy(caminho, backup);
                var alternar = cena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AlternarCamera>(true)).Single();
                var camera = alternar.cameraTerceiraPessoa;
                var corpo = camera.GetComponent<CinemachineThirdPersonFollow>();
                var alvo = camera.Follow.GetComponent<AlvoCameraTerceiraPessoa>();
                if (alvo == null)
                {
                    var mira = alternar.cameraPrimeiraPessoa.Follow;
                    var objeto = new GameObject("CameraAlvoTP");
                    objeto.transform.SetParent(alternar.transform, false);
                    alvo = objeto.AddComponent<AlvoCameraTerceiraPessoa>();
                    alvo.jogador = alternar.transform;
                    alvo.mira = mira;
                    alvo.deslocamentoLocal = new Vector3(0, alternar.transform.InverseTransformPoint(mira.position).y, 0);
                    Quaternion yaw = Quaternion.Euler(0, mira.eulerAngles.y, 0);
                    alvo.transform.SetPositionAndRotation(alternar.transform.position
                        + yaw * Vector3.Scale(alvo.deslocamentoLocal, alternar.transform.lossyScale), mira.rotation);
                    // Compensa a mudanca do alvo para preservar a posicao e a orbita da camera.
                    corpo.ShoulderOffset += Quaternion.Inverse(yaw) * (camera.Follow.position - alvo.transform.position);
                    camera.Follow = alvo.transform;
                }
                var colisao = corpo.AvoidObstacles;
                colisao.Enabled = true;
                colisao.CollisionFilter = Physics.DefaultRaycastLayers & ~(1 << LayerMask.NameToLayer("UI"));
                colisao.IgnoreTag = "Jogador";
                colisao.CameraRadius = .25f;
                colisao.DampingIntoCollision = 0;
                colisao.DampingFromCollision = .5f;
                corpo.AvoidObstacles = colisao;
                camera.PreviousStateIsValid = false;
                EditorUtility.SetDirty(alvo);
                EditorUtility.SetDirty(corpo);
                EditorUtility.SetDirty(camera);
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }
            finally { if (abriu) EditorSceneManager.CloseScene(cena, true); }
        }
        File.WriteAllText(Pasta + "/instalacao.txt", "Estadio e rua: alvo TP junto ao jogador; enquadramento preservado; colisao com solidos; raio 0.25; retorno 0.5s; jogador e gatilhos ignorados.\n");
        Debug.Log("[Camera TP] Colisao corrigida no estadio e na rua.");
    }

    [MenuItem("Derby/Camera terceira pessoa/Testar colisao em Play Mode")]
    public static void Testar()
    {
        Directory.CreateDirectory(Pasta);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var cena = SceneManager.GetSceneAt(i);
            if (!cena.isDirty || string.IsNullOrEmpty(cena.path)) continue;
            File.Copy(cena.path, Pasta + "/" + Path.GetFileNameWithoutExtension(cena.path) + ".antes-dos-testes.unity", true);
            EditorSceneManager.SaveScene(cena);
        }
        SessionState.SetBool("CameraTP.Testando", true);
        File.WriteAllText(Pasta + "/status.txt", "Executando...");
        File.WriteAllText(Pasta + "/testes.txt", "");
        runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "Derby.Camera.PlayModeTests" } }));
    }

    class Resultados : ICallbacks
    {
        public void RunStarted(ITestAdaptor testes) { }
        public void TestStarted(ITestAdaptor teste) { }
        public void TestFinished(ITestResultAdaptor resultado)
        {
            if (SessionState.GetBool("CameraTP.Testando", false) && !resultado.HasChildren)
                File.AppendAllText(Pasta + "/testes.txt", resultado.Name + ": " + resultado.ResultState + "\n" + resultado.Message + "\n" + resultado.Output + "\n");
        }
        public void RunFinished(ITestResultAdaptor resultado)
        {
            if (!SessionState.GetBool("CameraTP.Testando", false)) return;
            File.WriteAllText(Pasta + "/status.txt", $"{resultado.ResultState}; passed={resultado.PassCount}; failed={resultado.FailCount}; skipped={resultado.SkipCount}\n");
            TestRunnerApi.SaveResultToFile(resultado, Pasta + "/testes.xml");
            SessionState.SetBool("CameraTP.Testando", false);
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class ValidadorBolaFutebol
{
    static TestRunnerApi runner;
    static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/BolaFutebolTests"));

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Results());
        if (File.Exists(Folder + ".request")) EditorApplication.delayCall += RunRequested;
    }

    static void RunRequested()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequested;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Folder + ".request");
        Run();
    }

    [MenuItem("Derby/Bola de futebol/Testar fisica em Play Mode")]
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.path == "Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity" && activeScene.isDirty)
        {
            File.Copy(activeScene.path, Folder + "/BackupAntesDosTestes.unity", true);
            // Salva os ajustes que ja estao abertos na copia, sem recarregar ou descarta-los.
            EditorSceneManager.SaveScene(activeScene);
        }
        File.WriteAllText(Folder + "/status.txt", "Running Play Mode physics tests...");
        File.WriteAllText(Folder + "/results.txt", "");
        SessionState.SetBool("BolaFutebolTests.Active", true);
        runner.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.PlayMode,
            assemblyNames = new[] { "Derby.Bola.PlayModeTests" }
        }));
    }

    class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result)
        {
            if (SessionState.GetBool("BolaFutebolTests.Active", false) && !result.HasChildren)
                File.AppendAllText(Folder + "/results.txt", result.Name + ": " + result.ResultState + "\n" + result.Message + "\n" + result.Output + "\n");
        }
        public void RunFinished(ITestResultAdaptor result)
        {
            if (!SessionState.GetBool("BolaFutebolTests.Active", false)) return;
            File.WriteAllText(Folder + "/status.txt", $"{result.ResultState}; passed={result.PassCount}; failed={result.FailCount}; skipped={result.SkipCount}\n");
            TestRunnerApi.SaveResultToFile(result, Folder + "/results.xml");
            SessionState.SetBool("BolaFutebolTests.Active", false);
        }
    }
}

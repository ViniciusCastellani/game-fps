using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class ValidadorOrganizacao
{
    static TestRunnerApi runner;
    static double proximaVerificacao;
    const string Pasta = "Library/Organizacao";

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Resultados());
        EditorApplication.update += VerificarPedido;
    }

    static void VerificarPedido()
    {
        if (EditorApplication.timeSinceStartup < proximaVerificacao) return;
        proximaVerificacao = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Pasta + ".testar") || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Pasta + ".testar");
        Testar();
    }

    [MenuItem("Derby/Projeto/Validar campanha e organizacao")]
    public static void Testar()
    {
        Directory.CreateDirectory(Pasta);
        File.WriteAllText(Pasta + "/testes-status.txt", "Executando campanha em Play Mode...\n");
        SessionState.SetBool("Organizacao.Testando", true);
        runner.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.PlayMode,
            testNames = new[] { "CampanhaTests.MenuEstadioRuaVitoriaPausaEReinicioFuncionam" }
        }));
    }

    class Resultados : ICallbacks
    {
        public void RunStarted(ITestAdaptor teste) { }
        public void TestStarted(ITestAdaptor teste) { }
        public void TestFinished(ITestResultAdaptor resultado)
        {
            if (SessionState.GetBool("Organizacao.Testando", false) && !resultado.HasChildren)
                File.WriteAllText(Pasta + "/testes-detalhes.txt", resultado.FullName + ": " + resultado.ResultState + "\n" + resultado.Message + "\n" + resultado.StackTrace + "\n" + resultado.Output);
        }
        public void RunFinished(ITestResultAdaptor resultado)
        {
            if (!SessionState.GetBool("Organizacao.Testando", false)) return;
            File.WriteAllText(Pasta + "/testes-status.txt", $"{resultado.ResultState}; passou={resultado.PassCount}; falhou={resultado.FailCount}; ignorado={resultado.SkipCount}\n");
            TestRunnerApi.SaveResultToFile(resultado, Pasta + "/testes.xml");
            SessionState.SetBool("Organizacao.Testando", false);
        }
    }
}

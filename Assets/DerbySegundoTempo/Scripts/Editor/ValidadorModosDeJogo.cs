using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ValidadorModosDeJogo
{
    const string Raiz = "Assets/DerbySegundoTempo";
    const string CenaInicial = Raiz + "/Cenas/Menus/TelaInicial.unity";
    const string CenaEstadio = Raiz + "/Cenas/Fases/Estadio_TPS_Backup_Visual.unity";
    const string CenaRua = Raiz + "/Cenas/Fases/Rua_TPS.unity";
    const string CaminhoPigMan = Raiz + "/Prefabs/Inimigos/PigManMiniBoss.prefab";
    const string Pasta = "Library/ModosDeJogo";
    const string ChaveDeSessao = "ModosDeJogo.Testando";

    static TestRunnerApi runner;

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Resultados());
    }

    [MenuItem("Derby/Modos de Jogo/Validar modos de jogo")]
    public static void Validar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Valide fora do Play Mode.");

        Directory.CreateDirectory(Pasta);

        var linhas = new List<string>();
        ValidarCena(CenaInicial, linhas, ValidarTelaInicial);
        ValidarCena(CenaEstadio, linhas, ValidarFase);
        ValidarCena(CenaRua, linhas, ValidarFase);
        File.WriteAllText(Pasta + "/validacao.txt", string.Join("\n", linhas) + "\n");

        if (linhas.Any(linha => linha.StartsWith("FALHA")))
        {
            File.WriteAllText(Pasta + "/testes-status.txt", "Nao executado: validacao estatica falhou\n");
            Debug.LogError("Validacao estatica dos modos de jogo falhou. Veja " + Pasta + "/validacao.txt");
            return;
        }

        File.WriteAllText(Pasta + "/testes-status.txt", "Executando testes em Play Mode...\n");
        File.WriteAllText(Pasta + "/testes-detalhes.txt", "");
        SessionState.SetBool(ChaveDeSessao, true);
        runner.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.PlayMode,
            groupNames = new[]
            {
                "^ModosDeJogoTests\\.",
                "^CampanhaTests\\.",
                "^PigManTests\\.",
                "^EstacoesRuaTests\\."
            }
        }));
    }

    static void ValidarCena(string caminho, List<string> linhas, Action<Scene, string, List<string>> validacao)
    {
        Scene cena = default;
        try
        {
            cena = EditorSceneManager.OpenPreviewScene(caminho);
            validacao(cena, caminho, linhas);
        }
        catch (Exception erro)
        {
            linhas.Add("FALHA: " + caminho + ": " + erro.Message);
        }
        finally
        {
            if (cena.IsValid()) EditorSceneManager.ClosePreviewScene(cena);
        }
    }

    static void Verificar(List<string> linhas, string cena, string descricao, bool condicao)
    {
        linhas.Add(condicao ? "OK: " + cena + ": " + descricao : "FALHA: " + cena + ": " + descricao);
    }

    static void ValidarTelaInicial(Scene cena, string caminho, List<string> linhas)
    {
        string nome = cena.name;
        Transform[] transforms = TodosOsTransforms(cena);

        Verificar(linhas, nome, "um PainelEscolhaDeModo", transforms.Count(t => t.name == "PainelEscolhaDeModo") == 1);
        Verificar(linhas, nome, "um PainelEscolhaDeCenario", transforms.Count(t => t.name == "PainelEscolhaDeCenario") == 1);

        MenuDeModos[] menus = TodosOsComponentes<MenuDeModos>(cena);
        Verificar(linhas, nome, "um MenuDeModos", menus.Length == 1);
        if (menus.Length == 1)
        {
            Verificar(linhas, nome, "MenuDeModos com todas as referencias", SemReferenciaNula(menus[0], null));
        }

        Button botaoIniciar = BotaoPorNome(transforms, "BotaoIniciar");
        Verificar(linhas, nome, "BotaoIniciar com 1 chamada AbrirEscolhaDeModo em MenuDeModos",
            botaoIniciar != null
            && botaoIniciar.onClick.GetPersistentEventCount() == 1
            && botaoIniciar.onClick.GetPersistentTarget(0) is MenuDeModos
            && botaoIniciar.onClick.GetPersistentMethodName(0) == "AbrirEscolhaDeModo");

        Button botaoSair = BotaoPorNome(transforms, "BotaoSair");
        bool sairLigado = false;
        if (botaoSair != null)
        {
            for (int i = 0; i < botaoSair.onClick.GetPersistentEventCount(); i++)
            {
                if (botaoSair.onClick.GetPersistentMethodName(i) == "Sair") sairLigado = true;
            }
        }
        Verificar(linhas, nome, "BotaoSair ainda chama Sair", sairLigado);
    }

    static void ValidarFase(Scene cena, string caminho, List<string> linhas)
    {
        string nome = cena.name;
        Transform[] transforms = TodosOsTransforms(cena);

        GerenciadorModoInfinito[] gerenciadores = TodosOsComponentes<GerenciadorModoInfinito>(cena);
        GeradorDeInimigosInfinito[] geradores = TodosOsComponentes<GeradorDeInimigosInfinito>(cena);

        Verificar(linhas, nome, "um GerenciadorModoInfinito", gerenciadores.Length == 1);
        Verificar(linhas, nome, "um GeradorDeInimigosInfinito", geradores.Length == 1);

        if (gerenciadores.Length == 1 && geradores.Length == 1)
        {
            Transform pai = gerenciadores[0].transform.parent;
            Verificar(linhas, nome, "componentes em 00_Sistemas/ModoInfinito",
                gerenciadores[0].gameObject == geradores[0].gameObject
                && gerenciadores[0].name == "ModoInfinito"
                && pai != null && pai.name == "00_Sistemas");

            Verificar(linhas, nome, "GerenciadorModoInfinito com referencias obrigatorias",
                SemReferenciaNula(gerenciadores[0], "textoObjetivoTutorial"));

            SerializedObject gerenciadorSerializado = new SerializedObject(gerenciadores[0]);
            SerializedProperty somenteNormal = gerenciadorSerializado.FindProperty("objetosSomenteModoNormal");
            Verificar(linhas, nome, "objetosSomenteModoNormal com 2 itens", somenteNormal != null && somenteNormal.arraySize == 2);

            SerializedObject geradorSerializado = new SerializedObject(geradores[0]);
            SerializedProperty comuns = geradorSerializado.FindProperty("prefabsComuns");
            bool comunsValidos = comuns != null && comuns.arraySize >= 1;
            if (comunsValidos)
            {
                for (int i = 0; i < comuns.arraySize; i++)
                {
                    Object prefab = comuns.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (prefab == null || AssetDatabase.GetAssetPath(prefab) == CaminhoPigMan) comunsValidos = false;
                }
            }
            Verificar(linhas, nome, "prefabsComuns com 1 ou mais prefabs e sem PigMan", comunsValidos);

            SerializedProperty miniBoss = geradorSerializado.FindProperty("prefabMiniBoss");
            Verificar(linhas, nome, "prefabMiniBoss e o PigMan",
                miniBoss != null && miniBoss.objectReferenceValue != null
                && AssetDatabase.GetAssetPath(miniBoss.objectReferenceValue) == CaminhoPigMan);
        }

        Transform hud = transforms.FirstOrDefault(t => t.name == "HUDInfinito");
        Verificar(linhas, nome, "HUDInfinito inativo", hud != null && !hud.gameObject.activeSelf);

        Transform resultado = transforms.FirstOrDefault(t => t.name == "TelaDeResultadoInfinito");
        Verificar(linhas, nome, "TelaDeResultadoInfinito inativa", resultado != null && !resultado.gameObject.activeSelf);
    }

    static bool SemReferenciaNula(Object alvo, string campoIgnorado)
    {
        SerializedObject objetoSerializado = new SerializedObject(alvo);
        SerializedProperty propriedade = objetoSerializado.GetIterator();
        bool tudoLigado = true;

        while (propriedade.NextVisible(true))
        {
            if (propriedade.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (propriedade.name == "m_Script") continue;
            if (campoIgnorado != null && propriedade.propertyPath.StartsWith(campoIgnorado)) continue;
            if (propriedade.objectReferenceValue == null) tudoLigado = false;
        }

        return tudoLigado;
    }

    static Transform[] TodosOsTransforms(Scene cena)
    {
        return cena.GetRootGameObjects().SelectMany(raiz => raiz.GetComponentsInChildren<Transform>(true)).ToArray();
    }

    static T[] TodosOsComponentes<T>(Scene cena) where T : Component
    {
        return cena.GetRootGameObjects().SelectMany(raiz => raiz.GetComponentsInChildren<T>(true)).ToArray();
    }

    static Button BotaoPorNome(Transform[] transforms, string nome)
    {
        Transform encontrado = transforms.FirstOrDefault(t => t.name == nome);
        return encontrado != null ? encontrado.GetComponent<Button>() : null;
    }

    class Resultados : ICallbacks
    {
        public void RunStarted(ITestAdaptor teste) { }
        public void TestStarted(ITestAdaptor teste) { }

        public void TestFinished(ITestResultAdaptor resultado)
        {
            if (!SessionState.GetBool(ChaveDeSessao, false) || resultado.HasChildren) return;

            string detalhes = resultado.FullName + ": " + resultado.ResultState + "\n" + resultado.Message + "\n"
                + resultado.StackTrace + "\n" + resultado.Output + "\n";
            File.AppendAllText(Pasta + "/testes-detalhes.txt", detalhes);
        }

        public void RunFinished(ITestResultAdaptor resultado)
        {
            if (!SessionState.GetBool(ChaveDeSessao, false)) return;

            File.WriteAllText(Pasta + "/testes-status.txt",
                $"{resultado.ResultState}; passou={resultado.PassCount}; falhou={resultado.FailCount}; ignorado={resultado.SkipCount}\n");
            TestRunnerApi.SaveResultToFile(resultado, Pasta + "/testes.xml");
            SessionState.SetBool(ChaveDeSessao, false);
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CampanhaTests
{
    const string Cenas = "Assets/DerbySegundoTempo/Cenas/";
    Component Encontrar(string tipo) => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(x => x.GetComponentsInChildren(Type.GetType(tipo + ", Assembly-CSharp", true), true)).Single();
    object Campo(Component alvo, string nome) => alvo.GetType().GetField(nome,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).GetValue(alvo);
    object Propriedade(Component alvo, string nome) => alvo.GetType().GetProperty(nome).GetValue(alvo);
    void Chamar(Component alvo, string nome) => alvo.GetType().GetMethod(nome).Invoke(alvo, null);

    IEnumerator EsperarCena(string nome)
    {
        for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != nome; i++) yield return null;
        Assert.AreEqual(nome, SceneManager.GetActiveScene().name);
        yield return null;
    }

    void ConferirCenaJogavel()
    {
        var raizes = SceneManager.GetActiveScene().GetRootGameObjects();
        Assert.IsTrue(raizes.Any(x => x.name == "01_Jogador"));
        Assert.IsTrue(raizes.Any(x => x.name == "06_Interface"));
        var jogo = Encontrar("GerenciadorDeJogo");
        foreach (string tela in new[] { "telaDePause", "telaDeGameOver", "telaDeVitoria", "telaDeTutorial" })
        {
            Assert.IsNotNull(Campo(jogo, tela), tela);
            Assert.AreEqual(SceneManager.GetActiveScene(), ((GameObject)Campo(jogo, tela)).scene);
        }
        foreach (var objeto in raizes.SelectMany(x => x.GetComponentsInChildren<Transform>(true)))
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objeto.gameObject), objeto.name);
        var hordas = Encontrar("GerenciadorDeOrdas");
        Assert.IsNotNull(Campo(hordas, "inimigosAtivos"));
        var primeira = ((Array)Campo(hordas, "ordas")).GetValue(0);
        var prefab = ((GameObject[])primeira.GetType().GetField("prefabsInimigos").GetValue(primeira))[0];
        var filtro = new NavMeshQueryFilter { agentTypeID = prefab.GetComponent<NavMeshAgent>().agentTypeID, areaMask = NavMesh.AllAreas };
        foreach (var ponto in (Transform[])Campo(hordas, "pontosDeSpawn"))
            Assert.IsTrue(NavMesh.SamplePosition(ponto.position, out _, 2, filtro), ponto.name);
        var jogador = Encontrar("JogadorMovimento");
        Assert.IsTrue(NavMesh.SamplePosition(jogador.transform.position, out var destino, 3, filtro));
        foreach (var ponto in (Transform[])Campo(hordas, "pontosDeSpawn"))
        {
            var caminho = new NavMeshPath();
            Assert.IsTrue(NavMesh.CalculatePath(ponto.position, destino.position, filtro, caminho), ponto.name);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, caminho.status, ponto.name);
        }
        foreach (var agente in raizes.SelectMany(x => x.GetComponentsInChildren<NavMeshAgent>()))
            Assert.IsTrue(agente.isOnNavMesh, agente.name);
    }

    IEnumerator ConcluirHordas()
    {
        var jogo = Encontrar("GerenciadorDeJogo");
        Chamar(jogo, "FecharTutorial");
        var hordas = Encontrar("GerenciadorDeOrdas");
        var ordas = (Array)Campo(hordas, "ordas");
        for (int indice = 0; indice < ordas.Length; indice++)
        {
            var orda = ordas.GetValue(indice);
            var inimigos = (Array)orda.GetType().GetField("prefabsInimigos").GetValue(orda);
            for (int i = 0; i < inimigos.Length; i++) Chamar(hordas, "InimigoMorreu");
            Assert.AreEqual("Vitoria", Propriedade(jogo, "Estado").ToString());
            if (indice < ordas.Length - 1)
            {
                hordas.GetType().GetField("tempoEntreOrdas").SetValue(hordas, .01f);
                Chamar(jogo, "ProximaFase");
                yield return new WaitForSeconds(.05f);
                Assert.AreEqual("Jogando", Propriedade(jogo, "Estado").ToString());
            }
        }
    }

    [UnityTest]
    public IEnumerator MenuEstadioRuaVitoriaPausaEReinicioFuncionam()
    {
        CollectionAssert.AreEqual(new[]
        {
            Cenas + "Menus/TelaInicial.unity", Cenas + "Fases/Estadio_TPS.unity",
            Cenas + "Fases/Rua_TPS.unity", Cenas + "Menus/TelaPausa.unity",
            Cenas + "Menus/SelecaoDeModo.unity", Cenas + "Menus/SelecaoDeCenario.unity"
        }, EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray());
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Menus/TelaInicial.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        Chamar(Encontrar("GerenciadorDeCenas"), "ProximaFase");
        yield return EsperarCena("Estadio_TPS");
        ConferirCenaJogavel();
        yield return ConcluirHordas();
        Chamar(Encontrar("GerenciadorDeJogo"), "ProximaFase");
        yield return EsperarCena("Rua_TPS");
        ConferirCenaJogavel();
        var jogo = Encontrar("GerenciadorDeJogo");
        Chamar(jogo, "FecharTutorial");
        Chamar(jogo, "Pausar");
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(((GameObject)Campo(jogo, "telaDePause")).activeSelf);
        Chamar(jogo, "Continuar");
        Assert.AreEqual(1f, Time.timeScale);
        yield return ConcluirHordas();
        Assert.IsFalse((bool)Propriedade(Encontrar("GerenciadorDeCenas"), "TemProximaFase"));
        Chamar(Encontrar("GerenciadorDeJogo"), "ProximaFase");
        yield return EsperarCena("Estadio_TPS");
        Assert.AreEqual(1, Propriedade(Encontrar("GerenciadorDeOrdas"), "NumeroDaOrdaAtual"));
        // O checkpoint de uma horda vale somente para reiniciar a mesma cena.
        jogo = Encontrar("GerenciadorDeJogo");
        Chamar(jogo, "FecharTutorial");
        var hordas = Encontrar("GerenciadorDeOrdas");
        for (int i = 0; i < 5; i++) Chamar(hordas, "InimigoMorreu");
        Chamar(jogo, "ProximaFase");
        Chamar(jogo, "ReiniciarFase");
        yield return null;
        yield return null;
        Assert.AreEqual(2, Propriedade(Encontrar("GerenciadorDeOrdas"), "NumeroDaOrdaAtual"));
        Chamar(Encontrar("GerenciadorDeJogo"), "VoltarAoMenu");
        yield return EsperarCena("TelaInicial");
        Assert.AreEqual(1f, Time.timeScale);
        Chamar(Encontrar("GerenciadorDeCenas"), "ProximaFase");
        yield return EsperarCena("Estadio_TPS");
        Assert.AreEqual(1, Propriedade(Encontrar("GerenciadorDeOrdas"), "NumeroDaOrdaAtual"));
    }

    [UnityTearDown]
    public IEnumerator Limpar()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var tipo = Type.GetType("GerenciadorDeOrdas, Assembly-CSharp", true);
        tipo.GetMethod("LimparReinicio").Invoke(null, null);
        yield return null;
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class IluminacaoRuaTests
{
    const string Cena = "Assets/DerbySegundoTempo/Cenas/Fases/Rua_TPS.unity";
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Type Tipo(string nome) => Type.GetType(nome + ", Assembly-CSharp", true);
    static object Campo(object alvo, string nome) => alvo.GetType().GetField(nome, Flags).GetValue(alvo);
    static void Definir(object alvo, string nome, object valor) => alvo.GetType().GetField(nome, Flags).SetValue(alvo, valor);
    static object Chamar(object alvo, string nome, params object[] args) => alvo.GetType().GetMethod(nome, Flags).Invoke(alvo, args);
    static object Propriedade(object alvo, string nome) => alvo.GetType().GetProperty(nome).GetValue(alvo);
    static void Sessao(string metodo, params object[] args) => Tipo("SessaoDeJogo").GetMethod(metodo).Invoke(null, args);
    static Component Encontrar(string nome) => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(r => r.GetComponentsInChildren(Tipo(nome), true)).Single();

    static IEnumerator CarregarRua()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cena, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
    }

    static void ConferirNoite(Component controle)
    {
        var sol = (Light)Campo(controle, "sol");
        var noite = Campo(controle, "noite");
        Assert.That(sol.intensity, Is.EqualTo((float)Campo(noite, "intensidadeSol")).Within(.01f));
        Assert.Greater(sol.intensity, .3f, "A luz de preenchimento precisa manter os inimigos visiveis.");
        Assert.Greater(RenderSettings.ambientSkyColor.b, .2f);
        Assert.Less(RenderSettings.skybox.GetFloat("_Exposure"), .5f);
        var postes = (Light[])Campo(controle, "luzesDosPostes");
        Assert.AreEqual(6, postes.Length);
        Assert.IsTrue(postes.All(l => l != null && l.enabled && l.intensity > 0f && l.range >= 18f));
    }

    [UnityTest]
    public IEnumerator RuaNormalPassaDaTardeParaNoiteEReiniciaComPostesAcesos()
    {
        Sessao("Reiniciar");
        Tipo("GerenciadorDeOrdas").GetMethod("LimparReinicio").Invoke(null, null);
        yield return CarregarRua();
        var controle = Encontrar("IluminacaoPorOrda");
        var hordas = Encontrar("GerenciadorDeOrdas");
        var jogo = Encontrar("GerenciadorDeJogo");
        var jogador = Encontrar("JogadorVida");
        Definir(jogador, "vidaMaxima", 100000);
        Definir(jogador, "vidaAtual", 100000);
        Assert.IsTrue((bool)Campo(controle, "tardeENoite"));
        Assert.AreEqual(1, Propriedade(hordas, "NumeroDaOrdaAtual"));
        var tarde = Campo(controle, "tarde");
        var sol = (Light)Campo(controle, "sol");
        Assert.That(sol.intensity, Is.EqualTo((float)Campo(tarde, "intensidadeSol")).Within(.01f));
        Assert.That(sol.color.g, Is.EqualTo(.66f).Within(.01f));
        Assert.IsTrue(((Light[])Campo(controle, "luzesDosPostes")).All(l => !l.enabled));
        int vivos = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren(Tipo("InimigoVida"))).Count();
        for (int i = 0; i < vivos; i++) Chamar(hordas, "InimigoMorreu");
        Assert.AreEqual("Vitoria", Propriedade(jogo, "Estado").ToString());
        Definir(controle, "duracaoTransicao", .08f);
        Chamar(jogo, "ProximaFase");
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(2, Propriedade(hordas, "NumeroDaOrdaAtual"));
        ConferirNoite(controle);
        // O reinicio da ultima horda precisa carregar a noite imediatamente.
        Chamar(jogo, "ReiniciarFase");
        yield return null;
        yield return null;
        controle = Encontrar("IluminacaoPorOrda");
        Assert.AreEqual(2, Propriedade(Encontrar("GerenciadorDeOrdas"), "NumeroDaOrdaAtual"));
        ConferirNoite(controle);
    }

    [UnityTest]
    public IEnumerator RuaInfinitaPreservaLuzDoDia()
    {
        Sessao("IniciarInfinito", "Rua_TPS");
        yield return CarregarRua();
        var controle = Encontrar("IluminacaoPorOrda");
        var sol = (Light)Campo(controle, "sol");
        var dia = Campo(controle, "dia");
        Assert.That(sol.intensity, Is.EqualTo((float)Campo(dia, "intensidadeSol")).Within(.01f));
        Assert.IsTrue(((Light[])Campo(controle, "luzesDosPostes")).All(l => !l.enabled));
        Assert.AreEqual("Tutorial", Propriedade(Encontrar("GerenciadorDeJogo"), "Estado").ToString());
    }

    [UnityTearDown]
    public IEnumerator Limpar()
    {
        Time.timeScale = 1f;
        Sessao("Reiniciar");
        Tipo("GerenciadorDeOrdas").GetMethod("LimparReinicio").Invoke(null, null);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/DerbySegundoTempo/Cenas/Menus/TelaInicial.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
    }
}
#endif

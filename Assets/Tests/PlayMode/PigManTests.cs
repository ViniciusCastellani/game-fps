#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class PigManTests
{
    const string Prefab = "Assets/DerbySegundoTempo/Prefabs/Inimigos/PigManMiniBoss.prefab";
    const string Cenas = "Assets/DerbySegundoTempo/Cenas/";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Type Tipo(string nome) => Type.GetType(nome + ", Assembly-CSharp", true);
    static object Campo(Component alvo, string nome) => alvo.GetType().GetField(nome, Flags).GetValue(alvo);
    static void Definir(Component alvo, string nome, object valor) => alvo.GetType().GetField(nome, Flags).SetValue(alvo, valor);
    static object Chamar(Component alvo, string nome, params object[] argumentos) => alvo.GetType().GetMethod(nome, Flags).Invoke(alvo, argumentos);
    static Component Componente(GameObject go, string nome) => go.GetComponent(Tipo(nome));
    static Component Encontrar(string nome) => Object.FindObjectsByType(Tipo(nome)).Cast<Component>().Single();
    static Component[] Vidas() => Object.FindObjectsByType(Tipo("InimigoVida")).Cast<Component>().ToArray();

    [Test]
    public void PrefabUsaAvatarHumanoideTexturasOriginaisEEspadaNaMaoDireita()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        Assert.IsNotNull(prefab);
        var animator = prefab.GetComponentInChildren<Animator>();
        Assert.IsTrue(animator.avatar.isValid && animator.avatar.isHuman);
        Assert.IsFalse(animator.applyRootMotion);
        Assert.AreEqual(3, animator.runtimeAnimatorController.animationClips.Distinct().Count());
        Assert.IsTrue(animator.runtimeAnimatorController.animationClips.All(c => c.humanMotion));
        var espada = prefab.GetComponentsInChildren<Transform>().Single(t => t.name == "EspadaPigMan");
        Assert.AreEqual("hand.R", espada.parent.name);
        Assert.IsFalse(prefab.GetComponentsInChildren<Renderer>().Any(r => r.name.StartsWith("Axe")));
        foreach (var r in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
            foreach (var m in r.sharedMaterials)
            {
                Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, r.name);
                Assert.IsNotNull(m.GetTexture("_BaseMap"), r.name);
                StringAssert.Contains("/pigman/textures/", AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")));
                Assert.AreEqual(TextureImporterType.NormalMap,
                    ((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m.GetTexture("_BumpMap")))).textureType);
            }
        var porco = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DerbySegundoTempo/Prefabs/Inimigos/InimigoPerseguidor.prefab");
        Assert.Less(prefab.GetComponent<NavMeshAgent>().speed, porco.GetComponent<NavMeshAgent>().speed);
        Assert.Greater((int)Campo(Componente(prefab, "InimigoVida"), "vida"), (int)Campo(Componente(porco, "InimigoVida"), "vida"));
        Assert.AreEqual(porco.GetComponent<NavMeshAgent>().agentTypeID, prefab.GetComponent<NavMeshAgent>().agentTypeID);
    }

    IEnumerator ConferirHordas(string cena)
    {
        Tipo("GerenciadorDeOrdas").GetMethod("LimparReinicio").Invoke(null, null);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Fases/" + cena + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var jogo = Encontrar("GerenciadorDeJogo");
        Chamar(jogo, "FecharTutorial");
        var hordas = Encontrar("GerenciadorDeOrdas");
        Assert.AreEqual(5, Vidas().Length);
        Assert.IsFalse(Vidas().Any(v => Componente(v.gameObject, "PigManAnimador") != null));
        foreach (var vida in Vidas()) Chamar(vida, "ReceberDano", 10000);
        yield return null;
        Assert.AreEqual("Vitoria", jogo.GetType().GetProperty("Estado").GetValue(jogo).ToString());
        Definir(hordas, "tempoEntreOrdas", .01f);
        Chamar(jogo, "ProximaFase");
        yield return new WaitForSeconds(.1f);
        var vivos = Vidas();
        Assert.AreEqual(11, vivos.Length, "A horda final preserva os dez porcos e acrescenta um mini-boss.");
        var boss = vivos.Single(v => Componente(v.gameObject, "PigManAnimador") != null);
        var agente = boss.GetComponent<NavMeshAgent>();
        Assert.IsTrue(agente.isOnNavMesh);
        var posicaoInicial = boss.transform.position;
        yield return new WaitForSeconds(.8f);
        Assert.Greater(Vector3.Distance(posicaoInicial, boss.transform.position), .05f, "O mini-boss deve perseguir o jogador.");
        Assert.LessOrEqual(agente.velocity.magnitude, agente.speed + .1f);
        Assert.Greater(boss.GetComponentInChildren<Animator>().GetFloat("Velocidade"), .1f);
        foreach (var normal in vivos.Where(v => v != boss)) Chamar(normal, "ReceberDano", 10000);
        yield return null;
        Assert.AreEqual("Jogando", jogo.GetType().GetProperty("Estado").GetValue(jogo).ToString(), "O PigMan deve impedir a vitoria enquanto estiver vivo.");
        // Reiniciar a horda final recria apenas um mini-boss.
        Chamar(hordas, "ReiniciarNaOrdaAtual");
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Fases/" + cena + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        jogo = Encontrar("GerenciadorDeJogo");
        Chamar(jogo, "FecharTutorial");
        vivos = Vidas();
        Assert.AreEqual(11, vivos.Length);
        boss = vivos.Single(v => Componente(v.gameObject, "PigManAnimador") != null);
        foreach (var normal in vivos.Where(v => v != boss)) Chamar(normal, "ReceberDano", 10000);
        Chamar(boss, "ReceberDano", 10000);
        yield return null;
        Assert.AreEqual("Vitoria", jogo.GetType().GetProperty("Estado").GetValue(jogo).ToString());
    }

    [UnityTest]
    public IEnumerator EstadioTemUmMiniBossApenasNaHordaDois() => ConferirHordas("Estadio_TPS_Backup_Visual");

    [UnityTest]
    public IEnumerator RuaTemUmMiniBossApenasNaHordaDois() => ConferirHordas("Rua_TPS");

    [UnityTest]
    public IEnumerator EspadaCausaDanoNoMomentoDoGolpeEPermiteEsquivar()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Menus/TelaInicial.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        Time.timeScale = 1;
        var jogador = new GameObject("JogadorTestePigMan");
        jogador.tag = "Jogador";
        var vida = jogador.AddComponent(Tipo("JogadorVida"));
        Definir(vida, "vidaMaxima", 1000);
        jogador.transform.position = new Vector3(0, 0, 2.4f);
        var grupo = new GameObject("PigManTesteInativo");
        grupo.SetActive(false);
        var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), grupo.transform);
        boss.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        boss.GetComponent<NavMeshAgent>().enabled = false;
        ((Behaviour)Componente(boss, "InimigoPerseguidor")).enabled = false;
        grupo.SetActive(true);
        yield return null;
        var ataque = Componente(boss, "InimigoAtaque");
        Assert.IsTrue((bool)ataque.GetType().GetProperty("Atacando").GetValue(ataque));
        int inicial = (int)Campo(vida, "vidaAtual");
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(inicial, Campo(vida, "vidaAtual"), "O preparo do golpe nao causa dano.");
        var animator = boss.GetComponentInChildren<Animator>();
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("GolpeDeEspada"));
        var mao = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Vector3 antes = mao.position;
        yield return new WaitForSeconds(.3f);
        Assert.Greater(Vector3.Distance(antes, mao.position), .03f, "O clipe humanoide deve mover a mao da espada.");
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(inicial - (int)Campo(ataque, "dano"), Campo(vida, "vidaAtual"));
        yield return new WaitForSeconds((float)Campo(ataque, "duracaoDaAnimacao") + (float)Campo(ataque, "tempoEntreAtaques") - .65f);
        for (int i = 0; i < 100 && !(bool)ataque.GetType().GetProperty("Atacando").GetValue(ataque); i++) yield return null;
        Assert.IsTrue((bool)ataque.GetType().GetProperty("Atacando").GetValue(ataque));
        int antesDeEsquivar = (int)Campo(vida, "vidaAtual");
        jogador.transform.position = new Vector3(0, 0, 8);
        yield return new WaitForSeconds(.8f);
        Assert.AreEqual(antesDeEsquivar, Campo(vida, "vidaAtual"), "Sair do alcance antes do impacto deve evitar o dano.");
        Object.Destroy(grupo);
        Object.Destroy(jogador);
    }

    [UnityTearDown]
    public IEnumerator Limpar()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Tipo("GerenciadorDeOrdas").GetMethod("LimparReinicio").Invoke(null, null);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Menus/TelaInicial.unity", new LoadSceneParameters(LoadSceneMode.Single));
    }
}
#endif

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

    [Test]
    public void PrefabTemFoleyPesadoEAudioDeEspadaEm3D()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        var passos = Componente(prefab, "InimigoSomDePassos");
        var sons = (AudioClip[])Campo(passos, "sonsDePasso");
        Assert.AreEqual(3, sons.Length);
        Assert.AreEqual(3, sons.Distinct().Count(), "Os passos precisam de variacoes diferentes.");
        var ataque = Componente(prefab, "InimigoAtaque");
        var corte = (AudioClip)Campo(ataque, "somDoGolpe");
        var impacto = (AudioClip)Campo(ataque, "somDeAtaque");
        foreach (var clipe in sons.Concat(new[] { corte, impacto }))
        {
            Assert.IsNotNull(clipe);
            StringAssert.Contains("/Audio/PigMan/", AssetDatabase.GetAssetPath(clipe));
            Assert.AreEqual(1, clipe.channels, "Foley 3D deve usar um clipe mono.");
            Assert.AreEqual(44100, clipe.frequency);
            Assert.Greater(clipe.samples, 1000);
        }
        Assert.AreNotEqual(corte, impacto, "O corte e o acerto tem sons separados.");
        Assert.IsTrue((bool)Campo(ataque, "destacarSomDeAcerto"), "O prefab precisa habilitar o feedback independente do acerto.");
        Assert.Greater(corte.length, (float)Campo(ataque, "atrasoDoDano"));
        Assert.IsTrue(sons.All(s => s.length < (float)Campo(passos, "intervaloEntrePassos")));
        var fonte = (AudioSource)Campo(ataque, "fonteDeAudio");
        Assert.AreEqual(1f, fonte.spatialBlend);
        Assert.AreEqual(AudioRolloffMode.Linear, fonte.rolloffMode);
        Assert.IsFalse(fonte.playOnAwake);
        Assert.LessOrEqual(fonte.maxDistance, 30f);
        Assert.Greater(fonte.maxDistance, fonte.minDistance);
        var comum = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DerbySegundoTempo/Prefabs/Inimigos/InimigoPerseguidor.prefab");
        Assert.AreNotEqual(Campo(Componente(comum, "InimigoAtaque"), "somDeAtaque"), impacto);
        Assert.IsNull(Campo(Componente(comum, "InimigoAtaque"), "somDoGolpe"));
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
    public IEnumerator EstadioTemUmMiniBossApenasNaHordaDois() => ConferirHordas("Estadio_TPS");

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
        var clipeDeAcerto = (AudioClip)Campo(ataque, "somDeAtaque");
        var somDoAcerto = Object.FindObjectsByType<AudioSource>().Single(s => s.clip == clipeDeAcerto && s.isPlaying);
        Assert.Greater(somDoAcerto.timeSamples, 0, "O clipe de acerto deve estar avancando no dispositivo de audio.");
        Assert.AreEqual(0f, somDoAcerto.spatialBlend, "O feedback de dano nao pode sumir com a distancia da camera.");
        Assert.AreEqual((float)Campo(ataque, "volumeDoAcerto"), somDoAcerto.volume);
        Assert.IsFalse(((AudioSource)Campo(ataque, "fonteDeAudio")).isPlaying, "O corte deve ceder lugar ao acerto.");
        float[] amostras = new float[512];
        bool produziuAudio = false;
        // Amostra varios buffers DSP; a primeira consulta pode preceder o processamento do audio.
        for (int tentativa = 0; tentativa < 8 && somDoAcerto != null && somDoAcerto.isPlaying; tentativa++)
        {
            somDoAcerto.GetOutputData(amostras, 0);
            if (amostras.Any(a => Mathf.Abs(a) > .0001f))
            {
                produziuAudio = true;
                break;
            }
            yield return new WaitForSecondsRealtime(.02f);
        }
        Assert.IsTrue(produziuAudio, "O som de acerto deve produzir audio audivel. "
            + "Listener: volume=" + AudioListener.volume + "; pausado=" + AudioListener.pause
            + "; editor mudo=" + EditorUtility.audioMasterMute + "; taxa=" + AudioSettings.outputSampleRate);
        yield return new WaitForSeconds((float)Campo(ataque, "duracaoDaAnimacao") + (float)Campo(ataque, "tempoEntreAtaques") - .65f);
        for (int i = 0; i < 100 && !(bool)ataque.GetType().GetProperty("Atacando").GetValue(ataque); i++) yield return null;
        Assert.IsTrue((bool)ataque.GetType().GetProperty("Atacando").GetValue(ataque));
        int antesDeEsquivar = (int)Campo(vida, "vidaAtual");
        jogador.transform.position = new Vector3(0, 0, 8);
        yield return new WaitForSeconds(.8f);
        Assert.AreEqual(antesDeEsquivar, Campo(vida, "vidaAtual"), "Sair do alcance antes do impacto deve evitar o dano.");
        Assert.IsFalse(Object.FindObjectsByType<AudioSource>().Any(s => s.clip == clipeDeAcerto && s.isPlaying),
            "Uma esquiva nao deve produzir o som de ataque sucedido.");
        Object.Destroy(grupo);
        Object.Destroy(jogador);
    }

    [UnityTest]
    public IEnumerator AcertoFatalTocaAteOFimMesmoSePigManForDestruido()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Menus/TelaInicial.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        Time.timeScale = 1;
        var jogador = new GameObject("JogadorTesteAcertoFatal");
        jogador.tag = "Jogador";
        jogador.transform.position = new Vector3(0, 0, 2.4f);
        var vida = jogador.AddComponent(Tipo("JogadorVida"));
        Definir(vida, "vidaMaxima", 25);
        var grupo = new GameObject("PigManTesteAcertoFatal");
        grupo.SetActive(false);
        var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), grupo.transform);
        boss.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        boss.GetComponent<NavMeshAgent>().enabled = false;
        ((Behaviour)Componente(boss, "InimigoPerseguidor")).enabled = false;
        grupo.SetActive(true);
        yield return new WaitForSeconds(.75f);
        Assert.AreEqual(0, Campo(vida, "vidaAtual"));
        var ataque = Componente(boss, "InimigoAtaque");
        var clipe = (AudioClip)Campo(ataque, "somDeAtaque");
        var som = Object.FindObjectsByType<AudioSource>().Single(s => s.clip == clipe && s.isPlaying);
        Assert.IsFalse(som.transform.IsChildOf(boss.transform));
        int amostraAntes = som.timeSamples;
        Object.Destroy(grupo);
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.1f);
        Assert.IsTrue(som != null && som.isPlaying, "A morte do inimigo e o fim da partida nao podem cortar o acerto.");
        Assert.Greater(som.timeSamples, amostraAntes);
        Time.timeScale = 1;
        yield return new WaitForSeconds(clipe.length + .2f);
        Assert.IsTrue(som == null, "A fonte temporaria precisa ser removida apos tocar.");
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

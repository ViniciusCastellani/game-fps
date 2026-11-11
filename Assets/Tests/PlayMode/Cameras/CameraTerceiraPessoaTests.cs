#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class CameraTerceiraPessoaTests
{
    const string Cenas = "Assets/DerbySegundoTempo/Cenas/";
    Scene teste;
    Transform jogador;
    Transform mira;
    Component alvo;
    CinemachineCamera camera;
    CinemachineThirdPersonFollow corpo;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Fases/Estadio_TPS.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var raizes = SceneManager.GetActiveScene().GetRootGameObjects();
        var original = raizes.SelectMany(r => r.GetComponentsInChildren<CinemachineThirdPersonFollow>(true)).Single();
        var tipo = Type.GetType("AlvoCameraTerceiraPessoa, Assembly-CSharp", true);
        var originalAlvo = original.GetComponent<CinemachineCamera>().Follow.GetComponent(tipo);
        Assert.IsNotNull(originalAlvo, "Instale a correcao das cameras antes dos testes.");
        var originalJogador = (Transform)tipo.GetField("jogador").GetValue(originalAlvo);
        var originalMira = (Transform)tipo.GetField("mira").GetValue(originalAlvo);
        teste = SceneManager.CreateScene("TesteIsoladoCameraTP");
        SceneManager.SetActiveScene(teste);
        var go = new GameObject("JogadorTesteCamera");
        jogador = go.transform;
        jogador.position = new Vector3(1000, 50, 1000);
        jogador.localScale = originalJogador.lossyScale;
        go.tag = "Jogador";
        var collider = go.AddComponent<CapsuleCollider>();
        collider.center = Vector3.up;
        collider.height = 2;
        collider.radius = .5f;
        mira = new GameObject("MiraTesteCamera").transform;
        mira.SetParent(jogador, false);
        mira.localPosition = originalJogador.InverseTransformPoint(originalMira.position);
        mira.localRotation = Quaternion.identity;
        var alvoGo = Object.Instantiate(originalAlvo.gameObject);
        alvoGo.transform.SetParent(jogador, false);
        alvo = alvoGo.GetComponent(tipo);
        tipo.GetField("jogador").SetValue(alvo, jogador);
        tipo.GetField("mira").SetValue(alvo, mira);
        camera = Object.Instantiate(original.gameObject).GetComponent<CinemachineCamera>();
        camera.enabled = false; // O teste executa o pipeline real sem o Brain da fase.
        camera.Follow = alvo.transform;
        corpo = camera.GetComponent<CinemachineThirdPersonFollow>();
        foreach (var raiz in raizes) raiz.SetActive(false);
        Time.timeScale = 1;
        Atualizar();
        yield return null;
    }

    Vector3 Atualizar(float deltaTime = -1)
    {
        alvo.GetType().GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(alvo, null);
        Physics.SyncTransforms();
        camera.InternalUpdateCameraState(Vector3.up, deltaTime);
        return camera.State.GetFinalPosition();
    }

    GameObject Barreira(int camada = 0)
    {
        var parede = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parede.name = "BarreiraTesteCamera";
        parede.layer = camada;
        parede.transform.position = jogador.position + new Vector3(0, 1.2f, -1.2f);
        parede.transform.localScale = new Vector3(8, 8, .3f);
        return parede;
    }

    void ConferirFolga(Collider obstaculo, Vector3 posicao)
    {
        Assert.GreaterOrEqual(Vector3.Distance(posicao, obstaculo.ClosestPoint(posicao)),
            corpo.AvoidObstacles.CameraRadius - .025f, obstaculo.name + ": a camera nao pode penetrar o objeto.");
    }

    [UnityTest]
    public IEnumerator EnquadramentoLivrePreservadoAoGirarEInclinar()
    {
        foreach (float yaw in new[] { 0f, 95f, 180f, 275f })
            foreach (float pitch in new[] { -70f, 0f, 70f })
            {
                var heading = Quaternion.Euler(0, yaw, 0);
                mira.SetPositionAndRotation(jogador.position + heading * Vector3.Scale(new Vector3(.84f, 1.5293322f, 3.98f), jogador.lossyScale), Quaternion.Euler(pitch, yaw, 0));
                // Posicao original da camera antes da correcao do ponto de partida da colisao.
                Vector3 esperada = mira.position + heading * new Vector3(.71f, -.4f, -3.08f)
                    + mira.rotation * Vector3.up * .4f - mira.forward * 2;
                var atual = Atualizar();
                Assert.Less(Vector3.Distance(esperada, atual), .006f, $"Yaw={yaw}; pitch={pitch}");
                Assert.Less(Quaternion.Angle(mira.rotation, camera.State.GetFinalOrientation()), .01f);
            }
        yield return null;
    }

    [UnityTest]
    public IEnumerator BarreirasDefaultECenarioNaoSaoAtravessadas()
    {
        foreach (int camada in new[] { 0, LayerMask.NameToLayer("Cenario") })
        {
            var parede = Barreira(camada);
            foreach (float pitch in new[] { -35f, 0f, 35f })
            {
                mira.rotation = Quaternion.Euler(pitch, 0, 0);
                var posicao = Atualizar();
                Assert.Greater(posicao.z, parede.GetComponent<Collider>().bounds.max.z);
                ConferirFolga(parede.GetComponent<Collider>(), posicao);
                Assert.AreEqual(parede.GetComponent<Collider>(), corpo.CurrentObstacle);
            }
            Object.Destroy(parede);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator EstacoesDeCuraUpgradeEMunicaoAfastamACamera()
    {
        const string pasta = "Assets/DerbySegundoTempo/Prefabs/Estacoes/";
        foreach (string nome in new[] { "EstacaoCura", "EstacaoUpgrades", "EstacaoMunicao", "Rua/FarmaciaBairro", "Rua/OficinaDerby", "Rua/DepositoRecarga" })
        {
            Vector3 livre = Atualizar();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pasta + nome + ".prefab");
            Assert.IsNotNull(prefab, nome);
            var estacao = Object.Instantiate(prefab);
            foreach (var script in estacao.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            var solidos = estacao.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
            Assert.IsNotEmpty(solidos, nome);
            var principal = solidos.OrderByDescending(c => c.bounds.size.sqrMagnitude).First();
            estacao.transform.position += livre - principal.bounds.center;
            var corrigida = Atualizar();
            Assert.Less(Vector3.Distance(alvo.transform.position, corrigida), Vector3.Distance(alvo.transform.position, livre) - .05f, nome);
            foreach (var solido in solidos) ConferirFolga(solido, corrigida);
            Object.Destroy(estacao);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator RetornoDepoisDaBarreiraESuaveESemUltrapassarDistanciaOriginal()
    {
        Vector3 livre = Atualizar();
        var parede = Barreira();
        Vector3 recolhida = Atualizar();
        Assert.Greater(Vector3.Distance(livre, recolhida), .3f);
        parede.SetActive(false);
        Vector3 inicio = Atualizar(1f / 60f);
        Assert.Less(Vector3.Distance(inicio, recolhida), Vector3.Distance(livre, recolhida));
        Assert.Greater(Vector3.Distance(inicio, livre), .1f);
        float anterior = Vector3.Distance(inicio, livre);
        for (int i = 0; i < 90; i++)
        {
            Vector3 posicao = Atualizar(1f / 60f);
            float distancia = Vector3.Distance(posicao, livre);
            Assert.LessOrEqual(distancia, anterior + .002f);
            anterior = distancia;
        }
        Assert.Less(anterior, .01f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator JogadorGatilhosEObjetosAFrenteNaoPuxamACamera()
    {
        Vector3 livre = Atualizar();
        var gatilho = Barreira();
        gatilho.GetComponent<Collider>().isTrigger = true;
        gatilho.transform.localScale = new Vector3(8, 8, 8);
        Assert.Less(Vector3.Distance(livre, Atualizar()), .005f, "Gatilhos de interacao e o proprio jogador devem ser ignorados.");
        var frente = Barreira(LayerMask.NameToLayer("Cenario"));
        frente.transform.position = jogador.position + new Vector3(0, 1.2f, 1.5f);
        Assert.Less(Vector3.Distance(livre, Atualizar()), .005f, "Um obstaculo a frente nao pode deslocar a camera atras do jogador.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ConfiguracaoAplicadaAoEstadioERua()
    {
        foreach (string nome in new[] { "Estadio_TPS", "Rua_TPS" })
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Fases/" + nome + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var raiz = SceneManager.GetActiveScene().GetRootGameObjects();
            var tp = raiz.SelectMany(r => r.GetComponentsInChildren<CinemachineThirdPersonFollow>(true)).Single();
            Assert.IsTrue(tp.AvoidObstacles.Enabled);
            Assert.AreEqual("Jogador", tp.AvoidObstacles.IgnoreTag);
            Assert.IsTrue((tp.AvoidObstacles.CollisionFilter & 1) != 0, "A camada Default precisa ser detectada.");
            Assert.IsTrue((tp.AvoidObstacles.CollisionFilter & (1 << LayerMask.NameToLayer("Cenario"))) != 0);
            var alvoTp = tp.GetComponent<CinemachineCamera>().Follow;
            Assert.IsNotNull(alvoTp.GetComponent(Type.GetType("AlvoCameraTerceiraPessoa, Assembly-CSharp", true)));
            Assert.Less(new Vector2(alvoTp.localPosition.x, alvoTp.localPosition.z).magnitude, .01f);
            var fp = raiz.SelectMany(r => r.GetComponentsInChildren<CinemachineFollow>(true)).Single();
            Assert.IsFalse(alvoTp == fp.GetComponent<CinemachineCamera>().Follow,
                "Primeira e terceira pessoa devem usar alvos separados.");
            Assert.Less(Vector3.Distance(fp.FollowOffset, new Vector3(-.82f, -.13f, -2.83f)), .001f);
        }
    }

    [UnityTearDown]
    public IEnumerator Limpar()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (teste.IsValid() && teste.isLoaded) yield return SceneManager.UnloadSceneAsync(teste);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Menus/TelaInicial.unity", new LoadSceneParameters(LoadSceneMode.Single));
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ModosDeJogoTests
{
    const string Cenas = "Assets/DerbySegundoTempo/Cenas/";
    const string Estadio = "Estadio_TPS";
    const string Rua = "Rua_TPS";
    const string CartaoEstadio = "Assets/DerbySegundoTempo/Arte/Imagens/Interface/Cenarios/cartao_estadio.png";
    const string CartaoRua = "Assets/DerbySegundoTempo/Arte/Imagens/Interface/Cenarios/cartao_rua.png";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags FlagsEstaticos = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    static Type Tipo(string nome) => Type.GetType(nome + ", Assembly-CSharp", true);
    static object Campo(object alvo, string nome) => alvo.GetType().GetField(nome, Flags).GetValue(alvo);
    static void Definir(object alvo, string nome, object valor) => alvo.GetType().GetField(nome, Flags).SetValue(alvo, valor);
    static object Chamar(object alvo, string nome, params object[] argumentos) => alvo.GetType().GetMethod(nome, Flags).Invoke(alvo, argumentos);
    static object Propriedade(object alvo, string nome) => alvo.GetType().GetProperty(nome, Flags).GetValue(alvo);
    static object PropriedadeEstatica(string tipo, string nome) => Tipo(tipo).GetProperty(nome, FlagsEstaticos).GetValue(null);
    static object ChamarEstatico(string tipo, string nome, params object[] argumentos) => Tipo(tipo).GetMethod(nome, FlagsEstaticos).Invoke(null, argumentos);
    static string Texto(Component tmp) => (string)tmp.GetType().GetProperty("text").GetValue(tmp);

    static Component[] EncontrarTodos(string nome) => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(x => x.GetComponentsInChildren(Tipo(nome), true)).ToArray();
    static Component Encontrar(string nome) => EncontrarTodos(nome).Single();
    static Component[] Vidas() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(x => x.GetComponentsInChildren(Tipo("InimigoVida"), false)).ToArray();
    static int MiniBosses() => Vidas().Count(v => v.GetComponent(Tipo("PigManAnimador")) != null);

    static Transform Objeto(string caminho)
    {
        string[] partes = caminho.Split('/');
        var candidatos = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Where(t => t.name == partes[0]).ToArray();
        Assert.AreEqual(1, candidatos.Length, "Objeto unico: " + partes[0]);
        Transform atual = candidatos[0];
        for (int i = 1; i < partes.Length; i++)
        {
            atual = atual.Find(partes[i]);
            Assert.IsNotNull(atual, caminho);
        }
        return atual;
    }

    static Button Botao(string caminho) => Objeto(caminho).GetComponent<Button>();
    static string TextoDe(string caminho) => Texto(Objeto(caminho).GetComponents<Component>()
        .First(c => c != null && c.GetType().Name.StartsWith("TextMeshPro")));
    static string Estado(Component jogo) => Propriedade(jogo, "Estado").ToString();
    static string Modo() => PropriedadeEstatica("SessaoDeJogo", "Modo").ToString();
    static GameObject Selecionado() => EventSystem.current.currentSelectedGameObject;
    static string TelaDoMenu() => Propriedade(Encontrar("MenuDeModos"), "TelaAtual").ToString();

    static IEnumerator CarregarCena(string caminho)
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + caminho + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
    }

    static IEnumerator IrParaTelaInicial() => CarregarCena("Menus/TelaInicial");

    static IEnumerator EsperarCena(string nome)
    {
        for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != nome; i++) yield return null;
        Assert.AreEqual(nome, SceneManager.GetActiveScene().name);
        yield return null;
        yield return null;
    }

    static IEnumerator EsperarNovaInstancia(string tipo, Component antigo)
    {
        for (int i = 0; i < 120; i++)
        {
            var atuais = EncontrarTodos(tipo);
            if (atuais.Length == 1 && atuais[0] != antigo) break;
            yield return null;
        }
        var atual = EncontrarTodos(tipo);
        Assert.AreEqual(1, atual.Length);
        Assert.IsTrue(atual[0] != antigo, "Nova instancia de " + tipo);
        yield return null;
        yield return null;
    }

    static void TornarJogadorInvencivel()
    {
        var vida = Encontrar("JogadorVida");
        Definir(vida, "vidaMaxima", 1000000);
        Definir(vida, "vidaAtual", 1000000);
    }

    static void MatarJogador()
    {
        var vida = Encontrar("JogadorVida");
        Chamar(vida, "ReceberDano", (int)Campo(vida, "vidaAtual") + 1);
    }

    static IEnumerator EntrarNoInfinito(string cena)
    {
        yield return IrParaTelaInicial();
        Botao("BotaoIniciar").onClick.Invoke();
        yield return EsperarCena("SelecaoDeModo");
        Botao("BotaoModoInfinito").onClick.Invoke();
        yield return EsperarCena("SelecaoDeCenario");
        Botao(cena == Rua ? "CartaoRua" : "CartaoEstadio").onClick.Invoke();
        Botao("BotaoIniciarSobrevivencia").onClick.Invoke();
        yield return EsperarCena(cena);
    }

    static IEnumerator EntrarNoNormal()
    {
        yield return IrParaTelaInicial();
        Botao("BotaoIniciar").onClick.Invoke();
        yield return EsperarCena("SelecaoDeModo");
        Botao("BotaoModoNormal").onClick.Invoke();
        yield return EsperarCena(Estadio);
    }

    static void AssertCor(Color esperada, Color atual)
    {
        Assert.AreEqual(esperada.r, atual.r, 0.002f);
        Assert.AreEqual(esperada.g, atual.g, 0.002f);
        Assert.AreEqual(esperada.b, atual.b, 0.002f);
    }

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        ChamarEstatico("SessaoDeJogo", "Reiniciar");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Limpar()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ChamarEstatico("SessaoDeJogo", "Reiniciar");
        Tipo("GerenciadorDeOrdas").GetMethod("LimparReinicio").Invoke(null, null);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Cenas + "Menus/TelaInicial.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
    }

    [Test]
    public void FormatacaoEIntervalo()
    {
        MethodInfo formatar = Tipo("GerenciadorModoInfinito").GetMethod("FormatarTempo");
        Assert.AreEqual("00:00", formatar.Invoke(null, new object[] { 0f }));
        Assert.AreEqual("00:59", formatar.Invoke(null, new object[] { 59.9f }));
        Assert.AreEqual("01:00", formatar.Invoke(null, new object[] { 60f }));
        Assert.AreEqual("12:34", formatar.Invoke(null, new object[] { 754.2f }));
        Assert.AreEqual("00:00", formatar.Invoke(null, new object[] { -3f }));
        Assert.AreEqual("60:00", formatar.Invoke(null, new object[] { 3600f }));

        var objeto = new GameObject("GeradorIntervaloTeste");
        try
        {
            var gerador = objeto.AddComponent(Tipo("GeradorDeInimigosInfinito"));
            Assert.AreEqual(3f, (float)Chamar(gerador, "IntervaloAtual", 0f), 0.001f);
            Assert.AreEqual(2.4f, (float)Chamar(gerador, "IntervaloAtual", 60f), 0.001f);
            Assert.AreEqual(1.2f, (float)Chamar(gerador, "IntervaloAtual", 600f), 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(objeto);
        }
    }

    [Test]
    public void SessaoDeJogoSegueAsRegras()
    {
        ChamarEstatico("SessaoDeJogo", "Reiniciar");
        Assert.AreEqual("Normal", Modo());
        Assert.IsNull(PropriedadeEstatica("SessaoDeJogo", "CenaDoModoInfinito"));
        Assert.AreEqual(Estadio, PropriedadeEstatica("SessaoDeJogo", "UltimoCenarioInfinito"));
        Assert.IsFalse((bool)ChamarEstatico("SessaoDeJogo", "ConsumirPedidoDeEscolhaDeCenario"));

        ChamarEstatico("SessaoDeJogo", "IniciarInfinito", Rua);
        Assert.AreEqual("Infinito", Modo());
        Assert.IsTrue((bool)ChamarEstatico("SessaoDeJogo", "EhInfinitoNaCena", Rua));
        Assert.IsFalse((bool)ChamarEstatico("SessaoDeJogo", "EhInfinitoNaCena", Estadio));
        Assert.AreEqual(Rua, PropriedadeEstatica("SessaoDeJogo", "UltimoCenarioInfinito"));

        ChamarEstatico("SessaoDeJogo", "PedirEscolhaDeCenario");
        Assert.IsTrue((bool)ChamarEstatico("SessaoDeJogo", "ConsumirPedidoDeEscolhaDeCenario"));
        Assert.IsFalse((bool)ChamarEstatico("SessaoDeJogo", "ConsumirPedidoDeEscolhaDeCenario"));

        ChamarEstatico("SessaoDeJogo", "IniciarInfinito", Rua);
        ChamarEstatico("SessaoDeJogo", "PedirEscolhaDeCenario");
        ChamarEstatico("SessaoDeJogo", "DefinirNormal");
        Assert.AreEqual("Normal", Modo());
        Assert.IsNull(PropriedadeEstatica("SessaoDeJogo", "CenaDoModoInfinito"));
        Assert.IsFalse((bool)ChamarEstatico("SessaoDeJogo", "ConsumirPedidoDeEscolhaDeCenario"));
        Assert.AreEqual(Rua, PropriedadeEstatica("SessaoDeJogo", "UltimoCenarioInfinito"));
    }

    [UnityTest]
    public IEnumerator SessaoEValidacaoDeCenas()
    {
        yield return IrParaTelaInicial();
        var cenas = Encontrar("GerenciadorDeCenas");

        LogAssert.Expect(LogType.Error, new Regex("Cenario infinito invalido"));
        Chamar(cenas, "IniciarModoInfinito", "TelaInicial");
        for (int i = 0; i < 5; i++) yield return null;
        Assert.AreEqual("TelaInicial", SceneManager.GetActiveScene().name);
        Assert.AreEqual("Normal", Modo());

        ChamarEstatico("SessaoDeJogo", "IniciarInfinito", Rua);
        Chamar(cenas, "ProximaFase");
        yield return EsperarCena(Estadio);
        Assert.AreEqual("Normal", Modo());
    }

    [UnityTest]
    public IEnumerator PontosTotaisEMorteUnica()
    {
        yield return CarregarCena("Fases/" + Estadio);
        var pontos = (Component)Tipo("SistemaDePontos").GetField("instancia").GetValue(null);
        Assert.AreEqual(0, (int)Propriedade(pontos, "PontosTotaisConquistados"));

        int inicio = (int)Campo(pontos, "pontosAtuais");
        Chamar(pontos, "AdicionarPontos", 100);
        Assert.IsTrue((bool)Chamar(pontos, "GastarPontos", 60));
        Assert.AreEqual(inicio + 40, (int)Campo(pontos, "pontosAtuais"));
        Assert.AreEqual(100, (int)Propriedade(pontos, "PontosTotaisConquistados"));
        Assert.IsFalse((bool)Chamar(pontos, "GastarPontos", 100000));
        Assert.AreEqual(inicio + 40, (int)Campo(pontos, "pontosAtuais"));
        Assert.AreEqual(100, (int)Propriedade(pontos, "PontosTotaisConquistados"));

        Chamar(pontos, "AdicionarPontos", 0);
        Chamar(pontos, "AdicionarPontos", -5);
        Assert.AreEqual(100, (int)Propriedade(pontos, "PontosTotaisConquistados"));
        Chamar(pontos, "AdicionarPontos", 5);
        Chamar(pontos, "GastarPontos", 5);

        var inimigo = Vidas().First();
        int mortes = 0;
        Definir(inimigo, "aoMorrer", (Action)(() => mortes++));
        int saldo = (int)Campo(pontos, "pontosAtuais");
        int recompensa = (int)Campo(inimigo, "pontosAoMorrer");
        Chamar(inimigo, "ReceberDano", 10000);
        Chamar(inimigo, "ReceberDano", 10000);
        Assert.AreEqual(1, mortes);
        Assert.AreEqual(saldo + recompensa, (int)Campo(pontos, "pontosAtuais"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator MenuNavegaEntreTelas()
    {
        BaseInputModule entrada = null;
        void SuspenderEntradaDaCena(Scene cena, LoadSceneMode modo)
        {
            entrada = cena.GetRootGameObjects()
                .SelectMany(objeto => objeto.GetComponentsInChildren<BaseInputModule>()).Single();
            entrada.enabled = false;
        }

        SceneManager.sceneLoaded += SuspenderEntradaDaCena;
        try
        {
            yield return ConferirNavegacaoDoMenu();
        }
        finally
        {
            SceneManager.sceneLoaded -= SuspenderEntradaDaCena;
            if (entrada != null) entrada.enabled = true;
        }
    }

    static IEnumerator ConferirNavegacaoDoMenu()
    {
        yield return IrParaTelaInicial();
        Assert.AreEqual("Principal", TelaDoMenu());
        Assert.IsFalse(EncontrarTodos("CartaoDeCenario").Any());
        var iniciar = Botao("BotaoIniciar");
        var sair = Botao("BotaoSair");
        Assert.AreEqual(iniciar.gameObject, Selecionado());
        Assert.AreEqual(1, iniciar.onClick.GetPersistentEventCount());
        Assert.AreEqual("AbrirEscolhaDeModo", iniciar.onClick.GetPersistentMethodName(0));
        Assert.AreEqual("Sair", sair.onClick.GetPersistentMethodName(0));
        Assert.AreEqual(sair, iniciar.navigation.selectOnDown);
        Assert.AreEqual(iniciar, sair.navigation.selectOnUp);

        var eventos = EventSystem.current;
        var ponteiro = new PointerEventData(eventos) { button = PointerEventData.InputButton.Left };
        var resultados = new List<RaycastResult>();
        GameObject AlvoDoClique(Vector2 posicao)
        {
            ponteiro.position = posicao;
            eventos.RaycastAll(ponteiro, resultados);
            return resultados.FirstOrDefault().gameObject;
        }

        Assert.IsNotNull(iniciar.image.sprite);
        Assert.IsNotNull(sair.image.sprite);
        Vector2 centroIniciar = RectTransformUtility.WorldToScreenPoint(null, iniciar.image.rectTransform.position);
        Vector2 centroSair = RectTransformUtility.WorldToScreenPoint(null, sair.image.rectTransform.position);
        Assert.AreEqual(sair.gameObject, AlvoDoClique(centroSair));
        Assert.IsNull(AlvoDoClique(Vector2.Lerp(centroIniciar, centroSair, 0.5f)));

        GameObject alvoIniciar = AlvoDoClique(centroIniciar);
        Assert.AreEqual(iniciar.gameObject, alvoIniciar);
        ExecuteEvents.Execute(alvoIniciar, ponteiro, ExecuteEvents.pointerClickHandler);
        yield return EsperarCena("SelecaoDeModo");
        Assert.AreEqual("Modos", TelaDoMenu());
        Assert.AreEqual(Botao("BotaoModoNormal").gameObject, Selecionado());
        Assert.AreEqual("CLÁSSICO", TextoDe("BotaoModoNormal/Titulo"));
        Assert.AreEqual("INFINITO", TextoDe("BotaoModoInfinito/Titulo"));
        Assert.AreEqual(Botao("BotaoModoInfinito"), Botao("BotaoModoNormal").navigation.selectOnDown);
        Assert.AreEqual(Botao("BotaoModoNormal"), Botao("BotaoModoInfinito").navigation.selectOnUp);
        Assert.AreEqual(Botao("BotaoVoltarModos"), Botao("BotaoModoInfinito").navigation.selectOnDown);
        Botao("BotaoVoltarModos").onClick.Invoke();
        yield return EsperarCena("TelaInicial");
        Assert.AreEqual("Principal", TelaDoMenu());

        Botao("BotaoIniciar").onClick.Invoke();
        yield return EsperarCena("SelecaoDeModo");
        Botao("BotaoModoInfinito").onClick.Invoke();
        yield return EsperarCena("SelecaoDeCenario");
        Assert.AreEqual("Cenarios", TelaDoMenu());
        Assert.AreEqual("ESTÁDIO", TextoDe("CartaoEstadio/Nome"));
        Assert.AreEqual("RUA", TextoDe("CartaoRua/Nome"));
        var menu = Encontrar("MenuDeModos");
        var cartoes = EncontrarTodos("CartaoDeCenario");
        Assert.AreEqual(2, cartoes.Length);
        var estadio = cartoes.Single(c => (string)Propriedade(c, "NomeDaCena") == Estadio);
        var rua = cartoes.Single(c => (string)Propriedade(c, "NomeDaCena") == Rua);
        foreach (var cartao in cartoes) Assert.IsNotNull(((Image)Campo(cartao, "imagem")).sprite);
        Assert.IsTrue((bool)Propriedade(estadio, "Escolhido"));
        Assert.AreEqual(estadio.gameObject, Selecionado());
        Assert.AreEqual(Botao("CartaoRua"), Botao("CartaoEstadio").navigation.selectOnRight);
        Assert.AreEqual(Botao("CartaoEstadio"), Botao("CartaoRua").navigation.selectOnLeft);
        Assert.AreEqual(Botao("BotaoIniciarSobrevivencia"), Botao("CartaoRua").navigation.selectOnDown);
        Botao("CartaoRua").onClick.Invoke();
        Assert.AreEqual(Rua, (string)Propriedade(Propriedade(menu, "CartaoEscolhido"), "NomeDaCena"));
        AssertCor(new Color(1f, 0.776f, 0.161f), ((Image)Campo(rua, "moldura")).color);
        Assert.IsTrue(((GameObject)Campo(rua, "seloSelecionado")).activeSelf);
        Assert.IsFalse(((GameObject)Campo(estadio, "seloSelecionado")).activeSelf);
        EventSystem.current.SetSelectedGameObject(null);
        yield return null;
        yield return null;
        Assert.AreEqual(rua.gameObject, Selecionado());
        Chamar(menu, "Voltar");
        yield return EsperarCena("SelecaoDeModo");
        Assert.AreEqual("Modos", TelaDoMenu());
    }

    [UnityTest]
    public IEnumerator MenuIniciaNormalEInfinito()
    {
        yield return EntrarNoInfinito(Rua);
        Assert.IsTrue((bool)ChamarEstatico("SessaoDeJogo", "EhInfinitoNaCena", Rua));

        yield return EntrarNoInfinito(Estadio);
        Assert.IsTrue((bool)ChamarEstatico("SessaoDeJogo", "EhInfinitoNaCena", Estadio));

        yield return EntrarNoNormal();
        Assert.AreEqual(Estadio, SceneManager.GetActiveScene().name);
        Assert.AreEqual("Normal", Modo());
    }

    [UnityTest]
    public IEnumerator ArteEMenusMontados()
    {
        foreach (string caminho in new[] { CartaoEstadio, CartaoRua })
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            Assert.IsNotNull(sprite, caminho);
            Assert.AreEqual(1440f, sprite.rect.width);
            Assert.AreEqual(900f, sprite.rect.height);
        }

        foreach (string cena in new[] { Estadio, Rua })
        {
            yield return CarregarCena("Fases/" + cena);
            Assert.AreEqual(1, EncontrarTodos("GerenciadorModoInfinito").Length, cena);
            Assert.AreEqual(1, EncontrarTodos("GeradorDeInimigosInfinito").Length, cena);
            var gerenciador = Encontrar("GerenciadorModoInfinito");
            var gerador = Encontrar("GeradorDeInimigosInfinito");
            Assert.AreEqual(gerenciador.gameObject, gerador.gameObject);
            Assert.AreEqual("ModoInfinito", gerenciador.name);
            Assert.AreEqual("00_Sistemas", gerenciador.transform.parent.name);

            foreach (string campo in new[] { "gerenciadorDeJogo", "gerenciadorDeOrdas", "cronometro", "gerenciadorDeCenas", "gerador",
                "hudInfinito", "telaDeResultado", "textoResultadoCenario", "textoResultadoPontos", "textoResultadoAbates",
                "textoResultadoTempo", "textoResultadoHordas", "textoResultadoSaldo", "textoHorda", "textoTempoSobrevivencia", "textoAbates", "botaoTentarNovamente" })
                Assert.IsTrue((Object)Campo(gerenciador, campo) != null, cena + "." + campo);
            Assert.AreEqual(2, ((Array)Campo(gerenciador, "objetosSomenteModoNormal")).Length);

            var comuns = (GameObject[])Campo(gerador, "prefabsComuns");
            Assert.GreaterOrEqual(comuns.Length, 1);
            string pigMan = "Assets/DerbySegundoTempo/Prefabs/Inimigos/PigManMiniBoss.prefab";
            foreach (var prefab in comuns)
            {
                Assert.IsNotNull(prefab);
                Assert.AreNotEqual(pigMan, AssetDatabase.GetAssetPath(prefab));
            }
            Assert.AreEqual(pigMan, AssetDatabase.GetAssetPath((Object)Campo(gerador, "prefabMiniBoss")));

            var jogo = Encontrar("GerenciadorDeJogo");
            Assert.AreEqual("TelaDeGameOver", ((GameObject)Campo(jogo, "telaDeGameOver")).name);
            Assert.AreEqual(cena == Estadio, (bool)Campo(jogo, "mostrarTutorialAoIniciar"), cena);
            Assert.IsFalse(Objeto("HUDInfinito").gameObject.activeSelf);
            Assert.IsFalse(Objeto("TelaDeResultadoInfinito").gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator InfinitoEstadioCicloCompleto() => CicloInfinito(Estadio, "Estádio");

    [UnityTest]
    public IEnumerator InfinitoRuaCicloCompleto() => CicloInfinito(Rua, "Rua");

    IEnumerator CicloInfinito(string cena, string nomeDoCenario)
    {
        yield return EntrarNoInfinito(cena);

        var jogo = Encontrar("GerenciadorDeJogo");
        var manager = Encontrar("GerenciadorModoInfinito");
        var gerador = Encontrar("GeradorDeInimigosInfinito");
        Assert.AreEqual("Tutorial", Estado(jogo));
        Assert.IsTrue(((GameObject)Campo(jogo, "telaDeTutorial")).activeInHierarchy);
        Assert.IsFalse(((Behaviour)Encontrar("GerenciadorDeOrdas")).enabled);
        Assert.IsFalse(((Behaviour)Encontrar("Cronometro")).enabled);
        Assert.AreEqual(0, Vidas().Length);
        Assert.IsFalse(Objeto("TextOrdas").gameObject.activeSelf);
        Assert.IsFalse(Objeto("TextTempo").gameObject.activeSelf);
        Assert.IsTrue(Objeto("HUDInfinito").gameObject.activeSelf);
        Assert.AreEqual("TEMPO: 00:00", TextoDe("HUDInfinito/TextoTempoSobrevivencia"));
        Assert.AreEqual("ABATES: 0", TextoDe("HUDInfinito/TextoAbates"));
        Assert.AreEqual((string)Tipo("GerenciadorModoInfinito").GetField("TextoObjetivoInfinito").GetRawConstantValue(),
            TextoDe("TelaDeTutorial/PainelComandos/Objetivo"));
        yield return new WaitForSecondsRealtime(1f);
        Assert.AreEqual(0, Vidas().Length);
        Assert.AreEqual(0f, (float)Propriedade(manager, "TempoDeSobrevivencia"));

        TornarJogadorInvencivel();
        Chamar(jogo, "FecharTutorial");
        yield return new WaitForSeconds(1.2f);
        Assert.IsTrue((bool)Propriedade(manager, "SobrevivenciaIniciada"));
        float tempo = (float)Propriedade(manager, "TempoDeSobrevivencia");
        Assert.GreaterOrEqual(tempo, 1.0f);
        Assert.LessOrEqual(tempo, 1.5f);
        StringAssert.IsMatch("^TEMPO: 00:0[1-2]$", TextoDe("HUDInfinito/TextoTempoSobrevivencia"));

        Chamar(jogo, "Pausar");
        float tempoPausado = (float)Propriedade(manager, "TempoDeSobrevivencia");
        int vivosPausado = Vidas().Length;
        yield return new WaitForSecondsRealtime(1f);
        Assert.AreEqual(tempoPausado, (float)Propriedade(manager, "TempoDeSobrevivencia"));
        Assert.AreEqual(vivosPausado, Vidas().Length);
        Chamar(jogo, "Continuar");
        yield return new WaitForSeconds(0.5f);
        Assert.Greater((float)Propriedade(manager, "TempoDeSobrevivencia"), tempoPausado);

        Chamar(jogo, "AbrirTutorial");
        Assert.AreEqual("Tutorial", Estado(jogo));
        float tempoNoTutorial = (float)Propriedade(manager, "TempoDeSobrevivencia");
        int vivosNoTutorial = Vidas().Length;
        yield return new WaitForSecondsRealtime(1f);
        Assert.AreEqual(tempoNoTutorial, (float)Propriedade(manager, "TempoDeSobrevivencia"));
        Assert.AreEqual(vivosNoTutorial, Vidas().Length);
        Chamar(jogo, "FecharTutorial");
        yield return new WaitForSeconds(0.5f);
        Assert.Greater((float)Propriedade(manager, "TempoDeSobrevivencia"), tempoNoTutorial);

        Definir(gerador, "intervaloInicial", 0.25f);
        Definir(gerador, "intervaloMinimo", 0.25f);
        var vivos = Vidas();
        int abatidos = vivos.Length;
        foreach (var vida in vivos) Chamar(vida, "ReceberDano", 10000);
        yield return new WaitForSeconds(1f);
        Assert.AreEqual("Jogando", Estado(jogo));
        Assert.IsFalse(((GameObject)Campo(jogo, "telaDeVitoria")).activeSelf);
        Assert.GreaterOrEqual(Vidas().Length, 1);
        Assert.AreEqual(abatidos, (int)Propriedade(manager, "InimigosAbatidos"));
        Assert.AreEqual("ABATES: " + abatidos, TextoDe("HUDInfinito/TextoAbates"));

        var pontos = (Component)Tipo("SistemaDePontos").GetField("instancia").GetValue(null);
        int conquistadosAntes = (int)Propriedade(manager, "PontosConquistados");
        Chamar(pontos, "AdicionarPontos", 100);
        Assert.IsTrue((bool)Chamar(pontos, "GastarPontos", 50));
        var alvo = Vidas().First();
        int recompensa = (int)Campo(alvo, "pontosAoMorrer");
        Chamar(alvo, "ReceberDano", 10000);
        Assert.AreEqual(conquistadosAntes + 100 + recompensa, (int)Propriedade(manager, "PontosConquistados"));
        yield return null;

        MatarJogador();
        Assert.AreEqual("GameOver", Estado(jogo));
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(Objeto("TelaDeResultadoInfinito").gameObject.activeSelf);
        Assert.IsFalse(Objeto("TelaDeGameOver").gameObject.activeSelf);
        Assert.IsFalse(Objeto("TelaDeVitoria").gameObject.activeSelf);
        Assert.AreEqual(nomeDoCenario, TextoDe("TelaDeResultadoInfinito/Caixa/LinhaCenario/Valor"));
        Assert.AreEqual(Propriedade(manager, "PontosConquistados").ToString(), TextoDe("TelaDeResultadoInfinito/Caixa/LinhaPontos/Valor"));
        Assert.AreEqual(Propriedade(manager, "InimigosAbatidos").ToString(), TextoDe("TelaDeResultadoInfinito/Caixa/LinhaAbates/Valor"));
        Assert.AreEqual(Propriedade(manager, "HordasSobrevividas").ToString(), TextoDe("TelaDeResultadoInfinito/Caixa/LinhaHordas/Valor"));
        Assert.AreEqual(Propriedade(manager, "PontosDisponiveis").ToString(), TextoDe("TelaDeResultadoInfinito/Caixa/LinhaSaldo/Valor"));
        string tempoFinal = TextoDe("TelaDeResultadoInfinito/Caixa/LinhaTempo/Valor");
        Assert.AreEqual(Tipo("GerenciadorModoInfinito").GetMethod("FormatarTempo").Invoke(null, new[] { Propriedade(manager, "TempoDeSobrevivencia") }), tempoFinal);
        StringAssert.IsMatch(@"^\d{2}:\d{2}$", tempoFinal);
        Assert.AreEqual(Botao("BotaoTentarNovamente").gameObject, Selecionado());

        float tempoDaMorte = (float)Propriedade(manager, "TempoDeSobrevivencia");
        int abatesDaMorte = (int)Propriedade(manager, "InimigosAbatidos");
        int vivosDaMorte = Vidas().Length;
        yield return new WaitForSecondsRealtime(1f);
        Assert.AreEqual(tempoDaMorte, (float)Propriedade(manager, "TempoDeSobrevivencia"));
        Assert.AreEqual(abatesDaMorte, (int)Propriedade(manager, "InimigosAbatidos"));
        Assert.AreEqual(vivosDaMorte, Vidas().Length);
        Assert.IsFalse((bool)Propriedade(gerador, "Ativo"));

        Botao("BotaoTentarNovamente").onClick.Invoke();
        yield return EsperarNovaInstancia("GerenciadorModoInfinito", manager);
        manager = Encontrar("GerenciadorModoInfinito");
        jogo = Encontrar("GerenciadorDeJogo");
        Assert.AreEqual(cena, SceneManager.GetActiveScene().name);
        Assert.AreEqual("Tutorial", Estado(jogo));
        Assert.AreEqual(0f, (float)Propriedade(manager, "TempoDeSobrevivencia"));
        Assert.AreEqual(0, (int)Propriedade(manager, "InimigosAbatidos"));
        Assert.AreEqual(0, (int)Propriedade(manager, "HordasSobrevividas"));
        pontos = (Component)Tipo("SistemaDePontos").GetField("instancia").GetValue(null);
        Assert.AreEqual(0, (int)Propriedade(pontos, "PontosTotaisConquistados"));
        Assert.AreEqual(0, (int)Campo(pontos, "pontosAtuais"));
        Assert.AreEqual("Infinito", Modo());
        Assert.AreEqual(0, Vidas().Length);

        Chamar(jogo, "FecharTutorial");
        yield return null;
        MatarJogador();
        Botao("BotaoEscolherCenario").onClick.Invoke();
        yield return EsperarCena("SelecaoDeCenario");
        Assert.AreEqual("Cenarios", TelaDoMenu());
        Assert.IsTrue(Objeto("PainelEscolhaDeCenario").gameObject.activeSelf);
        Assert.AreEqual(cena, (string)Propriedade(Propriedade(Encontrar("MenuDeModos"), "CartaoEscolhido"), "NomeDaCena"));
        Assert.AreEqual("Normal", Modo());
        Assert.AreEqual(1f, Time.timeScale);

        Botao("BotaoIniciarSobrevivencia").onClick.Invoke();
        yield return EsperarCena(cena);
        jogo = Encontrar("GerenciadorDeJogo");
        Chamar(jogo, "FecharTutorial");
        yield return null;
        MatarJogador();
        Botao("BotaoMenuInicial").onClick.Invoke();
        yield return EsperarCena("TelaInicial");
        Assert.AreEqual("Principal", TelaDoMenu());
        Assert.AreEqual(0, EncontrarTodos("CartaoDeCenario").Length);
        Assert.AreEqual("Normal", Modo());
        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(Botao("BotaoIniciar").gameObject, Selecionado());
    }

    [UnityTest]
    public IEnumerator GeradorEstadioRespeitaLimiteEMiniBoss() => ConferirGerador(Estadio);

    [UnityTest]
    public IEnumerator GeradorRuaRespeitaLimiteEMiniBoss() => ConferirGerador(Rua);

    IEnumerator ConferirGerador(string cena)
    {
        yield return EntrarNoInfinito(cena);
        var jogo = Encontrar("GerenciadorDeJogo");
        var gerador = Encontrar("GeradorDeInimigosInfinito");
        var ordas = Encontrar("GerenciadorDeOrdas");
        TornarJogadorInvencivel();
        Chamar(jogo, "FecharTutorial");
        yield return null;
        yield return null;

        var iniciais = Vidas();
        Assert.AreEqual((int)Campo(gerador, "inimigosNoInicio"), iniciais.Length);
        var jogador = Encontrar("JogadorVida").transform;
        float distanciaMinima = (float)Campo(gerador, "distanciaMinimaDoJogador");
        float raio = (float)Campo(gerador, "raioDeBuscaNaNavMesh");
        bool haPontoDistante = ((Transform[])Campo(ordas, "pontosDeSpawn"))
            .Any(p => Vector3.Distance(p.position, jogador.position) >= distanciaMinima);
        foreach (var inimigo in iniciais)
        {
            Assert.IsTrue(inimigo.GetComponent<NavMeshAgent>().isOnNavMesh);
            Assert.AreEqual((Transform)Campo(ordas, "inimigosAtivos"), inimigo.transform.parent);
            Assert.IsTrue((Object)Campo(inimigo, "gerenciadorDeOrdas") == null);
            if (haPontoDistante)
                Assert.GreaterOrEqual(Vector3.Distance(inimigo.transform.position, jogador.position), distanciaMinima - raio - 0.5f);
        }

        Definir(gerador, "intervaloInicial", 0.25f);
        Definir(gerador, "intervaloMinimo", 0.25f);
        Definir(gerador, "maximoDeInimigosVivos", 5);
        float fim = Time.time + 3f;
        while (Time.time < fim)
        {
            Assert.LessOrEqual(Vidas().Length, 5);
            yield return null;
        }
        Assert.AreEqual(5, Vidas().Length);

        Definir(gerador, "maximoDeInimigosVivos", 30);
        Chamar(gerador, "Parar");
        int vivosAoParar = Vidas().Length;
        yield return new WaitForSeconds(1f);
        Assert.AreEqual(vivosAoParar, Vidas().Length);

        Definir(gerador, "inimigosPrimeiraHorda", 30);
        Definir(gerador, "intervaloDoMiniBoss", 1f);
        Chamar(gerador, "Iniciar");
        yield return new WaitForSeconds(1.6f);
        Assert.AreEqual(1, MiniBosses());
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(1, MiniBosses());
    }

    [UnityTest]
    public IEnumerator HordasEstadioContinuamAteAMorte() => ConferirHordas(Estadio);

    [UnityTest]
    public IEnumerator HordasRuaContinuamAteAMorte() => ConferirHordas(Rua);

    IEnumerator ConferirHordas(string cena)
    {
        yield return EntrarNoInfinito(cena);
        var jogo = Encontrar("GerenciadorDeJogo");
        var gerador = Encontrar("GeradorDeInimigosInfinito");
        var manager = Encontrar("GerenciadorModoInfinito");
        TornarJogadorInvencivel();
        Definir(gerador, "inimigosPrimeiraHorda", 2);
        Definir(gerador, "aumentoPorHorda", 1);
        Definir(gerador, "tempoEntreHordas", 5f);
        Definir(gerador, "intervaloInicial", 0.25f);
        Definir(gerador, "intervaloMinimo", 0.25f);
        Chamar(jogo, "FecharTutorial");
        yield return null;
        yield return null;
        int abates = 0;
        // Avanca alem do numero de hordas da campanha, sem vitoria ou troca de fase.
        for (int horda = 1; horda <= 8; horda++)
        {
            Assert.AreEqual(horda, Propriedade(gerador, "NumeroDaHordaAtual"));
            Assert.AreEqual(horda + 1, Propriedade(gerador, "InimigosRestantesNaHorda"));
            for (int passo = 0; passo < 50 && !(bool)Propriedade(gerador, "EmIntervalo"); passo++)
            {
                foreach (var vida in Vidas())
                {
                    Chamar(vida, "ReceberDano", 10000);
                    abates++;
                }
                if (!(bool)Propriedade(gerador, "EmIntervalo")) Chamar(gerador, "Avancar", 0.3f, 1f);
                yield return null;
            }
            Assert.IsTrue((bool)Propriedade(gerador, "EmIntervalo"));
            Assert.AreEqual(horda, Propriedade(manager, "HordasSobrevividas"));
            Assert.AreEqual(abates, Propriedade(manager, "InimigosAbatidos"));
            Assert.AreEqual("Jogando", Estado(jogo));
            Assert.AreEqual(cena, SceneManager.GetActiveScene().name);
            Chamar(jogo, "Vitoria");
            Assert.AreEqual("Jogando", Estado(jogo));
            if (horda == 1)
            {
                Chamar(jogo, "Pausar");
                float intervalo = (float)Propriedade(gerador, "IntervaloRestante");
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.AreEqual(intervalo, Propriedade(gerador, "IntervaloRestante"));
                Chamar(jogo, "Continuar");
            }
            Chamar(gerador, "Avancar", 6f, 1f);
        }
        Assert.AreEqual(9, Propriedade(gerador, "NumeroDaHordaAtual"));
        MatarJogador();
        Assert.AreEqual("GameOver", Estado(jogo));
        Assert.AreEqual("8", TextoDe("TelaDeResultadoInfinito/Caixa/LinhaHordas/Valor"));
        Assert.AreEqual(abates.ToString(), TextoDe("TelaDeResultadoInfinito/Caixa/LinhaAbates/Valor"));
        Assert.IsFalse((bool)Propriedade(gerador, "Ativo"));
    }

    [UnityTest]
    public IEnumerator GeradorSemPrefabsNaoCriaInimigos()
    {
        yield return CarregarCena("Fases/" + Estadio);
        int antes = Vidas().Length;
        var objeto = new GameObject("GeradorSemPrefabs");
        var gerador = objeto.AddComponent(Tipo("GeradorDeInimigosInfinito"));
        Definir(gerador, "prefabsComuns", new GameObject[0]);
        LogAssert.Expect(LogType.Error, new Regex("sem prefabs comuns"));
        Chamar(gerador, "Iniciar");
        Assert.IsFalse((bool)Propriedade(gerador, "Ativo"));
        yield return null;
        Assert.AreEqual(antes, Vidas().Length);
        Object.Destroy(objeto);
    }

    [UnityTest]
    public IEnumerator NormalIsoladoDoInfinito()
    {
        yield return EntrarNoNormal();
        yield return ConferirEstadioNormal();

        ChamarEstatico("SessaoDeJogo", "IniciarInfinito", Rua);
        yield return CarregarCena("Fases/" + Estadio);
        yield return ConferirEstadioNormal();

        ChamarEstatico("SessaoDeJogo", "Reiniciar");
        yield return CarregarCena("Fases/" + Rua);
        var jogo = Encontrar("GerenciadorDeJogo");
        Assert.AreEqual("Jogando", Estado(jogo));
        Assert.IsFalse(((GameObject)Campo(jogo, "telaDeTutorial")).activeSelf);
    }

    IEnumerator ConferirEstadioNormal()
    {
        var jogo = Encontrar("GerenciadorDeJogo");
        var manager = Encontrar("GerenciadorModoInfinito");
        Assert.AreEqual("Tutorial", Estado(jogo));
        Assert.IsTrue(((GameObject)Campo(jogo, "telaDeTutorial")).activeSelf);
        Assert.IsFalse(((Behaviour)manager).enabled);
        Assert.IsFalse((bool)Propriedade(manager, "Ativo"));
        Assert.IsFalse(((Behaviour)Encontrar("GeradorDeInimigosInfinito")).enabled);
        Assert.IsFalse(Objeto("HUDInfinito").gameObject.activeSelf);
        Assert.IsFalse(Objeto("TelaDeResultadoInfinito").gameObject.activeSelf);
        Assert.IsTrue(Objeto("TextOrdas").gameObject.activeSelf);
        Assert.IsTrue(((Behaviour)Encontrar("Cronometro")).enabled);
        Assert.AreEqual("TelaDeGameOver", ((GameObject)Campo(jogo, "telaDeGameOver")).name);

        Chamar(jogo, "FecharTutorial");
        TornarJogadorInvencivel();
        yield return null;
        Assert.AreEqual(5, Vidas().Length);
        yield return new WaitForSeconds(3f);
        Assert.AreEqual(5, Vidas().Length);
    }
}
#endif

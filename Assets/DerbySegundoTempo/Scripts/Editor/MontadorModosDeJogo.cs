using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MontadorModosDeJogo
{
    const string Raiz = "Assets/DerbySegundoTempo";
    const string CenaInicial = Raiz + "/Cenas/Menus/TelaInicial.unity";
    const string CenaModos = Raiz + "/Cenas/Menus/SelecaoDeModo.unity";
    const string CenaCenarios = Raiz + "/Cenas/Menus/SelecaoDeCenario.unity";
    const string CenaEstadio = Raiz + "/Cenas/Fases/" + GerenciadorDeCenas.PrimeiraFase + ".unity";
    const string CenaRua = Raiz + "/Cenas/Fases/Rua_TPS.unity";
    const string CaminhoPigMan = Raiz + "/Prefabs/Inimigos/PigManMiniBoss.prefab";
    const string CaminhoPersonagemDerrota = Raiz + "/Arte/Imagens/num_vai_da.png";
    const string PastaDosCartoes = Raiz + "/Arte/Imagens/Interface/Cenarios";
    const string CartaoEstadio = PastaDosCartoes + "/cartao_estadio.png";
    const string CartaoRua = PastaDosCartoes + "/cartao_rua.png";
    const string OrigemEstadio = "Docs/Previews/TPS/estadio.png";
    const string OrigemRua = "Docs/Previews/Rua/rua.png";
    const string Pasta = "Library/ModosDeJogo";

    static readonly Color CorBranca = Color.white;

    // Permite montar no editor aberto pelo mesmo mecanismo das outras ferramentas do projeto.
    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        EditorApplication.update -= ExecutarPedido;
        EditorApplication.update += ExecutarPedido;
    }

    static void ExecutarPedido()
    {
        if (!File.Exists(Pasta + ".montar") || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Pasta + ".montar");
        try { MontarTudo(); }
        catch (Exception erro)
        {
            Directory.CreateDirectory(Pasta);
            File.WriteAllText(Pasta + "/erro.txt", erro.ToString());
            Debug.LogException(erro);
        }
    }

    [MenuItem("Derby/Modos de Jogo/Montar menus e modo infinito")]
    public static void MontarTudo()
    {
        if (!Application.isBatchMode)
        {
            Executar();
            return;
        }

        try
        {
            Executar();
        }
        catch (Exception erro)
        {
            Debug.LogException(erro);
            EditorApplication.Exit(1);
        }
    }

    static void Executar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Monte fora do Play Mode.");

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SceneSetup[] configuracaoAnterior = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            Sprite spriteEstadio = EstiloDerbyUI.ImportarCartao(OrigemEstadio, CartaoEstadio);
            Sprite spriteRua = EstiloDerbyUI.ImportarCartao(OrigemRua, CartaoRua);

            MontarTelaInicial();
            MontarCenaDeSelecao(MenuDeModos.Tela.Modos, CenaModos, spriteEstadio, spriteRua);
            MontarCenaDeSelecao(MenuDeModos.Tela.Cenarios, CenaCenarios, spriteEstadio, spriteRua);
            MontarFase(CenaEstadio, "Estádio");
            MontarFase(CenaRua, "Rua");
            AtualizarCenasDoBuild();

            Directory.CreateDirectory(Pasta);
            File.WriteAllText(Pasta + "/status.txt",
                "cenas montadas: 5\n" + CenaInicial + "\n" + CenaModos + "\n" + CenaCenarios + "\n" + CenaEstadio + "\n" + CenaRua + "\n");
        }
        finally
        {
            if (configuracaoAnterior.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(configuracaoAnterior);
            }
        }
    }

    static void MontarTelaInicial()
    {
        Scene cena = EditorSceneManager.OpenScene(CenaInicial, OpenSceneMode.Single);

        Transform canvas = ExigirPorNome(cena, "Interface_Principal");
        Transform menuManager = ExigirPorNome(cena, "MenuManager");
        Button botaoIniciar = ExigirPorNome(cena, "BotaoIniciar").GetComponent<Button>();
        Button botaoSair = ExigirPorNome(cena, "BotaoSair").GetComponent<Button>();

        if (botaoIniciar == null) throw new InvalidOperationException("Objeto ausente: BotaoIniciar (Button)");
        if (botaoSair == null) throw new InvalidOperationException("Objeto ausente: BotaoSair (Button)");

        GerenciadorDeCenas gerenciadorDeCenas = menuManager.GetComponent<GerenciadorDeCenas>();
        if (gerenciadorDeCenas == null) throw new InvalidOperationException("Objeto ausente: GerenciadorDeCenas em MenuManager");

        ApagarFilhos(canvas, "PainelEscolhaDeModo");
        ApagarFilhos(canvas, "PainelEscolhaDeCenario");

        MenuDeModos menu = menuManager.GetComponent<MenuDeModos>();
        if (menu == null) menu = menuManager.gameObject.AddComponent<MenuDeModos>();

        PrepararBotaoDoMenuPrincipal(botaoIniciar);
        PrepararBotaoDoMenuPrincipal(botaoSair);
        DefinirNavegacaoDoMenuPrincipal(botaoIniciar, botaoSair);

        for (int i = botaoIniciar.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(botaoIniciar.onClick, i);
        }
        UnityEventTools.AddPersistentListener(botaoIniciar.onClick, new UnityAction(menu.AbrirEscolhaDeModo));
        EditorUtility.SetDirty(botaoIniciar);

        EstiloDerbyUI.LigarCampo(menu, "gerenciadorDeCenas", gerenciadorDeCenas);
        EstiloDerbyUI.LigarCampo(menu, "botaoJogar", botaoIniciar);
        EstiloDerbyUI.LigarCampo(menu, "botaoSair", botaoSair);
        DefinirTelaInicial(menu, MenuDeModos.Tela.Principal);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
    }

    static void DefinirTelaInicial(MenuDeModos menu, MenuDeModos.Tela tela)
    {
        var serializado = new SerializedObject(menu);
        serializado.FindProperty("telaInicial").enumValueIndex = (int)tela;
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    static void MontarCenaDeSelecao(MenuDeModos.Tela tela, string caminho, Sprite spriteEstadio, Sprite spriteRua)
    {
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sistemas = new GameObject("MenuManager");
        var gerenciadorDeCenas = sistemas.AddComponent<GerenciadorDeCenas>();
        var menu = sistemas.AddComponent<MenuDeModos>();
        EstiloDerbyUI.LigarCampo(menu, "gerenciadorDeCenas", gerenciadorDeCenas);
        DefinirTelaInicial(menu, tela);

        var canvas = new GameObject("Interface_Principal", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var camera = new GameObject("CameraMenu", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = EstiloDerbyUI.CorPainel;

        if (tela == MenuDeModos.Tela.Modos)
        {
            GameObject painel = MontarPainelDeModos(canvas.transform, menu,
                out Button normal, out Button infinito, out Button voltar);
            painel.SetActive(true);
            DefinirNavegacaoDosModos(normal, infinito, voltar);
            EstiloDerbyUI.LigarCampo(menu, "painelModos", painel);
            EstiloDerbyUI.LigarCampo(menu, "botaoModoNormal", normal);
            EstiloDerbyUI.LigarCampo(menu, "botaoModoInfinito", infinito);
            EstiloDerbyUI.LigarCampo(menu, "botaoVoltarModos", voltar);
        }
        else
        {
            GameObject painel = MontarPainelDeCenarios(canvas.transform, menu, spriteEstadio, spriteRua,
                out CartaoDeCenario estadio, out CartaoDeCenario rua, out Button voltar, out Button iniciar);
            painel.SetActive(true);
            DefinirNavegacaoDosCenarios(estadio.GetComponent<Button>(), rua.GetComponent<Button>(), voltar, iniciar);
            EstiloDerbyUI.LigarCampo(menu, "painelCenarios", painel);
            EstiloDerbyUI.LigarLista(menu, "cartoes", new List<Object> { estadio, rua });
            EstiloDerbyUI.LigarCampo(menu, "botaoIniciarSobrevivencia", iniciar);
            EstiloDerbyUI.LigarCampo(menu, "botaoVoltarCenarios", voltar);
        }

        EditorSceneManager.SaveScene(cena, caminho);
    }

    static void AtualizarCenasDoBuild()
    {
        var cenas = EditorBuildSettings.scenes.ToList();
        foreach (string caminho in new[] { CenaInicial, CenaModos, CenaCenarios, CenaEstadio, CenaRua })
        {
            int indice = cenas.FindIndex(c => c.path == caminho);
            if (indice >= 0) cenas[indice].enabled = true;
            else cenas.Add(new EditorBuildSettingsScene(caminho, true));
        }
        EditorBuildSettings.scenes = cenas.ToArray();
        AssetDatabase.SaveAssets();
    }

    static void PrepararBotaoDoMenuPrincipal(Button botao)
    {
        Transform focoAntigo = botao.transform.Find("Foco");
        while (focoAntigo != null)
        {
            Object.DestroyImmediate(focoAntigo.gameObject);
            focoAntigo = botao.transform.Find("Foco");
        }

        GameObject foco = EstiloDerbyUI.CriarIndicadorDeFoco(botao.transform, EstiloDerbyUI.CorTextoClaro, 5f, 8f);
        EstiloDerbyUI.AdicionarRealce(botao.gameObject, foco, 1f);
    }

    static void DefinirNavegacaoDoMenuPrincipal(Button botaoIniciar, Button botaoSair)
    {
        EstiloDerbyUI.DefinirNavegacao(botaoIniciar, null, botaoSair, null, null);
        EstiloDerbyUI.DefinirNavegacao(botaoSair, botaoIniciar, null, null, null);
    }

    static GameObject MontarPainelDeModos(Transform canvas, MenuDeModos menu,
        out Button botaoNormal, out Button botaoInfinito, out Button botaoVoltar)
    {
        GameObject painel = EstiloDerbyUI.CriarTela(canvas, "PainelEscolhaDeModo", EstiloDerbyUI.CorFundoMenu, false);
        painel.transform.SetAsLastSibling();

        EstiloDerbyUI.CriarPainel(painel.transform, "Caixa", Vector2.zero, new Vector2(1100f, 720f),
            EstiloDerbyUI.CorPainel, EstiloDerbyUI.CorVermelho);

        EstiloDerbyUI.CriarTexto(painel.transform, "Titulo", "ESCOLHA O MODO DE JOGO", 64, CorBranca,
            new Vector2(0f, 290f), new Vector2(1000f, 90f), FontStyles.Bold);
        EstiloDerbyUI.CriarTexto(painel.transform, "Subtitulo",
            "Mouse ou setas para navegar, ENTER confirma, ESC volta.", 24, EstiloDerbyUI.CorTextoSuave,
            new Vector2(0f, 232f), new Vector2(1000f, 40f), FontStyles.Normal);

        botaoNormal = CriarBotaoDeOpcao(painel.transform, "BotaoModoNormal", "CLÁSSICO",
            "Complete as hordas do estádio e avance para a rua.", new Vector2(0f, 110f), menu.IniciarModoNormal);
        botaoInfinito = CriarBotaoDeOpcao(painel.transform, "BotaoModoInfinito", "INFINITO",
            "Sobreviva a hordas sem limite no estádio ou na rua.", new Vector2(0f, -80f), menu.AbrirEscolhaDeCenario);

        botaoVoltar = EstiloDerbyUI.CriarBotao(painel.transform, "BotaoVoltarModos", "Voltar",
            new Vector2(0f, -270f), new Vector2(360f, 80f),
            EstiloDerbyUI.CorBotao2, EstiloDerbyUI.CorBotao2Claro, EstiloDerbyUI.CorBotao2Escuro,
            EstiloDerbyUI.CorTextoClaro, menu.Voltar);

        return painel;
    }

    static Button CriarBotaoDeOpcao(Transform pai, string nome, string titulo, string descricao,
        Vector2 posicao, UnityAction acao)
    {
        GameObject objeto = EstiloDerbyUI.CriarObjetoUI(nome, pai);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(980f, 170f);

        Image imagem = objeto.AddComponent<Image>();
        imagem.color = Color.white;
        Button botao = objeto.AddComponent<Button>();
        botao.targetGraphic = imagem;

        ColorBlock cores = botao.colors;
        cores.normalColor = EstiloDerbyUI.CorBotao2Claro;
        cores.highlightedColor = EstiloDerbyUI.CorOuro;
        cores.selectedColor = EstiloDerbyUI.CorOuro;
        cores.pressedColor = EstiloDerbyUI.CorBotaoEscuro;
        botao.colors = cores;

        CriarFundoInterno(objeto.transform, "Fundo", EstiloDerbyUI.CorPainel, 6f);

        GameObject barra = EstiloDerbyUI.CriarObjetoUI("BarraLateral", objeto.transform);
        RectTransform rectBarra = barra.GetComponent<RectTransform>();
        rectBarra.anchorMin = new Vector2(0f, 0f);
        rectBarra.anchorMax = new Vector2(0f, 1f);
        rectBarra.pivot = new Vector2(0f, 0.5f);
        rectBarra.anchoredPosition = new Vector2(6f, 0f);
        rectBarra.sizeDelta = new Vector2(14f, -12f);
        Image imagemBarra = barra.AddComponent<Image>();
        imagemBarra.color = EstiloDerbyUI.CorVermelho;
        imagemBarra.raycastTarget = false;

        TMP_Text textoTitulo = EstiloDerbyUI.CriarTexto(objeto.transform, "Titulo", titulo, 52, CorBranca,
            Vector2.zero, Vector2.zero, FontStyles.Bold, TextAlignmentOptions.Left);
        Ancorar(textoTitulo.rectTransform, new Vector2(0f, 0.5f), Vector2.one, new Vector2(48f, 0f), new Vector2(-20f, -8f));

        TMP_Text textoDescricao = EstiloDerbyUI.CriarTexto(objeto.transform, "Descricao", descricao, 30,
            EstiloDerbyUI.CorTextoClaro, Vector2.zero, Vector2.zero, FontStyles.Normal, TextAlignmentOptions.Left);
        Ancorar(textoDescricao.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(48f, 8f), new Vector2(-20f, 0f));

        UnityEventTools.AddPersistentListener(botao.onClick, acao);
        EstiloDerbyUI.AdicionarRealce(objeto, EstiloDerbyUI.CriarIndicadorDeFoco(objeto.transform, EstiloDerbyUI.CorTextoClaro), 1.03f);

        return botao;
    }

    static GameObject MontarPainelDeCenarios(Transform canvas, MenuDeModos menu, Sprite spriteEstadio, Sprite spriteRua,
        out CartaoDeCenario cartaoEstadio, out CartaoDeCenario cartaoRua,
        out Button botaoVoltar, out Button botaoIniciar)
    {
        GameObject painel = EstiloDerbyUI.CriarTela(canvas, "PainelEscolhaDeCenario", EstiloDerbyUI.CorFundoMenu, false);
        painel.transform.SetAsLastSibling();

        EstiloDerbyUI.CriarTexto(painel.transform, "Kicker", "MODO INFINITO", 26, EstiloDerbyUI.CorVermelho,
            new Vector2(0f, 470f), new Vector2(1000f, 40f), FontStyles.Bold);
        EstiloDerbyUI.CriarTexto(painel.transform, "Titulo", "Onde você quer sobreviver?", 64, CorBranca,
            new Vector2(0f, 410f), new Vector2(1400f, 90f), FontStyles.Bold);

        cartaoEstadio = CriarCartao(painel.transform, menu, "CartaoEstadio", -410f, GerenciadorDeCenas.PrimeiraFase,
            "ESTÁDIO", "Sobreviva no gramado cercado pelas arquibancadas do Derby.", spriteEstadio);
        cartaoRua = CriarCartao(painel.transform, menu, "CartaoRua", 410f, GerenciadorDeCenas.SegundaFase,
            "RUA", "Resista entre prédios, oficinas e farmácias do bairro.", spriteRua);

        EstiloDerbyUI.CriarTexto(painel.transform, "Dica", "Clique em um cenário ou use as setas e ENTER para escolher.",
            24, EstiloDerbyUI.CorTextoSuave, new Vector2(0f, -330f), new Vector2(1400f, 40f), FontStyles.Normal);

        botaoVoltar = EstiloDerbyUI.CriarBotao(painel.transform, "BotaoVoltarCenarios", "Voltar",
            new Vector2(-330f, -420f), new Vector2(360f, 84f),
            EstiloDerbyUI.CorBotao2, EstiloDerbyUI.CorBotao2Claro, EstiloDerbyUI.CorBotao2Escuro,
            EstiloDerbyUI.CorTextoClaro, menu.Voltar);

        botaoIniciar = EstiloDerbyUI.CriarBotao(painel.transform, "BotaoIniciarSobrevivencia", "Iniciar sobrevivência",
            new Vector2(190f, -420f), new Vector2(560f, 84f),
            EstiloDerbyUI.CorVermelho, EstiloDerbyUI.CorVermelhoClaro, EstiloDerbyUI.CorVermelhoEscuro,
            CorBranca, menu.IniciarSobrevivencia);

        return painel;
    }

    static CartaoDeCenario CriarCartao(Transform pai, MenuDeModos menu, string nome, float x, string cena,
        string titulo, string descricao, Sprite sprite)
    {
        GameObject objeto = EstiloDerbyUI.CriarObjetoUI(nome, pai);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, 20f);
        rect.sizeDelta = new Vector2(760f, 620f);

        Image moldura = objeto.AddComponent<Image>();
        moldura.color = CartaoDeCenario.CorNormal;
        Button botao = objeto.AddComponent<Button>();
        botao.transition = Selectable.Transition.None;
        botao.targetGraphic = moldura;

        CriarFundoInterno(objeto.transform, "Fundo", EstiloDerbyUI.CorPainel, 6f);

        Image imagem = EstiloDerbyUI.CriarImagem(objeto.transform, "Imagem", sprite,
            new Vector2(0f, 65f), new Vector2(720f, 450f), false);

        EstiloDerbyUI.CriarTexto(objeto.transform, "Nome", titulo, 44, CorBranca,
            new Vector2(0f, -215f), new Vector2(700f, 56f), FontStyles.Bold);
        EstiloDerbyUI.CriarTexto(objeto.transform, "Descricao", descricao, 26, EstiloDerbyUI.CorTextoClaro,
            new Vector2(0f, -268f), new Vector2(680f, 60f), FontStyles.Normal, TextAlignmentOptions.Center, true);

        GameObject selo = EstiloDerbyUI.CriarObjetoUI("SeloSelecionado", objeto.transform);
        RectTransform rectSelo = selo.GetComponent<RectTransform>();
        rectSelo.anchoredPosition = new Vector2(250f, 255f);
        rectSelo.sizeDelta = new Vector2(220f, 52f);
        Image imagemSelo = selo.AddComponent<Image>();
        imagemSelo.color = EstiloDerbyUI.CorOuro;
        imagemSelo.raycastTarget = false;
        TMP_Text textoSelo = EstiloDerbyUI.CriarTexto(selo.transform, "Texto", "SELECIONADO", 24,
            EstiloDerbyUI.CorTextoBotao, Vector2.zero, Vector2.zero, FontStyles.Bold);
        EstiloDerbyUI.Esticar(textoSelo.rectTransform);
        selo.SetActive(false);

        EstiloDerbyUI.AdicionarRealce(objeto, EstiloDerbyUI.CriarIndicadorDeFoco(objeto.transform, EstiloDerbyUI.CorTextoClaro), 1.02f);

        CartaoDeCenario cartao = objeto.AddComponent<CartaoDeCenario>();
        EstiloDerbyUI.LigarCampo(cartao, "menu", menu);
        EstiloDerbyUI.LigarCampo(cartao, "moldura", moldura);
        EstiloDerbyUI.LigarCampo(cartao, "imagem", imagem);
        EstiloDerbyUI.LigarCampo(cartao, "seloSelecionado", selo);

        SerializedObject cartaoSerializado = new SerializedObject(cartao);
        SerializedProperty nomeDaCena = cartaoSerializado.FindProperty("nomeDaCena");
        if (nomeDaCena == null) throw new InvalidOperationException("Campo ausente: CartaoDeCenario.nomeDaCena");
        nomeDaCena.stringValue = cena;
        cartaoSerializado.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(botao.onClick, new UnityAction(cartao.Escolher));

        return cartao;
    }

    static void DefinirNavegacaoDosModos(Button normal, Button infinito, Button voltar)
    {
        EstiloDerbyUI.DefinirNavegacao(normal, null, infinito, null, null);
        EstiloDerbyUI.DefinirNavegacao(infinito, normal, voltar, null, null);
        EstiloDerbyUI.DefinirNavegacao(voltar, infinito, null, null, null);
    }

    static void DefinirNavegacaoDosCenarios(Button estadio, Button rua, Button voltar, Button iniciar)
    {
        EstiloDerbyUI.DefinirNavegacao(estadio, null, voltar, null, rua);
        EstiloDerbyUI.DefinirNavegacao(rua, null, iniciar, estadio, null);
        EstiloDerbyUI.DefinirNavegacao(voltar, estadio, null, null, iniciar);
        EstiloDerbyUI.DefinirNavegacao(iniciar, rua, null, voltar, null);
    }

    static void MontarFase(string caminho, string nomeDoCenario)
    {
        Scene cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

        GerenciadorDeJogo gerenciadorDeJogo = ExigirUnico<GerenciadorDeJogo>(cena);
        GerenciadorDeOrdas gerenciadorDeOrdas = ExigirUnico<GerenciadorDeOrdas>(cena);
        Cronometro cronometro = ExigirUnico<Cronometro>(cena);
        SistemaDePontos sistemaDePontos = ExigirUnico<SistemaDePontos>(cena);
        JogadorVida jogadorVida = ExigirUnico<JogadorVida>(cena);

        Transform sistemas = cena.GetRootGameObjects().Select(raiz => raiz.transform)
            .FirstOrDefault(raiz => raiz.name == "00_Sistemas");
        if (sistemas == null) throw new InvalidOperationException("Objeto ausente: 00_Sistemas (" + cena.name + ")");

        GerenciadorDeCenas gerenciadorDeCenas = LerCampo(gerenciadorDeJogo, "gerenciadorDeCenas") as GerenciadorDeCenas;
        if (gerenciadorDeCenas == null) throw new InvalidOperationException("Referencia ausente: GerenciadorDeJogo.gerenciadorDeCenas (" + cena.name + ")");

        GameObject telaDeGameOver = LerCampo(gerenciadorDeJogo, "telaDeGameOver") as GameObject;
        if (telaDeGameOver == null) throw new InvalidOperationException("Referencia ausente: GerenciadorDeJogo.telaDeGameOver (" + cena.name + ")");
        Transform canvasTelas = telaDeGameOver.transform.parent;
        if (canvasTelas == null) throw new InvalidOperationException("Objeto ausente: CanvasTelas (" + cena.name + ")");

        if (sistemaDePontos.textoPontos == null) throw new InvalidOperationException("Referencia ausente: SistemaDePontos.textoPontos (" + cena.name + ")");
        Canvas canvasDoHud = sistemaDePontos.textoPontos.GetComponentInParent<Canvas>();
        if (canvasDoHud == null) throw new InvalidOperationException("Objeto ausente: Canvas do HUD (" + cena.name + ")");
        Transform raizDoHud = canvasDoHud.rootCanvas.transform;

        GameObject prefabPigMan = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPigMan);
        if (prefabPigMan == null) throw new InvalidOperationException("Prefab ausente: " + CaminhoPigMan);

        Sprite spriteDerrota = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoPersonagemDerrota);
        if (spriteDerrota == null) throw new InvalidOperationException("Sprite ausente: " + CaminhoPersonagemDerrota);

        ApagarFilhos(sistemas, "ModoInfinito");
        ApagarFilhos(raizDoHud, "HUDInfinito");
        ApagarFilhos(canvasTelas, "TelaDeResultadoInfinito");

        GameObject modoInfinito = new GameObject("ModoInfinito");
        modoInfinito.transform.SetParent(sistemas, false);
        GeradorDeInimigosInfinito gerador = modoInfinito.AddComponent<GeradorDeInimigosInfinito>();
        GerenciadorModoInfinito infinito = modoInfinito.AddComponent<GerenciadorModoInfinito>();

        List<Object> prefabsComuns = ListarPrefabsComuns(gerenciadorDeOrdas);
        EstiloDerbyUI.LigarLista(gerador, "prefabsComuns", prefabsComuns);
        EstiloDerbyUI.LigarCampo(gerador, "prefabMiniBoss", prefabPigMan);
        EstiloDerbyUI.LigarCampo(gerador, "origemDosPontos", gerenciadorDeOrdas);
        EstiloDerbyUI.LigarCampo(gerador, "jogador", jogadorVida.transform);

        GameObject hud = MontarHud(raizDoHud, out TMP_Text textoTempo, out TMP_Text textoAbates, out TMP_Text textoHorda);
        GameObject telaDeResultado = MontarTelaDeResultado(canvasTelas, infinito, spriteDerrota, nomeDoCenario,
            out TMP_Text resultadoCenario, out TMP_Text resultadoPontos, out TMP_Text resultadoAbates,
            out TMP_Text resultadoTempo, out TMP_Text resultadoHordas, out TMP_Text resultadoSaldo, out Button botaoTentarNovamente);

        SerializedObject infinitoSerializado = new SerializedObject(infinito);
        SerializedProperty propriedadeNome = infinitoSerializado.FindProperty("nomeDoCenario");
        if (propriedadeNome == null) throw new InvalidOperationException("Campo ausente: GerenciadorModoInfinito.nomeDoCenario");
        propriedadeNome.stringValue = nomeDoCenario;
        infinitoSerializado.ApplyModifiedPropertiesWithoutUndo();

        EstiloDerbyUI.LigarCampo(infinito, "gerenciadorDeJogo", gerenciadorDeJogo);
        EstiloDerbyUI.LigarCampo(infinito, "gerenciadorDeOrdas", gerenciadorDeOrdas);
        EstiloDerbyUI.LigarCampo(infinito, "cronometro", cronometro);
        EstiloDerbyUI.LigarCampo(infinito, "gerenciadorDeCenas", gerenciadorDeCenas);
        EstiloDerbyUI.LigarCampo(infinito, "gerador", gerador);
        EstiloDerbyUI.LigarCampo(infinito, "hudInfinito", hud);
        EstiloDerbyUI.LigarCampo(infinito, "textoTempoSobrevivencia", textoTempo);
        EstiloDerbyUI.LigarCampo(infinito, "textoAbates", textoAbates);
        EstiloDerbyUI.LigarCampo(infinito, "textoHorda", textoHorda);

        var somenteNormal = new List<Object>();
        if (gerenciadorDeOrdas.textoOrda != null) somenteNormal.Add(gerenciadorDeOrdas.textoOrda.gameObject);
        Component textoDoCronometro = LerCampo(cronometro, "textoTempo") as Component;
        if (textoDoCronometro != null) somenteNormal.Add(textoDoCronometro.gameObject);
        EstiloDerbyUI.LigarLista(infinito, "objetosSomenteModoNormal", somenteNormal);

        EstiloDerbyUI.LigarCampo(infinito, "telaDeResultado", telaDeResultado);
        EstiloDerbyUI.LigarCampo(infinito, "textoResultadoCenario", resultadoCenario);
        EstiloDerbyUI.LigarCampo(infinito, "textoResultadoPontos", resultadoPontos);
        EstiloDerbyUI.LigarCampo(infinito, "textoResultadoAbates", resultadoAbates);
        EstiloDerbyUI.LigarCampo(infinito, "textoResultadoTempo", resultadoTempo);
        EstiloDerbyUI.LigarCampo(infinito, "textoResultadoHordas", resultadoHordas);
        EstiloDerbyUI.LigarCampo(infinito, "textoResultadoSaldo", resultadoSaldo);
        EstiloDerbyUI.LigarCampo(infinito, "botaoTentarNovamente", botaoTentarNovamente);

        GameObject telaDeTutorial = LerCampo(gerenciadorDeJogo, "telaDeTutorial") as GameObject;
        Transform objetivo = telaDeTutorial != null ? telaDeTutorial.transform.Find("PainelComandos/Objetivo") : null;
        TMP_Text textoObjetivo = objetivo != null ? objetivo.GetComponent<TMP_Text>() : null;
        if (textoObjetivo != null)
            EstiloDerbyUI.LigarCampo(infinito, "textoObjetivoTutorial", textoObjetivo);
        else
            Debug.LogWarning("TelaDeTutorial/PainelComandos/Objetivo ausente em " + cena.name, gerenciadorDeJogo);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
    }

    static List<Object> ListarPrefabsComuns(GerenciadorDeOrdas gerenciadorDeOrdas)
    {
        var prefabs = new List<Object>();

        foreach (GerenciadorDeOrdas.Orda orda in gerenciadorDeOrdas.ordas)
        {
            if (orda == null || orda.prefabsInimigos == null) continue;

            foreach (GameObject prefab in orda.prefabsInimigos)
            {
                if (prefab == null) continue;
                if (AssetDatabase.GetAssetPath(prefab) == CaminhoPigMan) continue;
                if (!prefabs.Contains(prefab)) prefabs.Add(prefab);
            }
        }

        return prefabs;
    }

    static GameObject MontarHud(Transform raizDoHud, out TMP_Text textoTempo, out TMP_Text textoAbates, out TMP_Text textoHorda)
    {
        GameObject hud = EstiloDerbyUI.CriarObjetoUI("HUDInfinito", raizDoHud);
        RectTransform rect = hud.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.sizeDelta = new Vector2(740f, 170f);

        Image fundo = hud.AddComponent<Image>();
        Color corDoFundo = EstiloDerbyUI.CorPainel;
        corDoFundo.a = 0.6f;
        fundo.color = corDoFundo;
        fundo.raycastTarget = false;

        textoTempo = EstiloDerbyUI.CriarTexto(hud.transform, "TextoTempoSobrevivencia", "TEMPO: 00:00", 48,
            EstiloDerbyUI.CorOuro, new Vector2(0f, -38f), new Vector2(540f, 60f), FontStyles.Bold);
        AncorarNoTopo(textoTempo.rectTransform);

        textoAbates = EstiloDerbyUI.CriarTexto(hud.transform, "TextoAbates", "ABATES: 0", 32,
            CorBranca, new Vector2(0f, -95f), new Vector2(540f, 44f), FontStyles.Bold);
        AncorarNoTopo(textoAbates.rectTransform);

        textoHorda = EstiloDerbyUI.CriarTexto(hud.transform, "TextoHorda", "HORDA 1", 28,
            CorBranca, new Vector2(0f, -140f), new Vector2(720f, 42f), FontStyles.Bold);
        AncorarNoTopo(textoHorda.rectTransform);

        hud.SetActive(false);
        return hud;
    }

    static GameObject MontarTelaDeResultado(Transform canvasTelas, GerenciadorModoInfinito infinito, Sprite personagem,
        string nomeDoCenario, out TMP_Text valorCenario, out TMP_Text valorPontos, out TMP_Text valorAbates,
        out TMP_Text valorTempo, out TMP_Text valorHordas, out TMP_Text valorSaldo, out Button botaoTentarNovamente)
    {
        GameObject tela = EstiloDerbyUI.CriarTela(canvasTelas, "TelaDeResultadoInfinito", EstiloDerbyUI.CorFundoDerrota, false);

        float proporcao = personagem.rect.width / personagem.rect.height;
        float altura = 776f;
        EstiloDerbyUI.CriarImagem(tela.transform, "Personagem", personagem, new Vector2(500f, -20f),
            new Vector2(altura * proporcao, altura));

        GameObject caixa = EstiloDerbyUI.CriarPainel(tela.transform, "Caixa", new Vector2(-440f, 0f),
            new Vector2(880f, 930f), EstiloDerbyUI.CorPainelDerrota, EstiloDerbyUI.CorVermelho);

        EstiloDerbyUI.CriarTexto(caixa.transform, "Titulo", "FIM DA SOBREVIVÊNCIA", 64, EstiloDerbyUI.CorVermelho,
            new Vector2(0f, 400f), new Vector2(820f, 90f), FontStyles.Bold);
        EstiloDerbyUI.CriarTexto(caixa.transform, "Subtitulo", "SUA VIDA CHEGOU A ZERO!", 30, EstiloDerbyUI.CorTextoClaro,
            new Vector2(0f, 330f), new Vector2(820f, 44f), FontStyles.Normal);

        valorCenario = CriarLinhaDeResultado(caixa.transform, "LinhaCenario", "CENÁRIO", nomeDoCenario, 255f);
        valorPontos = CriarLinhaDeResultado(caixa.transform, "LinhaPontos", "PONTOS TOTAIS COLETADOS", "0", 195f);
        valorSaldo = CriarLinhaDeResultado(caixa.transform, "LinhaSaldo", "SALDO NÃO GASTO", "0", 135f);
        valorAbates = CriarLinhaDeResultado(caixa.transform, "LinhaAbates", "INIMIGOS ABATIDOS", "0", 75f);
        valorHordas = CriarLinhaDeResultado(caixa.transform, "LinhaHordas", "HORDAS SOBREVIVIDAS", "0", 15f);
        valorTempo = CriarLinhaDeResultado(caixa.transform, "LinhaTempo", "TEMPO DE SOBREVIVÊNCIA", "00:00", -45f);

        botaoTentarNovamente = EstiloDerbyUI.CriarBotao(caixa.transform, "BotaoTentarNovamente", "Tentar novamente",
            new Vector2(0f, -160f), new Vector2(460f, 76f),
            EstiloDerbyUI.CorBotao, EstiloDerbyUI.CorBotaoClaro, EstiloDerbyUI.CorBotaoEscuro,
            EstiloDerbyUI.CorTextoBotao, infinito.TentarNovamente);
        Button botaoEscolherCenario = EstiloDerbyUI.CriarBotao(caixa.transform, "BotaoEscolherCenario", "Escolher cenário",
            new Vector2(0f, -250f), new Vector2(460f, 76f),
            EstiloDerbyUI.CorBotao2, EstiloDerbyUI.CorBotao2Claro, EstiloDerbyUI.CorBotao2Escuro,
            EstiloDerbyUI.CorTextoClaro, infinito.EscolherCenario);
        Button botaoMenuInicial = EstiloDerbyUI.CriarBotao(caixa.transform, "BotaoMenuInicial", "Menu inicial",
            new Vector2(0f, -340f), new Vector2(460f, 76f),
            EstiloDerbyUI.CorBotao2, EstiloDerbyUI.CorBotao2Claro, EstiloDerbyUI.CorBotao2Escuro,
            EstiloDerbyUI.CorTextoClaro, infinito.VoltarAoMenuInicial);

        EstiloDerbyUI.DefinirNavegacao(botaoTentarNovamente, null, botaoEscolherCenario, null, null);
        EstiloDerbyUI.DefinirNavegacao(botaoEscolherCenario, botaoTentarNovamente, botaoMenuInicial, null, null);
        EstiloDerbyUI.DefinirNavegacao(botaoMenuInicial, botaoEscolherCenario, null, null, null);

        return tela;
    }

    static TMP_Text CriarLinhaDeResultado(Transform pai, string nome, string rotulo, string valor, float y)
    {
        GameObject linha = EstiloDerbyUI.CriarObjetoUI(nome, pai);
        RectTransform rect = linha.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(760f, 56f);

        TMP_Text textoRotulo = EstiloDerbyUI.CriarTexto(linha.transform, "Rotulo", rotulo, 28,
            EstiloDerbyUI.CorTextoSuave, Vector2.zero, Vector2.zero, FontStyles.Normal, TextAlignmentOptions.Left);
        Ancorar(textoRotulo.rectTransform, Vector2.zero, new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero);

        TMP_Text textoValor = EstiloDerbyUI.CriarTexto(linha.transform, "Valor", valor, 34,
            EstiloDerbyUI.CorOuro, Vector2.zero, Vector2.zero, FontStyles.Bold, TextAlignmentOptions.Right);
        Ancorar(textoValor.rectTransform, new Vector2(0.6f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

        return textoValor;
    }

    static void CriarFundoInterno(Transform pai, string nome, Color cor, float margem)
    {
        GameObject fundo = EstiloDerbyUI.CriarObjetoUI(nome, pai);
        Ancorar(fundo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(margem, margem), new Vector2(-margem, -margem));

        Image imagem = fundo.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
    }

    static void Ancorar(RectTransform rect, Vector2 ancoraMinima, Vector2 ancoraMaxima,
        Vector2 deslocamentoMinimo, Vector2 deslocamentoMaximo)
    {
        rect.anchorMin = ancoraMinima;
        rect.anchorMax = ancoraMaxima;
        rect.offsetMin = deslocamentoMinimo;
        rect.offsetMax = deslocamentoMaximo;
    }

    static void AncorarNoTopo(RectTransform rect)
    {
        Vector2 posicao = rect.anchoredPosition;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = posicao;
    }

    static void ApagarFilhos(Transform pai, string nome)
    {
        Transform filho = pai.Find(nome);
        while (filho != null)
        {
            Object.DestroyImmediate(filho.gameObject);
            filho = pai.Find(nome);
        }
    }

    static Transform ExigirPorNome(Scene cena, string nome)
    {
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            foreach (Transform candidato in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (candidato.name == nome) return candidato;
            }
        }

        throw new InvalidOperationException("Objeto ausente: " + nome);
    }

    static T ExigirUnico<T>(Scene cena) where T : Component
    {
        var encontrados = new List<T>();
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            encontrados.AddRange(raiz.GetComponentsInChildren<T>(true));
        }

        if (encontrados.Count != 1)
            throw new InvalidOperationException("Esperado exatamente 1 " + typeof(T).Name + " em " + cena.name
                + ", encontrados " + encontrados.Count);

        return encontrados[0];
    }

    static Object LerCampo(Object alvo, string campo)
    {
        SerializedProperty propriedade = new SerializedObject(alvo).FindProperty(campo);
        return propriedade != null ? propriedade.objectReferenceValue : null;
    }
}

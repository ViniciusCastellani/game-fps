using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

// Ferramenta de Editor (fica na pasta "Editor", então NÃO vai para o build).
// Aparece no menu "Jogo" da barra superior da Unity.
//
// Reconstrói, na cena de gameplay aberta, as telas de Tutorial, Vitória e Game Over
// usando as ilustrações do personagem, e religa todas as referências do GerenciadorDeJogo.
//
// Pode ser executada quantas vezes quiser: as telas antigas são apagadas e refeitas,
// então ajustar um valor aqui e rodar de novo é o fluxo de trabalho esperado.
public static class MontadorDeTelasIlustradas
{
    // Mesma resolução de referência da CanvasScaler montada por MontadorDeTelas.
    const float LarguraDaTela = 1920f;
    const float AlturaDaTela = 1080f;

    const string CaminhoTutorial = "Assets/MEU_PROJETO/Imagens/aprende_ai_zeruela.png";
    const string CaminhoVitoria = "Assets/MEU_PROJETO/Imagens/vitoria_confetti.png";
    const string CaminhoDerrota = "Assets/MEU_PROJETO/Imagens/num_vai_da.png";

    // ---------- Paleta ----------
    // Preto azulado do fundo, dourado do "ZÉ RUELA" e vermelho da derrota.
    static readonly Color CorFundoTutorial = new Color(0.024f, 0.031f, 0.051f, 0.96f);
    static readonly Color CorFundoDerrota = new Color(0.039f, 0.020f, 0.027f, 0.97f);
    static readonly Color CorPainel = new Color(0.055f, 0.078f, 0.125f, 0.96f);
    static readonly Color CorPainelSobreFoto = new Color(0.027f, 0.035f, 0.059f, 1f);
    static readonly Color CorPainelDerrota = new Color(0.078f, 0.039f, 0.051f, 0.94f);
    static readonly Color CorOuro = new Color(1f, 0.776f, 0.161f);
    static readonly Color CorVermelho = new Color(0.902f, 0.247f, 0.286f);
    static readonly Color CorTextoClaro = new Color(0.910f, 0.929f, 0.961f);
    static readonly Color CorTextoSuave = new Color(0.624f, 0.690f, 0.784f);
    static readonly Color CorBadge = new Color(0.078f, 0.110f, 0.161f);
    static readonly Color CorEscurecedor = new Color(0.016f, 0.020f, 0.035f, 0.55f);

    // Botão dourado com texto escuro: é o maior contraste possível sobre os painéis escuros.
    static readonly Color CorBotao = new Color(1f, 0.776f, 0.161f);
    static readonly Color CorBotaoClaro = new Color(1f, 0.859f, 0.420f);
    static readonly Color CorBotaoEscuro = new Color(0.816f, 0.600f, 0.078f);
    static readonly Color CorTextoBotao = new Color(0.063f, 0.075f, 0.110f);

    // Botão secundário: cinza-azulado, para não competir com a ação principal.
    static readonly Color CorBotao2 = new Color(0.157f, 0.196f, 0.267f);
    static readonly Color CorBotao2Claro = new Color(0.231f, 0.286f, 0.376f);
    static readonly Color CorBotao2Escuro = new Color(0.106f, 0.133f, 0.184f);

    [MenuItem("Jogo/Montar Telas Ilustradas (Tutorial, Vitória, Derrota)")]
    public static void MontarTelasIlustradas()
    {
        GerenciadorDeJogo gerenciador = Encontrar<GerenciadorDeJogo>();

        if (gerenciador == null)
        {
            EditorUtility.DisplayDialog("Telas Ilustradas",
                "Esta cena não tem um GerenciadorDeJogo.\n\n"
                + "Abra a cena de gameplay (Estadio_Futebol) ou rode antes "
                + "\"Jogo > Montar Telas na Cena de Gameplay\".", "OK");
            return;
        }

        Sprite spriteTutorial = CarregarSprite(CaminhoTutorial);
        Sprite spriteVitoria = CarregarSprite(CaminhoVitoria);
        Sprite spriteDerrota = CarregarSprite(CaminhoDerrota);

        if (spriteTutorial == null || spriteVitoria == null || spriteDerrota == null)
        {
            EditorUtility.DisplayDialog("Telas Ilustradas",
                "Não encontrei as imagens em Assets/MEU_PROJETO/Imagens/.\n\n"
                + "Esperado: aprende_ai_zeruela.png, vitoria_confetti.png e num_vai_da.png "
                + "importadas como Sprite (2D and UI).", "OK");
            return;
        }

        Transform canvasTelas = EncontrarCanvasDasTelas(gerenciador);

        if (canvasTelas == null)
        {
            EditorUtility.DisplayDialog("Telas Ilustradas",
                "Não encontrei a Canvas das telas (CanvasTelas).", "OK");
            return;
        }

        // As telas antigas somem; as referências são refeitas no final.
        ApagarTela(canvasTelas, "TelaDeTutorial");
        ApagarTela(canvasTelas, "TelaDeVitoria");
        ApagarTela(canvasTelas, "TelaDeGameOver");

        GameObject telaDeTutorial = MontarTelaDeTutorial(canvasTelas, spriteTutorial, gerenciador);

        GameObject telaDeVitoria = MontarTelaDeVitoria(canvasTelas, spriteVitoria, gerenciador,
            out TMP_Text descricaoVitoria, out TMP_Text pontuacaoVitoria, out TMP_Text rotuloProximaFase);

        GameObject telaDeGameOver = MontarTelaDeGameOver(canvasTelas, spriteDerrota, gerenciador,
            out TMP_Text motivoGameOver, out TMP_Text pontuacaoGameOver);

        // ---------- Referências no Inspector ----------
        Ligar(gerenciador, "telaDeTutorial", telaDeTutorial);
        Ligar(gerenciador, "telaDeVitoria", telaDeVitoria);
        Ligar(gerenciador, "telaDeGameOver", telaDeGameOver);
        Ligar(gerenciador, "textoDescricaoVitoria", descricaoVitoria);
        Ligar(gerenciador, "textoPontuacaoVitoria", pontuacaoVitoria);
        Ligar(gerenciador, "textoBotaoProximaFase", rotuloProximaFase);
        Ligar(gerenciador, "textoMotivoGameOver", motivoGameOver);
        Ligar(gerenciador, "textoPontuacaoGameOver", pontuacaoGameOver);

        string avisoAcoes = RelegarAcoesDoJogador(gerenciador);
        AvisarSobreTutorialNoPause(gerenciador);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = telaDeTutorial;

        EditorUtility.DisplayDialog("Telas Ilustradas",
            "Telas de Tutorial, Vitória e Game Over montadas e ligadas.\n\n"
            + avisoAcoes
            + AvisoEventSystem()
            + "Salve a cena (Ctrl+S).", "OK");
    }

    // ===================================================================
    //  TELA DE TUTORIAL
    // ===================================================================

    static GameObject MontarTelaDeTutorial(Transform canvas, Sprite personagem, GerenciadorDeJogo gerenciador)
    {
        GameObject tela = CriarTela(canvas, "TelaDeTutorial", CorFundoTutorial);

        // Personagem à esquerda, sangrando um pouco para fora da tela (é um recorte em PNG).
        CriarImagem(tela.transform, "Personagem", personagem, new Vector2(-620f, -10f), new Vector2(700f, 700f));

        // Painel dos comandos à direita.
        GameObject painel = CriarPainel(tela.transform, "PainelComandos", new Vector2(300f, 60f),
            new Vector2(1180f, 800f), CorPainel, CorOuro);

        CriarTexto(painel.transform, "Titulo", "COMO JOGAR", 64, CorOuro,
            new Vector2(0f, 306f), new Vector2(1100f, 90f), FontStyles.Bold);

        CriarTexto(painel.transform, "Subtitulo", "Aprende aí, zé ruela — esses são os comandos da partida.",
            26, CorTextoSuave, new Vector2(0f, 240f), new Vector2(1100f, 40f), FontStyles.Normal);

        CriarBarra(painel.transform, "Separador", new Vector2(0f, 208f), new Vector2(1060f, 2f),
            new Color(CorOuro.r, CorOuro.g, CorOuro.b, 0.30f));

        // Duas colunas: à esquerda o que move o jogador, à direita o que ele faz em combate.
        const float LarguraColuna = 550f;
        const float Passo = 82f;
        const float PrimeiraLinha = 140f;

        string[,] coluna1 =
        {
            { "W A S D", "Mover o jogador" },
            { "SHIFT", "Correr" },
            { "ESPAÇO", "Pular" },
            { "MOUSE", "Olhar ao redor" },
            { "ESC", "Pausar o jogo" }
        };

        string[,] coluna2 =
        {
            { "CLIQUE ESQ.", "Atirar" },
            { "R", "Recarregar a arma" },
            { "E", "Usar estações de cura e munição" },
            { "1  2  3", "Comprar upgrades com pontos" },
            { "T", "Reabrir esta tela" }
        };

        CriarColunaDeComandos(painel.transform, coluna1, -288f, PrimeiraLinha, Passo, LarguraColuna);
        CriarColunaDeComandos(painel.transform, coluna2, 288f, PrimeiraLinha, Passo, LarguraColuna);

        CriarBarra(painel.transform, "Separador2", new Vector2(0f, -240f), new Vector2(1060f, 2f),
            new Color(CorOuro.r, CorOuro.g, CorOuro.b, 0.30f));

        CriarTexto(painel.transform, "Objetivo",
            "<b><color=#FFC629>OBJETIVO</color></b>   Elimine todos os inimigos da orda antes do cronômetro zerar.\n"
            + "Cada inimigo abatido rende pontos para gastar em cura, dano e capacidade do pente.",
            25, CorTextoClaro, new Vector2(0f, -292f), new Vector2(1100f, 70f), FontStyles.Normal);

        CriarBotao(tela.transform, "BotaoComecar", "COMEÇAR A PARTIDA", new Vector2(300f, -452f),
            new Vector2(460f, 82f), CorBotao, CorBotaoClaro, CorBotaoEscuro, CorTextoBotao,
            gerenciador.FecharTutorial);

        return tela;
    }

    static void CriarColunaDeComandos(Transform pai, string[,] comandos, float x, float primeiraLinha,
        float passo, float largura)
    {
        for (int i = 0; i < comandos.GetLength(0); i++)
        {
            CriarComando(pai, new Vector2(x, primeiraLinha - passo * i), largura,
                comandos[i, 0], comandos[i, 1]);
        }
    }

    // Uma linha "tecla + descrição". A tecla fica numa plaquinha com borda dourada.
    static void CriarComando(Transform pai, Vector2 posicao, float largura, string tecla, string descricao)
    {
        const float Altura = 56f;
        const float LarguraBadge = 168f;
        const float Borda = 3f;

        GameObject linha = CriarObjetoUI("Comando_" + descricao, pai);
        RectTransform rectLinha = linha.GetComponent<RectTransform>();
        rectLinha.anchoredPosition = posicao;
        rectLinha.sizeDelta = new Vector2(largura, Altura);

        // Borda: a imagem de fora é dourada, a de dentro é escura e menor.
        GameObject borda = CriarObjetoUI("Tecla", linha.transform);
        RectTransform rectBorda = borda.GetComponent<RectTransform>();
        rectBorda.anchorMin = new Vector2(0f, 0.5f);
        rectBorda.anchorMax = new Vector2(0f, 0.5f);
        rectBorda.pivot = new Vector2(0f, 0.5f);
        rectBorda.anchoredPosition = Vector2.zero;
        rectBorda.sizeDelta = new Vector2(LarguraBadge, Altura);
        borda.AddComponent<Image>().color = new Color(CorOuro.r, CorOuro.g, CorOuro.b, 0.55f);

        GameObject fundo = CriarObjetoUI("Fundo", borda.transform);
        RectTransform rectFundo = fundo.GetComponent<RectTransform>();
        Esticar(rectFundo);
        rectFundo.offsetMin = new Vector2(Borda, Borda);
        rectFundo.offsetMax = new Vector2(-Borda, -Borda);
        fundo.AddComponent<Image>().color = CorBadge;

        TMP_Text textoTecla = CriarTexto(fundo.transform, "Texto", tecla, 26, CorOuro,
            Vector2.zero, Vector2.zero, FontStyles.Bold);
        Esticar(textoTecla.rectTransform);

        // Respiro interno para "CLIQUE ESQ." não encostar na borda da plaquinha.
        textoTecla.rectTransform.offsetMin = new Vector2(10f, 0f);
        textoTecla.rectTransform.offsetMax = new Vector2(-10f, 0f);

        // "CLIQUE ESQ." é bem mais largo que "R": o auto-size resolve sem apertar as outras.
        textoTecla.enableAutoSizing = true;
        textoTecla.fontSizeMin = 14f;
        textoTecla.fontSizeMax = 26f;
        textoTecla.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text textoDescricao = CriarTexto(linha.transform, "Descricao", descricao, 26, CorTextoClaro,
            Vector2.zero, Vector2.zero, FontStyles.Normal);
        RectTransform rectDescricao = textoDescricao.rectTransform;
        rectDescricao.anchorMin = new Vector2(0f, 0.5f);
        rectDescricao.anchorMax = new Vector2(0f, 0.5f);
        rectDescricao.pivot = new Vector2(0f, 0.5f);
        rectDescricao.anchoredPosition = new Vector2(LarguraBadge + 16f, 0f);
        rectDescricao.sizeDelta = new Vector2(largura - LarguraBadge - 16f, Altura);
        textoDescricao.alignment = TextAlignmentOptions.Left;

        // Sem isso as descrições mais longas vazam para fora do painel.
        textoDescricao.textWrappingMode = TextWrappingModes.NoWrap;
        textoDescricao.enableAutoSizing = true;
        textoDescricao.fontSizeMin = 19f;
        textoDescricao.fontSizeMax = 26f;
    }

    // ===================================================================
    //  TELA DE VITÓRIA
    // ===================================================================

    static GameObject MontarTelaDeVitoria(Transform canvas, Sprite comemoracao, GerenciadorDeJogo gerenciador,
        out TMP_Text descricao, out TMP_Text pontuacao, out TMP_Text rotuloProximaFase)
    {
        GameObject tela = CriarTela(canvas, "TelaDeVitoria", Color.black);

        // A foto cobre a tela inteira sem distorcer (equivale ao "cover" do CSS).
        // O tamanho é calculado na mão em vez de usar AspectRatioFitter porque o fitter
        // assume o controle do anchoredPosition, e aqui é justamente o deslocamento
        // vertical que decide se o rosto do personagem aparece ou fica cortado.
        float proporcao = comemoracao.rect.width / comemoracao.rect.height;
        float largura = Mathf.Max(LarguraDaTela, AlturaDaTela * proporcao);
        float altura = largura / proporcao;

        GameObject fundo = CriarObjetoUI("FundoComemoracao", tela.transform);
        RectTransform rectFundo = fundo.GetComponent<RectTransform>();
        rectFundo.anchorMin = new Vector2(0.5f, 0.5f);
        rectFundo.anchorMax = new Vector2(0.5f, 0.5f);
        rectFundo.pivot = new Vector2(0.5f, 0.5f);
        rectFundo.sizeDelta = new Vector2(largura, altura);

        // A imagem é quadrada e o rosto está no topo dela; a faixa 16:9 visível pega só
        // o meio. Descer a imagem traz o rosto para dentro da área visível, logo acima
        // do painel de texto.
        rectFundo.anchoredPosition = new Vector2(0f, -380f);

        Image imagemFundo = fundo.AddComponent<Image>();
        imagemFundo.sprite = comemoracao;
        imagemFundo.raycastTarget = false;

        // Escurecedor sobre a foto: sem ele o confete branco engole o texto.
        GameObject escurecedor = CriarObjetoUI("Escurecedor", tela.transform);
        Esticar(escurecedor.GetComponent<RectTransform>());
        Image imagemEscurecedor = escurecedor.AddComponent<Image>();
        imagemEscurecedor.color = CorEscurecedor;
        imagemEscurecedor.raycastTarget = false;

        // Painel na parte de baixo: deixa o rosto do personagem à mostra em cima.
        GameObject painel = CriarPainel(tela.transform, "Caixa", new Vector2(0f, -205f),
            new Vector2(1020f, 520f), CorPainelSobreFoto, CorOuro);

        CriarTexto(painel.transform, "Titulo", "STAGE CLEAR", 78, CorOuro,
            new Vector2(0f, 185f), new Vector2(960f, 110f), FontStyles.Bold);

        descricao = CriarTexto(painel.transform, "Subtitulo", "Fase concluída!", 32, CorTextoClaro,
            new Vector2(0f, 92f), new Vector2(960f, 80f), FontStyles.Normal);

        pontuacao = CriarTexto(painel.transform, "Pontuacao", "PONTUAÇÃO FINAL: 0", 34, CorOuro,
            new Vector2(0f, 22f), new Vector2(960f, 50f), FontStyles.Bold);

        rotuloProximaFase = CriarBotao(painel.transform, "BotaoProximaFase", "Próxima Fase",
            new Vector2(0f, -70f), new Vector2(440f, 84f),
            CorBotao, CorBotaoClaro, CorBotaoEscuro, CorTextoBotao, gerenciador.ProximaFase);

        CriarBotao(painel.transform, "BotaoMenuPrincipal", "Menu Principal",
            new Vector2(0f, -168f), new Vector2(440f, 84f),
            CorBotao2, CorBotao2Claro, CorBotao2Escuro, CorTextoClaro, gerenciador.VoltarAoMenu);

        return tela;
    }

    // ===================================================================
    //  TELA DE GAME OVER
    // ===================================================================

    static GameObject MontarTelaDeGameOver(Transform canvas, Sprite personagem, GerenciadorDeJogo gerenciador,
        out TMP_Text motivo, out TMP_Text pontuacao)
    {
        GameObject tela = CriarTela(canvas, "TelaDeGameOver", CorFundoDerrota);

        // O recorte é mais alto que largo (1156x1360); manter a proporção evita o personagem achatado.
        float proporcao = personagem.rect.width / personagem.rect.height;
        float altura = 776f;

        CriarImagem(tela.transform, "Personagem", personagem, new Vector2(500f, -20f),
            new Vector2(altura * proporcao, altura));

        GameObject painel = CriarPainel(tela.transform, "Caixa", new Vector2(-440f, 0f),
            new Vector2(880f, 580f), CorPainelDerrota, CorVermelho);

        CriarTexto(painel.transform, "Titulo", "GAME OVER", 82, CorVermelho,
            new Vector2(0f, 195f), new Vector2(820f, 110f), FontStyles.Bold);

        // O texto real ("SUA VIDA CHEGOU A ZERO!" ou "O TEMPO ACABOU!") é escrito pelo
        // GerenciadorDeJogo.GameOver(); isto aqui só evita um buraco na tela no Editor.
        motivo = CriarTexto(painel.transform, "Subtitulo", "SUA VIDA CHEGOU A ZERO!", 32, CorTextoClaro,
            new Vector2(0f, 95f), new Vector2(820f, 80f), FontStyles.Normal);

        pontuacao = CriarTexto(painel.transform, "Pontuacao", "PONTUAÇÃO FINAL: 0", 32, CorOuro,
            new Vector2(0f, 25f), new Vector2(820f, 50f), FontStyles.Bold);

        CriarBotao(painel.transform, "BotaoReiniciar", "Tentar de Novo",
            new Vector2(0f, -70f), new Vector2(420f, 84f),
            CorBotao, CorBotaoClaro, CorBotaoEscuro, CorTextoBotao, gerenciador.ReiniciarFase);

        CriarBotao(painel.transform, "BotaoMenuPrincipal", "Menu Principal",
            new Vector2(0f, -168f), new Vector2(420f, 84f),
            CorBotao2, CorBotao2Claro, CorBotao2Escuro, CorTextoClaro, gerenciador.VoltarAoMenu);

        return tela;
    }

    // ===================================================================
    //  Funções auxiliares de criação de UI
    // ===================================================================

    // Fundo que cobre a tela inteira; também bloqueia cliques no que está atrás.
    static GameObject CriarTela(Transform pai, string nome, Color corDoFundo)
    {
        GameObject tela = CriarObjetoUI(nome, pai);
        Undo.RegisterCreatedObjectUndo(tela, "Montar Telas Ilustradas");

        Esticar(tela.GetComponent<RectTransform>());
        tela.AddComponent<Image>().color = corDoFundo;

        // As telas nascem escondidas; o GerenciadorDeJogo mostra a certa.
        tela.SetActive(false);
        return tela;
    }

    // Painel com uma barra de destaque colada no topo.
    static GameObject CriarPainel(Transform pai, string nome, Vector2 posicao, Vector2 tamanho,
        Color corDoPainel, Color corDaBarra)
    {
        GameObject painel = CriarObjetoUI(nome, pai);
        RectTransform rect = painel.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        painel.AddComponent<Image>().color = corDoPainel;

        CriarBarra(painel.transform, "BarraTopo", new Vector2(0f, tamanho.y * 0.5f - 4f),
            new Vector2(tamanho.x, 8f), corDaBarra);

        return painel;
    }

    static void CriarBarra(Transform pai, string nome, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        GameObject barra = CriarObjetoUI(nome, pai);
        RectTransform rect = barra.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        Image imagem = barra.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
    }

    static Image CriarImagem(Transform pai, string nome, Sprite sprite, Vector2 posicao, Vector2 tamanho)
    {
        GameObject objeto = CriarObjetoUI(nome, pai);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        Image imagem = objeto.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;

        return imagem;
    }

    static TMP_Text CriarBotao(Transform pai, string nome, string rotulo, Vector2 posicao, Vector2 tamanho,
        Color normal, Color destaque, Color pressionado, Color corDoTexto, UnityAction acao)
    {
        GameObject objetoBotao = CriarObjetoUI(nome, pai);
        RectTransform rect = objetoBotao.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        Image imagem = objetoBotao.AddComponent<Image>();
        Button botao = objetoBotao.AddComponent<Button>();
        botao.targetGraphic = imagem;

        ColorBlock cores = botao.colors;
        cores.normalColor = normal;
        cores.highlightedColor = destaque;
        cores.pressedColor = pressionado;
        cores.selectedColor = normal;
        botao.colors = cores;

        // Equivale a arrastar o GerenciadorDeJogo no OnClick() do botão e escolher o método.
        UnityEventTools.AddPersistentListener(botao.onClick, acao);

        TMP_Text texto = CriarTexto(objetoBotao.transform, "Texto", rotulo, 34, corDoTexto,
            Vector2.zero, Vector2.zero, FontStyles.Bold);
        Esticar(texto.rectTransform);

        return texto;
    }

    static TMP_Text CriarTexto(Transform pai, string nome, string conteudo, float tamanhoDaFonte, Color cor,
        Vector2 posicao, Vector2 tamanho, FontStyles estilo)
    {
        GameObject objeto = CriarObjetoUI(nome, pai);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanhoDaFonte;
        texto.color = cor;
        texto.fontStyle = estilo;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;

        return texto;
    }

    static GameObject CriarObjetoUI(string nome, Transform pai)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = LayerMask.NameToLayer("UI");
        objeto.transform.SetParent(pai, false);
        return objeto;
    }

    static void Esticar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // ===================================================================
    //  Funções auxiliares de cena e de referência
    // ===================================================================

    static Sprite CarregarSprite(string caminho)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
    }

    // A Canvas das telas é a mãe das telas que já existem; se nenhuma existir,
    // procura pelo nome criado por MontadorDeTelas.
    static Transform EncontrarCanvasDasTelas(GerenciadorDeJogo gerenciador)
    {
        string[] campos = { "telaDeVitoria", "telaDeGameOver", "telaDePause", "telaDeTutorial" };

        foreach (string campo in campos)
        {
            GameObject tela = Ler(gerenciador, campo) as GameObject;

            if (tela != null)
            {
                return tela.transform.parent;
            }
        }

        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.name == "CanvasTelas")
            {
                return canvas.transform;
            }
        }

        return null;
    }

    static void ApagarTela(Transform canvas, string nome)
    {
        Transform tela = canvas.Find(nome);

        if (tela != null)
        {
            Undo.DestroyObjectImmediate(tela.gameObject);
        }
    }

    // O GerenciadorDeJogo desliga esses scripts sempre que o estado não é "Jogando".
    // Sem isso o jogador continua atirando e comprando upgrades com o tutorial aberto.
    static string RelegarAcoesDoJogador(GerenciadorDeJogo gerenciador)
    {
        JogadorVida jogadorVida = Encontrar<JogadorVida>();

        if (jogadorVida == null)
        {
            return "ATENÇÃO: não encontrei o Jogador na cena, então \"acoesDoJogador\" não foi preenchido.\n\n";
        }

        GameObject jogador = jogadorVida.gameObject;
        List<Object> acoes = new List<Object>();
        acoes.AddRange(jogador.GetComponentsInChildren<Arma>(true));
        acoes.AddRange(jogador.GetComponentsInChildren<JogadorMovimento>(true));
        acoes.AddRange(jogador.GetComponentsInChildren<JogadorMovimentoCamera>(true));
        acoes.AddRange(jogador.GetComponentsInChildren<JogadorPulo>(true));
        acoes.AddRange(jogador.GetComponentsInChildren<JogadorInteracao>(true));
        acoes.AddRange(jogador.GetComponentsInChildren<LojaDeUpgrades>(true));

        LigarLista(gerenciador, "acoesDoJogador", acoes);

        return "Scripts do jogador religados em \"acoesDoJogador\": " + acoes.Count + ".\n\n";
    }

    // Deixa o tutorial descobrível: quem pausar lê que o T reabre os comandos.
    static void AvisarSobreTutorialNoPause(GerenciadorDeJogo gerenciador)
    {
        GameObject telaDePause = Ler(gerenciador, "telaDePause") as GameObject;

        if (telaDePause == null)
        {
            return;
        }

        Transform subtitulo = telaDePause.transform.Find("Caixa/Subtitulo");

        if (subtitulo == null)
        {
            return;
        }

        TMP_Text texto = subtitulo.GetComponent<TMP_Text>();

        if (texto != null)
        {
            texto.text = "Pressione ESC para continuar\n<size=80%>T para rever os comandos</size>";
            EditorUtility.SetDirty(texto);
        }
    }

    // Preenche um campo [SerializeField] no Inspector (funciona com campos private).
    static void Ligar(Object alvo, string campo, Object valor)
    {
        SerializedObject objetoSerializado = new SerializedObject(alvo);
        objetoSerializado.FindProperty(campo).objectReferenceValue = valor;
        objetoSerializado.ApplyModifiedProperties();
    }

    static void LigarLista(Object alvo, string campo, List<Object> valores)
    {
        SerializedObject objetoSerializado = new SerializedObject(alvo);
        SerializedProperty lista = objetoSerializado.FindProperty(campo);
        lista.arraySize = valores.Count;

        for (int i = 0; i < valores.Count; i++)
        {
            lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        }

        objetoSerializado.ApplyModifiedProperties();
    }

    static Object Ler(Object alvo, string campo)
    {
        SerializedProperty propriedade = new SerializedObject(alvo).FindProperty(campo);

        return propriedade != null ? propriedade.objectReferenceValue : null;
    }

    static T Encontrar<T>() where T : Component
    {
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            T componente = raiz.GetComponentInChildren<T>(true);

            if (componente != null)
            {
                return componente;
            }
        }

        return null;
    }

    static string AvisoEventSystem()
    {
        if (Encontrar<EventSystem>() != null)
        {
            return "";
        }

        return "ATENÇÃO: a cena não tem EventSystem, os botões não vão responder. "
            + "Crie um em GameObject > UI > Event System.\n\n";
    }
}

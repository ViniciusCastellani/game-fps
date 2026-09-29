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
// Monta na cena aberta as telas de Pause, Game Over e Vitória (com botões funcionando),
// o texto do cronômetro e o contador de FPS, e já liga todas as referências no Inspector.
public static class MontadorDeTelas
{
    [MenuItem("Jogo/Montar Telas na Cena de Gameplay")]
    static void MontarTelasDeGameplay()
    {
        if (Encontrar<GerenciadorDeJogo>() != null)
        {
            EditorUtility.DisplayDialog("Montar Telas", "Esta cena já tem um GerenciadorDeJogo. Nada foi criado.", "OK");
            return;
        }

        GerenciadorDeOrdas gerenciadorDeOrdas = Encontrar<GerenciadorDeOrdas>();
        JogadorVida jogadorVida = Encontrar<JogadorVida>();

        if (gerenciadorDeOrdas == null || jogadorVida == null)
        {
            EditorUtility.DisplayDialog("Montar Telas",
                "Abra a cena de gameplay (que tem GerenciadorDeOrdas e o Jogador com JogadorVida) antes de usar esta opção.", "OK");
            return;
        }

        // ---------- GerenciadorDeJogo + Cronometro + GerenciadorDeCenas ----------
        GameObject objetoGerenciador = new GameObject("GerenciadorDeJogo");
        Undo.RegisterCreatedObjectUndo(objetoGerenciador, "Montar Telas");

        GerenciadorDeJogo gerenciador = objetoGerenciador.AddComponent<GerenciadorDeJogo>();
        Cronometro cronometro = objetoGerenciador.AddComponent<Cronometro>();

        GerenciadorDeCenas gerenciadorDeCenas = objetoGerenciador.AddComponent<GerenciadorDeCenas>();
        gerenciadorDeCenas.nomeDaTelaInicial = "TelaInicial";

        AudioSource fonteDeAudio = objetoGerenciador.AddComponent<AudioSource>();
        fonteDeAudio.playOnAwake = false;

        // ---------- Telas ----------
        GameObject canvasTelas = CriarCanvas("CanvasTelas", 10);

        GameObject telaDePause = CriarTela(canvasTelas.transform, "TelaDePause", "JOGO PAUSADO", Color.white,
            "Pressione ESC para continuar", out TMP_Text _, out Transform caixaPause);
        CriarBotao(caixaPause, "BotaoContinuar", "Continuar", -60f, gerenciador.Continuar);
        CriarBotao(caixaPause, "BotaoSairParaMenu", "Sair para o Menu", -175f, gerenciador.VoltarAoMenu);

        GameObject telaDeGameOver = CriarTela(canvasTelas.transform, "TelaDeGameOver", "GAME OVER", new Color(0.95f, 0.25f, 0.25f),
            "", out TMP_Text textoMotivoGameOver, out Transform caixaGameOver);
        CriarBotao(caixaGameOver, "BotaoReiniciar", "Reiniciar", -60f, gerenciador.ReiniciarFase);
        CriarBotao(caixaGameOver, "BotaoMenuPrincipal", "Menu Principal", -175f, gerenciador.VoltarAoMenu);

        GameObject telaDeVitoria = CriarTela(canvasTelas.transform, "TelaDeVitoria", "STAGE CLEAR", new Color(1f, 0.8f, 0.2f),
            "Fase concluída!", out TMP_Text textoDescricaoVitoria, out Transform caixaVitoria);
        TMP_Text textoBotaoProximaFase = CriarBotao(caixaVitoria, "BotaoProximaFase", "Próxima Fase", -60f, gerenciador.ProximaFase);
        CriarBotao(caixaVitoria, "BotaoMenuPrincipal", "Menu Principal", -175f, gerenciador.VoltarAoMenu);

        // ---------- Texto do cronômetro (na Canvas da HUD que já existe) ----------
        Transform canvasDaHUD = canvasTelas.transform;
        HUDJogador hud = Encontrar<HUDJogador>();

        if (hud != null && hud.textoVida != null)
        {
            canvasDaHUD = hud.textoVida.GetComponentInParent<Canvas>().transform;
        }

        TMP_Text textoTempo = CriarTexto(canvasDaHUD, "TextTempo", "TEMPO: 00:00", 48, Color.white,
            Vector2.zero, new Vector2(500f, 70f), FontStyles.Bold);
        FixarNoCanto(textoTempo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f));

        // Se existe o texto da orda, usa a mesma âncora dele e fica logo abaixo da caixa,
        // assim os dois nunca se sobrepõem, qualquer que seja a proporção da tela.
        if (gerenciadorDeOrdas.textoOrda != null)
        {
            RectTransform orda = gerenciadorDeOrdas.textoOrda.rectTransform;
            RectTransform tempo = textoTempo.rectTransform;
            float baseDaCaixaDaOrda = orda.anchoredPosition.y - orda.sizeDelta.y * orda.pivot.y;

            tempo.anchorMin = orda.anchorMin;
            tempo.anchorMax = orda.anchorMax;
            tempo.pivot = new Vector2(0.5f, 1f);
            tempo.anchoredPosition = new Vector2(orda.anchoredPosition.x, baseDaCaixaDaOrda - 10f);
        }

        // ---------- Scripts do jogador que param em Pause/Game Over/Vitória ----------
        GameObject jogador = jogadorVida.gameObject;
        List<Object> acoesDoJogador = new List<Object>();
        acoesDoJogador.AddRange(jogador.GetComponentsInChildren<Arma>(true));
        acoesDoJogador.AddRange(jogador.GetComponentsInChildren<JogadorMovimento>(true));
        acoesDoJogador.AddRange(jogador.GetComponentsInChildren<JogadorMovimentoCamera>(true));
        acoesDoJogador.AddRange(jogador.GetComponentsInChildren<JogadorPulo>(true));
        acoesDoJogador.AddRange(jogador.GetComponentsInChildren<JogadorInteracao>(true));
        acoesDoJogador.AddRange(jogador.GetComponentsInChildren<LojaDeUpgrades>(true));

        // ---------- Referências no Inspector ----------
        Ligar(gerenciador, "telaDePause", telaDePause);
        Ligar(gerenciador, "telaDeGameOver", telaDeGameOver);
        Ligar(gerenciador, "telaDeVitoria", telaDeVitoria);
        Ligar(gerenciador, "textoMotivoGameOver", textoMotivoGameOver);
        Ligar(gerenciador, "textoDescricaoVitoria", textoDescricaoVitoria);
        Ligar(gerenciador, "textoBotaoProximaFase", textoBotaoProximaFase);
        Ligar(gerenciador, "gerenciadorDeOrdas", gerenciadorDeOrdas);
        Ligar(gerenciador, "gerenciadorDeCenas", gerenciadorDeCenas);
        Ligar(gerenciador, "fonteDeAudio", fonteDeAudio);
        LigarLista(gerenciador, "acoesDoJogador", acoesDoJogador);

        Ligar(cronometro, "textoTempo", textoTempo);
        Ligar(cronometro, "gerenciadorDeJogo", gerenciador);

        Ligar(gerenciadorDeOrdas, "gerenciadorDeJogo", gerenciador);
        Ligar(gerenciadorDeOrdas, "cronometro", cronometro);

        Ligar(jogadorVida, "gerenciadorDeJogo", gerenciador);

        if (Encontrar<ContadorFPS>() == null)
        {
            CriarContadorDeFPS();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = objetoGerenciador;

        EditorUtility.DisplayDialog("Montar Telas",
            "Telas de Pause, Game Over e Vitória, cronômetro e FPS criados e ligados.\n\n"
            + AvisoEventSystem()
            + "Salve a cena (Ctrl+S).", "OK");
    }

    [MenuItem("Jogo/Adicionar Contador de FPS na Cena")]
    static void AdicionarContadorDeFPS()
    {
        if (Encontrar<ContadorFPS>() != null)
        {
            EditorUtility.DisplayDialog("Contador de FPS", "Esta cena já tem um ContadorFPS. Nada foi criado.", "OK");
            return;
        }

        CriarContadorDeFPS();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Contador de FPS", "Contador de FPS criado. Salve a cena (Ctrl+S).", "OK");
    }

    static void CriarContadorDeFPS()
    {
        // Sort Order 100: fica por cima da HUD e das telas (CanvasTelas usa 10).
        GameObject canvasFPS = CriarCanvas("CanvasFPS", 100);

        TMP_Text textoFPS = CriarTexto(canvasFPS.transform, "TextFPS", "FPS: --", 32, new Color(0.3f, 1f, 0.3f),
            Vector2.zero, new Vector2(260f, 50f), FontStyles.Bold);
        textoFPS.alignment = TextAlignmentOptions.Right;
        FixarNoCanto(textoFPS.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -20f));

        ContadorFPS contador = canvasFPS.AddComponent<ContadorFPS>();
        Ligar(contador, "textoFPS", textoFPS);
    }

    // ================= Funções auxiliares de criação de UI =================

    static GameObject CriarCanvas(string nome, int ordem)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        objeto.layer = LayerMask.NameToLayer("UI");
        Undo.RegisterCreatedObjectUndo(objeto, "Montar Telas");

        Canvas canvas = objeto.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordem;

        CanvasScaler escala = objeto.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;

        return objeto;
    }

    static GameObject CriarTela(Transform pai, string nome, string titulo, Color corDoTitulo, string subtitulo,
        out TMP_Text textoSubtitulo, out Transform caixa)
    {
        // Fundo escuro que cobre a tela inteira (também bloqueia cliques no jogo).
        GameObject fundo = CriarObjetoUI(nome, pai);
        Esticar(fundo.GetComponent<RectTransform>());
        fundo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        GameObject objetoCaixa = CriarObjetoUI("Caixa", fundo.transform);
        objetoCaixa.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 560f);
        objetoCaixa.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.95f);

        CriarTexto(objetoCaixa.transform, "Titulo", titulo, 80, corDoTitulo,
            new Vector2(0f, 175f), new Vector2(720f, 110f), FontStyles.Bold);
        textoSubtitulo = CriarTexto(objetoCaixa.transform, "Subtitulo", subtitulo, 34, Color.white,
            new Vector2(0f, 70f), new Vector2(720f, 100f), FontStyles.Normal);

        caixa = objetoCaixa.transform;

        // As telas começam escondidas; o GerenciadorDeJogo mostra a certa.
        fundo.SetActive(false);
        return fundo;
    }

    static TMP_Text CriarBotao(Transform pai, string nome, string rotulo, float posicaoY, UnityAction acao)
    {
        GameObject objetoBotao = CriarObjetoUI(nome, pai);
        RectTransform rect = objetoBotao.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, posicaoY);
        rect.sizeDelta = new Vector2(440f, 90f);

        Image imagem = objetoBotao.AddComponent<Image>();
        Button botao = objetoBotao.AddComponent<Button>();
        botao.targetGraphic = imagem;

        ColorBlock cores = botao.colors;
        cores.normalColor = new Color(0.2f, 0.45f, 0.85f);
        cores.highlightedColor = new Color(0.35f, 0.6f, 1f);
        cores.pressedColor = new Color(0.12f, 0.3f, 0.6f);
        cores.selectedColor = cores.normalColor;
        botao.colors = cores;

        // Equivale a arrastar o GerenciadorDeJogo no OnClick() do botão e escolher o método.
        UnityEventTools.AddPersistentListener(botao.onClick, acao);

        TMP_Text texto = CriarTexto(objetoBotao.transform, "Texto", rotulo, 38, Color.white,
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

    static void FixarNoCanto(RectTransform rect, Vector2 canto, Vector2 deslocamento)
    {
        rect.anchorMin = canto;
        rect.anchorMax = canto;
        rect.pivot = canto;
        rect.anchoredPosition = deslocamento;
    }

    // ================= Funções auxiliares de referência =================

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

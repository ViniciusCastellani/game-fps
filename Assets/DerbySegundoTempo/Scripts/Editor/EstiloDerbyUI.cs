using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEditor.Events;
using Object = UnityEngine.Object;

public static class EstiloDerbyUI
{
    public static readonly Color CorPainel = new Color(0.055f, 0.078f, 0.125f, 0.96f);
    public static readonly Color CorPainelDerrota = new Color(0.078f, 0.039f, 0.051f, 0.94f);
    public static readonly Color CorFundoDerrota = new Color(0.039f, 0.020f, 0.027f, 0.97f);
    public static readonly Color CorFundoMenu = new Color(0.016f, 0.020f, 0.035f, 0.86f);
    public static readonly Color CorOuro = new Color(1f, 0.776f, 0.161f);
    public static readonly Color CorVermelho = new Color(0.902f, 0.247f, 0.286f);
    public static readonly Color CorVermelhoClaro = new Color(1f, 0.42f, 0.45f);
    public static readonly Color CorVermelhoEscuro = new Color(0.70f, 0.16f, 0.20f);
    public static readonly Color CorTextoClaro = new Color(0.910f, 0.929f, 0.961f);
    public static readonly Color CorTextoSuave = new Color(0.624f, 0.690f, 0.784f);
    public static readonly Color CorTextoBotao = new Color(0.063f, 0.075f, 0.110f);

    public static readonly Color CorBotao = new Color(1f, 0.776f, 0.161f);
    public static readonly Color CorBotaoClaro = new Color(1f, 0.859f, 0.420f);
    public static readonly Color CorBotaoEscuro = new Color(0.816f, 0.600f, 0.078f);

    public static readonly Color CorBotao2 = new Color(0.157f, 0.196f, 0.267f);
    public static readonly Color CorBotao2Claro = new Color(0.231f, 0.286f, 0.376f);
    public static readonly Color CorBotao2Escuro = new Color(0.106f, 0.133f, 0.184f);

    public static GameObject CriarObjetoUI(string nome, Transform pai)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = LayerMask.NameToLayer("UI");
        objeto.transform.SetParent(pai, false);
        return objeto;
    }

    public static void Esticar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static GameObject CriarTela(Transform pai, string nome, Color corDoFundo, bool ativo = false)
    {
        GameObject tela = CriarObjetoUI(nome, pai);

        Esticar(tela.GetComponent<RectTransform>());
        tela.AddComponent<Image>().color = corDoFundo;

        tela.SetActive(ativo);
        return tela;
    }

    public static GameObject CriarPainel(Transform pai, string nome, Vector2 posicao, Vector2 tamanho,
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

    public static void CriarBarra(Transform pai, string nome, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        GameObject barra = CriarObjetoUI(nome, pai);
        RectTransform rect = barra.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        Image imagem = barra.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
    }

    public static Image CriarImagem(Transform pai, string nome, Sprite sprite, Vector2 posicao, Vector2 tamanho,
        bool preservarProporcao = true)
    {
        GameObject objeto = CriarObjetoUI(nome, pai);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        Image imagem = objeto.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.preserveAspect = preservarProporcao;
        imagem.raycastTarget = false;

        return imagem;
    }

    public static TMP_Text CriarTexto(Transform pai, string nome, string conteudo, float tamanhoDaFonte, Color cor,
        Vector2 posicao, Vector2 tamanho, FontStyles estilo,
        TextAlignmentOptions alinhamento = TextAlignmentOptions.Center, bool quebraDeLinha = false)
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
        texto.alignment = alinhamento;
        texto.textWrappingMode = quebraDeLinha ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        texto.raycastTarget = false;

        return texto;
    }

    public static Button CriarBotao(Transform pai, string nome, string rotulo, Vector2 posicao, Vector2 tamanho,
        Color normal, Color destaque, Color pressionado, Color corDoTexto, UnityAction acao, int fonte = 34)
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
        cores.selectedColor = destaque;
        botao.colors = cores;

        UnityEventTools.AddPersistentListener(botao.onClick, acao);

        TMP_Text texto = CriarTexto(objetoBotao.transform, "Texto", rotulo, fonte, corDoTexto,
            Vector2.zero, Vector2.zero, FontStyles.Bold);
        Esticar(texto.rectTransform);

        AdicionarRealce(objetoBotao, CriarIndicadorDeFoco(objetoBotao.transform, CorTextoClaro), 1.04f);

        return botao;
    }

    public static GameObject CriarIndicadorDeFoco(Transform alvo, Color cor, float espessura = 5f, float margem = 8f)
    {
        GameObject foco = CriarObjetoUI("Foco", alvo);
        RectTransform rect = foco.GetComponent<RectTransform>();
        Esticar(rect);
        rect.offsetMin = new Vector2(-margem, -margem);
        rect.offsetMax = new Vector2(margem, margem);

        CriarBarraDeFoco(foco.transform, "Topo", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, espessura), cor);
        CriarBarraDeFoco(foco.transform, "Base", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, espessura), cor);
        CriarBarraDeFoco(foco.transform, "Esquerda", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(espessura, 0f), cor);
        CriarBarraDeFoco(foco.transform, "Direita", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), new Vector2(espessura, 0f), cor);

        foco.SetActive(false);
        return foco;
    }

    static void CriarBarraDeFoco(Transform pai, string nome, Vector2 ancoraMinima, Vector2 ancoraMaxima,
        Vector2 pivo, Vector2 tamanho, Color cor)
    {
        GameObject barra = CriarObjetoUI(nome, pai);
        RectTransform rect = barra.GetComponent<RectTransform>();
        rect.anchorMin = ancoraMinima;
        rect.anchorMax = ancoraMaxima;
        rect.pivot = pivo;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = tamanho;

        Image imagem = barra.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
    }

    public static RealceDeSelecao AdicionarRealce(GameObject alvo, GameObject indicadorDeFoco, float escalaEmFoco)
    {
        RealceDeSelecao realce = alvo.GetComponent<RealceDeSelecao>();
        if (realce == null) realce = alvo.AddComponent<RealceDeSelecao>();

        LigarCampo(realce, "indicadorDeFoco", indicadorDeFoco);

        SerializedObject objetoSerializado = new SerializedObject(realce);
        SerializedProperty escala = objetoSerializado.FindProperty("escalaEmFoco");
        if (escala == null) throw new InvalidOperationException("Campo ausente: RealceDeSelecao.escalaEmFoco");
        escala.floatValue = escalaEmFoco;
        objetoSerializado.ApplyModifiedPropertiesWithoutUndo();

        return realce;
    }

    public static void DefinirNavegacao(Selectable selecionavel, Selectable cima, Selectable baixo,
        Selectable esquerda, Selectable direita)
    {
        Navigation navegacao = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = cima,
            selectOnDown = baixo,
            selectOnLeft = esquerda,
            selectOnRight = direita
        };

        selecionavel.navigation = navegacao;
        EditorUtility.SetDirty(selecionavel);
    }

    public static void LigarCampo(Object alvo, string campo, Object valor)
    {
        SerializedObject objetoSerializado = new SerializedObject(alvo);
        SerializedProperty propriedade = objetoSerializado.FindProperty(campo);
        if (propriedade == null)
            throw new InvalidOperationException("Campo ausente: " + alvo.GetType().Name + "." + campo);

        propriedade.objectReferenceValue = valor;
        objetoSerializado.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void LigarLista(Object alvo, string campo, IList<Object> valores)
    {
        SerializedObject objetoSerializado = new SerializedObject(alvo);
        SerializedProperty lista = objetoSerializado.FindProperty(campo);
        if (lista == null || !lista.isArray)
            throw new InvalidOperationException("Lista ausente: " + alvo.GetType().Name + "." + campo);

        lista.arraySize = valores.Count;
        for (int i = 0; i < valores.Count; i++)
        {
            lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        }

        objetoSerializado.ApplyModifiedPropertiesWithoutUndo();
    }

    public static Sprite ImportarCartao(string origemRelativaAoProjeto, string destinoAsset)
    {
        string raizDoProjeto = Directory.GetParent(Application.dataPath).FullName;
        string destinoNoDisco = Path.Combine(raizDoProjeto, destinoAsset);

        if (!File.Exists(destinoNoDisco))
        {
            string origemNoDisco = Path.Combine(raizDoProjeto, origemRelativaAoProjeto);
            if (!File.Exists(origemNoDisco))
                throw new FileNotFoundException("Imagem de origem ausente: " + origemRelativaAoProjeto, origemNoDisco);

            Directory.CreateDirectory(Path.GetDirectoryName(destinoNoDisco));
            File.Copy(origemNoDisco, destinoNoDisco);
        }

        AssetDatabase.ImportAsset(destinoAsset, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importador = AssetImporter.GetAtPath(destinoAsset) as TextureImporter;
        if (importador == null) throw new InvalidOperationException("Importador de textura ausente: " + destinoAsset);

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.mipmapEnabled = false;
        importador.maxTextureSize = 2048;
        importador.textureCompression = TextureImporterCompression.Compressed;
        importador.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(destinoAsset);
        if (sprite == null) throw new InvalidOperationException("Sprite nao carregado: " + destinoAsset);

        return sprite;
    }
}

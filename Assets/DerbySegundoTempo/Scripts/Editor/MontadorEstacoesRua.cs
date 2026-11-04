using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class MontadorEstacoesRua
{
    const string Raiz = "Assets/DerbySegundoTempo";
    const string Cena = Raiz + "/Cenas/Fases/Rua_TPS.unity";
    const string Prefabs = Raiz + "/Prefabs/Estacoes/Rua";
    const string Pasta = "Library/EstacoesRua";
    static double proximaVerificacao;
    static TestRunnerApi runner;

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        runner = ScriptableObject.CreateInstance<TestRunnerApi>();
        runner.RegisterCallbacks(new Resultados());
        EditorApplication.update += Verificar;
    }

    static void Verificar()
    {
        if (EditorApplication.timeSinceStartup < proximaVerificacao) return;
        proximaVerificacao = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(Pasta + ".request"))
        {
            File.Delete(Pasta + ".request");
            try { Montar(); }
            catch (Exception erro) { Directory.CreateDirectory(Pasta); File.WriteAllText(Pasta + "/erro.txt", erro.ToString()); Debug.LogException(erro); }
            return;
        }
        if (File.Exists(Pasta + ".testar") && File.Exists(Pasta + "/status.txt"))
        { File.Delete(Pasta + ".testar"); Testar(); }
    }

    [MenuItem("Derby/Rua TPS/Montar oficinas e estacoes urbanas")]
    public static void Montar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Monte fora do Play Mode.");
        Directory.CreateDirectory(Pasta);
        File.Copy(Cena, Pasta + "/RuaAntesDasEstacoes.unity", true);
        CriarPasta(Prefabs);
        var referencia = EditorSceneManager.OpenPreviewScene(Raiz + "/Cenas/Fases/Estadio_TPS_Backup_Visual.unity");
        try
        {
            CriarPrefab("OficinaDerby", "EstacaoUpgrades", Encontrar<EstacaoDeUpgrade>(referencia).First());
            CriarPrefab("FarmaciaBairro", "EstacaoCura", Encontrar<EstacaoDeCura>(referencia).First());
            CriarPrefab("DepositoRecarga", "EstacaoMunicao", Encontrar<EstacaoDeMunicao>(referencia).First());
        }
        finally { EditorSceneManager.ClosePreviewScene(referencia); }
        var cena = SceneManager.GetSceneByPath(Cena);
        bool abriu = !cena.isLoaded;
        if (abriu) cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Additive);
        try
        {
            var grupo = cena.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Single(x => x.name == "Estacoes_Rua");
            // A cena pode conter modelos de referencia do estadio com estacoes embutidas.
            // Desativa somente esses servicos antigos na rua, preservando os modelos.
            var antigos = Encontrar<EstacaoDeUpgrade>(cena).Cast<Component>()
                .Concat(Encontrar<EstacaoDeCura>(cena)).Concat(Encontrar<EstacaoDeMunicao>(cena))
                .Where(x => !x.transform.IsChildOf(grupo)).Select(x => x.gameObject).Distinct().ToArray();
            foreach (var antigo in antigos)
            {
                antigo.SetActive(false);
                if (PrefabUtility.IsPartOfPrefabInstance(antigo)) PrefabUtility.RecordPrefabInstancePropertyModifications(antigo);
            }
            // Os objetos antigos eram apenas as pequenas malhas dos prefabs do estadio.
            foreach (Transform antigo in grupo.Cast<Transform>().ToArray()) Object.DestroyImmediate(antigo.gameObject);
            Instanciar(cena, grupo, "OficinaDerby", "OficinaDerby_Entrada", new Vector3(-15.25f, .2f, -11.5f), -90);
            Instanciar(cena, grupo, "OficinaDerby", "OficinaDerby_Apoio", new Vector3(-15.25f, .2f, 26), -90);
            Instanciar(cena, grupo, "FarmaciaBairro", "Farmacia_DePlantao", new Vector3(15.25f, .2f, 13.5f), 90);
            Instanciar(cena, grupo, "DepositoRecarga", "Deposito_Recarga", new Vector3(15.25f, .2f, -11.5f), 90);
            foreach (var loja in Encontrar<LojaDeUpgrades>(cena))
            {
                loja.enabled = false;
                EditorUtility.SetDirty(loja);
            }
            var jogo = new SerializedObject(Encontrar<GerenciadorDeJogo>(cena).Single());
            var acoes = jogo.FindProperty("acoesDoJogador");
            var manter = Enumerable.Range(0, acoes.arraySize).Select(i => acoes.GetArrayElementAtIndex(i).objectReferenceValue)
                .Where(x => !(x is LojaDeUpgrades)).ToArray();
            acoes.arraySize = manter.Length;
            for (int i = 0; i < manter.Length; i++) acoes.GetArrayElementAtIndex(i).objectReferenceValue = manter[i];
            jogo.ApplyModifiedPropertiesWithoutUndo();
            var linhaTutorial = cena.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(x => x.name == "Comando_Comprar upgrades com pontos");
            if (linhaTutorial != null)
                foreach (var texto in linhaTutorial.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (texto.name == "Texto") texto.text = "1  2";
                    if (texto.name == "Descricao") texto.text = "Oficina: dano / pente (por pontos)";
                }
            AtualizarLetreiro(cena, "Edificio_Oeste_02", "OFICINA");
            AtualizarLetreiro(cena, "Edificio_Oeste_05", "OFICINA");
            AtualizarLetreiro(cena, "Edificio_Leste_04", "FARMACIA");
            AtualizarLetreiro(cena, "Edificio_Leste_02", "RECARGA");
            foreach (var estacao in grupo.GetComponentsInChildren<EstacaoDeUpgrade>(true)) Validar(estacao, estacao.painelDeTexto, estacao.textoFlutuante);
            foreach (var estacao in grupo.GetComponentsInChildren<EstacaoDeCura>(true)) Validar(estacao, estacao.painelDeTexto, estacao.textoFlutuante);
            foreach (var estacao in grupo.GetComponentsInChildren<EstacaoDeMunicao>(true)) Validar(estacao, estacao.painelDeTexto, estacao.textoFlutuante);
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Pasta + "/status.txt", "Instaladas: 2 oficinas, farmacia e deposito de recarga.\nPrefabs variantes dos originais; mesmos componentes, precos, teclas e sons do estadio.\nGatilhos frontais, colisao fisica e carving de navegacao configurados.\nLoja rapida global desativada somente na rua para evitar compras duplicadas nas teclas 1 e 2.\n");
            File.Delete(Pasta + "/erro.txt");
        }
        finally { if (abriu) EditorSceneManager.CloseScene(cena, true); }
        Capturar();
        File.WriteAllText(Pasta + ".testar", "");
    }

    static System.Collections.Generic.IEnumerable<T> Encontrar<T>(Scene cena) where T : Component =>
        cena.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true));

    static void CriarPrefab(string nome, string baseNome, Component referencia)
    {
        var cena = EditorSceneManager.NewPreviewScene();
        try
        {
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Prefabs/Estacoes/" + baseNome + ".prefab");
            var objeto = (GameObject)PrefabUtility.InstantiatePrefab(original, cena);
            objeto.name = nome;
            objeto.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            objeto.transform.localScale = Vector3.one;
            foreach (var renderer in objeto.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var animator in objeto.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var collider in objeto.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var componente = objeto.GetComponent(referencia.GetType());
            // Copia somente as regras do jogo; campos internos do Unity e a UI pertencem a nova instancia.
            var dados = new SerializedObject(componente);
            var origem = new SerializedObject(referencia);
            string[] campos = referencia is EstacaoDeUpgrade
                ? new[] { "aumentoDeDano", "custoDoDano", "teclaDano", "aumentoDeCapacidade", "custoDaCapacidade", "teclaCapacidade", "somSemPontos", "somDeCompra" }
                : new[] { referencia is EstacaoDeCura ? "quantidadeDeCura" : "quantidadeDeMunicao", "custoEmPontos", "teclaDeUso", "somSemPontos", "somDeCompra" };
            foreach (string campo in campos) dados.CopyFromSerializedProperty(origem.FindProperty(campo));
            dados.ApplyModifiedPropertiesWithoutUndo();
            var painel = dados.FindProperty("painelDeTexto").objectReferenceValue;
            var texto = dados.FindProperty("textoFlutuante").objectReferenceValue;
            var ui = ((GameObject)painel).GetComponent<RectTransform>();
            ui.localPosition = new Vector3(0, 2.3f, -.85f);
            ui.localRotation = Quaternion.identity;
            ui.localScale = Vector3.one * .0065f;
            ui.sizeDelta = new Vector2(440, 115);
            var mensagem = (TMP_Text)texto;
            mensagem.fontSize = 26;
            mensagem.enableAutoSizing = false;
            mensagem.color = Color.white;
            mensagem.raycastTarget = false;
            mensagem.margin = new Vector4(12, 8, 12, 8);
            mensagem.rectTransform.anchorMin = Vector2.zero;
            mensagem.rectTransform.anchorMax = Vector2.one;
            mensagem.rectTransform.offsetMin = Vector2.zero;
            mensagem.rectTransform.offsetMax = Vector2.zero;
            if (((GameObject)painel).GetComponent<UnityEngine.UI.Image>() == null)
            {
                var fundo = ((GameObject)painel).AddComponent<UnityEngine.UI.Image>();
                fundo.color = new Color(.025f, .045f, .055f, .96f);
                fundo.raycastTarget = false;
            }
            ((GameObject)painel).SetActive(false);
            var zona = objeto.AddComponent<BoxCollider>();
            zona.isTrigger = true;
            zona.center = new Vector3(0, 1.3f, -1.45f);
            zona.size = new Vector3(3.2f, 2.6f, 2.1f);
            var visual = Grupo(objeto.transform, "VisualUrbano");
            if (referencia is EstacaoDeUpgrade upgrade) Oficina(visual, upgrade);
            else if (referencia is EstacaoDeCura cura) Farmacia(visual, cura);
            else Recarga(visual, (EstacaoDeMunicao)referencia);
            var corpo = Cubo(visual, "Colisao_Estacao", new Vector3(0, .72f, .12f), new Vector3(3.5f, 1.44f, 1.35f), null);
            corpo.GetComponent<Renderer>().enabled = false;
            var obstaculo = corpo.AddComponent<NavMeshObstacle>();
            obstaculo.shape = NavMeshObstacleShape.Box;
            obstaculo.size = Vector3.one;
            obstaculo.carving = true;
            obstaculo.carveOnlyStationary = true;
            foreach (var item in objeto.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(item.gameObject) == null) continue;
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
                foreach (var comp in item.GetComponents<Component>())
                    if (comp != null && PrefabUtility.GetCorrespondingObjectFromSource(comp) != null)
                        PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
            }
            var salvo = PrefabUtility.SaveAsPrefabAsset(objeto, Prefabs + "/" + nome + ".prefab");
            if (PrefabUtility.GetPrefabAssetType(salvo) != PrefabAssetType.Variant)
                throw new InvalidOperationException("O asset precisa herdar o prefab original: " + nome);
        }
        finally { EditorSceneManager.ClosePreviewScene(cena); }
    }

    static void Oficina(Transform raiz, EstacaoDeUpgrade regras)
    {
        var azul = Mat("AzulOficina", new Color(.10f, .23f, .26f));
        var laranja = Mat("LaranjaOficina", new Color(.93f, .34f, .08f));
        var aco = Mat("AcoEscuro", new Color(.09f, .12f, .14f));
        var madeira = Mat("MadeiraBancada", new Color(.48f, .29f, .13f));
        var claro = Mat("MarfimEstacoes", new Color(.91f, .87f, .74f));
        Cubo(raiz, "TampoDeMadeira", new Vector3(0, 1.03f, 0), new Vector3(3.5f, .18f, 1.4f), madeira);
        foreach (float x in new[] { -1.5f, 1.5f }) foreach (float z in new[] { -.45f, .45f })
            Cubo(raiz, "PeBancada", new Vector3(x, .5f, z), new Vector3(.12f, 1, .12f), aco);
        Cubo(raiz, "Gaveteiro", new Vector3(.82f, .54f, .05f), new Vector3(1.35f, .9f, 1.08f), azul);
        for (int i = 0; i < 4; i++)
        {
            Cubo(raiz, "FrenteGaveta_" + i, new Vector3(.82f, .25f + i * .21f, -.505f), new Vector3(1.22f, .16f, .025f), laranja);
            Cubo(raiz, "Puxador_" + i, new Vector3(.82f, .26f + i * .21f, -.54f), new Vector3(.32f, .035f, .05f), aco);
        }
        Cubo(raiz, "PainelFerramentas", new Vector3(0, 1.88f, .53f), new Vector3(3.4f, 1.48f, .12f), azul);
        for (int coluna = 0; coluna < 12; coluna++) for (int linha = 0; linha < 4; linha++)
            Cubo(raiz, "FuroPainel", new Vector3(-1.53f + coluna * .28f, 1.38f + linha * .31f, .455f), new Vector3(.035f, .035f, .012f), aco);
        for (int i = 0; i < 3; i++)
        {
            var ferramenta = Grupo(raiz, "ChaveDeBoca_" + (i + 1));
            ferramenta.localPosition = new Vector3(-.95f + i * .65f, 1.88f, .38f);
            ferramenta.localRotation = Quaternion.Euler(0, 0, -20 + i * 20);
            Cubo(ferramenta, "Cabo", Vector3.zero, new Vector3(.10f, .65f, .055f), claro);
            Cubo(ferramenta, "Cabeca", new Vector3(0, .32f, 0), new Vector3(.3f, .14f, .055f), claro);
            Cubo(ferramenta, "GarraEsquerda", new Vector3(-.11f, .43f, 0), new Vector3(.08f, .17f, .055f), claro);
            Cubo(ferramenta, "GarraDireita", new Vector3(.11f, .43f, 0), new Vector3(.08f, .17f, .055f), claro);
        }
        Cubo(raiz, "MaletaFerramentas", new Vector3(-1.0f, 1.29f, -.18f), new Vector3(.72f, .32f, .42f), laranja);
        Cubo(raiz, "AlcaMaleta", new Vector3(-1.0f, 1.5f, -.18f), new Vector3(.32f, .09f, .08f), aco);
        Cubo(raiz, "ArmaEmManutencao", new Vector3(.13f, 1.23f, -.2f), new Vector3(.85f, .16f, .24f), aco);
        Cubo(raiz, "CelulaDaArma", new Vector3(.13f, 1.33f, -.2f), new Vector3(.36f, .05f, .16f), Mat("LuzCiano", new Color(.1f, .8f, .95f), true));
        Placa(raiz, "OFICINA DO DERBY", "UPGRADES DE ARMA  /  1 E 2", laranja, aco);
        Cubo(raiz, "PlacaPrecos", new Vector3(0, .81f, -.73f), new Vector3(3.3f, .35f, .04f), aco);
        Texto(raiz, "TabelaPrecos", $"DANO +{regras.aumentoDeDano}: {regras.custoDoDano} PTS   |   PENTE +{regras.aumentoDeCapacidade}: {regras.custoDaCapacidade} PTS", new Vector3(0, .81f, -.756f), 1.4f, new Vector2(3.1f, .3f), Color.white);
    }

    static void Farmacia(Transform raiz, EstacaoDeCura regras)
    {
        var verde = Mat("VerdeFarmacia", new Color(.08f, .47f, .30f));
        var claro = Mat("MarfimEstacoes", new Color(.91f, .87f, .74f));
        var vidro = Mat("VidroEstacoes", new Color(.12f, .32f, .35f));
        Cubo(raiz, "ArmarioPrimeirosSocorros", new Vector3(0, 1.3f, .22f), new Vector3(2.7f, 2.5f, .8f), claro);
        Cubo(raiz, "FundoPrateleiras", new Vector3(0, 1.4f, -.205f), new Vector3(2.4f, 1.9f, .04f), vidro);
        for (int linha = 0; linha < 3; linha++)
        {
            Cubo(raiz, "Prateleira_" + linha, new Vector3(0, .7f + linha * .6f, -.32f), new Vector3(2.4f, .055f, .35f), verde);
            for (int caixa = 0; caixa < 5; caixa++)
            {
                Cubo(raiz, "KitCura", new Vector3(-.92f + caixa * .46f, .92f + linha * .6f, -.34f), new Vector3(.33f, .38f, .22f), claro);
                Cubo(raiz, "FaixaKit", new Vector3(-.92f + caixa * .46f, .92f + linha * .6f, -.46f), new Vector3(.3f, .09f, .02f), verde);
            }
        }
        Cubo(raiz, "BalcaoAtendimento", new Vector3(0, .9f, -.63f), new Vector3(3.1f, .16f, .85f), verde);
        Placa(raiz, "FARMACIA DO BAIRRO", "PRIMEIROS SOCORROS  /  E", verde, claro);
        Cubo(raiz, "CruzVertical", new Vector3(-1.44f, 2.6f, -.2f), new Vector3(.14f, .54f, .08f), verde);
        Cubo(raiz, "CruzHorizontal", new Vector3(-1.44f, 2.6f, -.2f), new Vector3(.54f, .14f, .08f), verde);
        Cubo(raiz, "PlacaPrecos", new Vector3(0, .56f, -1.075f), new Vector3(2.4f, .43f, .05f), verde);
        Texto(raiz, "TabelaPrecos", $"+{regras.quantidadeDeCura} VIDA   |   {regras.custoEmPontos} PONTOS", new Vector3(0, .56f, -1.107f), 1.7f, new Vector2(2.25f, .35f), Color.white);
    }

    static void Recarga(Transform raiz, EstacaoDeMunicao regras)
    {
        var azul = Mat("AzulRecarga", new Color(.08f, .22f, .39f));
        var amarelo = Mat("AmareloRecarga", new Color(.98f, .66f, .12f));
        var aco = Mat("AcoEscuro", new Color(.09f, .12f, .14f));
        var ciano = Mat("LuzCiano", new Color(.1f, .8f, .95f), true);
        Cubo(raiz, "ArmarioIndustrial", new Vector3(0, 1.27f, .15f), new Vector3(2.7f, 2.5f, .95f), azul);
        Cubo(raiz, "PortaEsquerda", new Vector3(-.67f, 1.3f, -.35f), new Vector3(1.22f, 2.27f, .06f), aco);
        Cubo(raiz, "PortaDireita", new Vector3(.67f, 1.3f, -.35f), new Vector3(1.22f, 2.27f, .06f), aco);
        Cubo(raiz, "FaixaSeguranca", new Vector3(0, .33f, -.405f), new Vector3(2.5f, .12f, .04f), amarelo);
        for (int i = 0; i < 3; i++)
        {
            Cubo(raiz, "CelulaEnergia_" + i, new Vector3(-.76f + i * .76f, 1.56f, -.43f), new Vector3(.42f, .68f, .16f), ciano);
            Cubo(raiz, "BaseCelula_" + i, new Vector3(-.76f + i * .76f, 1.16f, -.43f), new Vector3(.54f, .1f, .25f), amarelo);
        }
        Cubo(raiz, "TerminalPagamento", new Vector3(1.36f, 1.17f, -.6f), new Vector3(.42f, .65f, .36f), amarelo);
        Cubo(raiz, "TelaTerminal", new Vector3(1.36f, 1.26f, -.79f), new Vector3(.29f, .28f, .025f), ciano);
        Placa(raiz, "RECARGA EXPRESSA", "DEPOSITO DE MUNICAO  /  E", amarelo, aco);
        Texto(raiz, "TabelaPrecos", $"+{regras.quantidadeDeMunicao} MUNICAO   |   {regras.custoEmPontos} PTS", new Vector3(-.15f, .76f, -.41f), 1.5f, new Vector2(2.2f, .38f), Color.white);
    }

    static void Placa(Transform pai, string titulo, string subtitulo, Material cor, Material fundo)
    {
        Cubo(pai, "Letreiro", new Vector3(0, 3.08f, .2f), new Vector3(3.9f, .82f, .2f), fundo);
        Cubo(pai, "BordaLetreiro", new Vector3(0, 3.51f, .2f), new Vector3(4.05f, .08f, .27f), cor);
        Cubo(pai, "Marquise", new Vector3(0, 2.64f, -.03f), new Vector3(3.9f, .09f, 1.6f), cor);
        Texto(pai, "NomeEstacao", titulo, new Vector3(0, 3.23f, .085f), 3.05f, new Vector2(3.7f, .42f), cor.color);
        Texto(pai, "Servico", subtitulo, new Vector3(0, 2.92f, .085f), 1.45f, new Vector2(3.7f, .3f),
            fundo.color.grayscale > .6f ? new Color(.06f, .2f, .13f) : Color.white);
    }

    static Transform Grupo(Transform pai, string nome)
    {
        var go = new GameObject(nome);
        SceneManager.MoveGameObjectToScene(go, pai.gameObject.scene);
        go.transform.SetParent(pai, false);
        return go.transform;
    }

    static GameObject Cubo(Transform pai, string nome, Vector3 posicao, Vector3 tamanho, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(go, pai.gameObject.scene);
        go.name = nome;
        go.layer = 6;
        go.transform.SetParent(pai, false);
        go.transform.localPosition = posicao;
        go.transform.localScale = tamanho;
        if (material != null) { go.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(go.GetComponent<Collider>()); }
        return go;
    }

    static void Texto(Transform pai, string nome, string conteudo, Vector3 posicao, float tamanho, Vector2 caixa, Color cor)
    {
        var go = Grupo(pai, nome).gameObject;
        go.transform.localPosition = posicao;
        var texto = go.AddComponent<TextMeshPro>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = cor;
        texto.rectTransform.sizeDelta = caixa;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = tamanho * .65f;
        texto.fontSizeMax = tamanho;
        texto.ForceMeshUpdate();
    }

    static Material Mat(string nome, Color cor, bool emissivo = false)
    {
        string pasta = Raiz + "/Cenarios/Rua/Materiais/Estacoes";
        CriarPasta(pasta);
        string caminho = pasta + "/" + nome + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nome, color = cor };
        material.SetFloat("_Smoothness", .25f);
        if (emissivo) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", cor * 1.2f); }
        AssetDatabase.CreateAsset(material, caminho);
        return material;
    }

    static void CriarPasta(string pasta)
    {
        if (AssetDatabase.IsValidFolder(pasta)) return;
        string pai = pasta.Substring(0, pasta.LastIndexOf('/'));
        CriarPasta(pai);
        AssetDatabase.CreateFolder(pai, pasta.Substring(pai.Length + 1));
    }

    static void Instanciar(Scene cena, Transform grupo, string prefab, string nome, Vector3 posicao, float angulo)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + prefab + ".prefab"), cena);
        go.name = nome;
        go.transform.SetParent(grupo, false);
        go.transform.SetPositionAndRotation(posicao, Quaternion.Euler(0, angulo, 0));
    }

    static void AtualizarLetreiro(Scene cena, string edificio, string titulo)
    {
        var raiz = cena.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Single(x => x.name == edificio);
        var letreiro = raiz.GetComponentsInChildren<TextMeshPro>(true).First(x => x.name == "Letreiro");
        letreiro.text = titulo;
        letreiro.gameObject.SetActive(false); // A placa fisica da estacao passa a identificar o comercio.
    }

    static void Validar(Component estacao, GameObject painel, TMP_Text texto)
    {
        if (painel == null || texto == null || !painel.transform.IsChildOf(estacao.transform))
            throw new InvalidOperationException("UI invalida: " + estacao.name);
        if (!estacao.GetComponents<Collider>().Any(x => x.enabled && x.isTrigger))
            throw new InvalidOperationException("Estacao sem gatilho: " + estacao.name);
        string caminho = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(estacao.gameObject);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null || PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.Variant)
            throw new InvalidOperationException("Estacao sem variante: " + estacao.name + "; asset=" + caminho);
    }

    static void Capturar()
    {
        Directory.CreateDirectory("Docs/Previews/Rua");
        var cena = EditorSceneManager.OpenPreviewScene(Cena);
        RenderTexture alvo = null;
        try
        {
            foreach (var raiz in cena.GetRootGameObjects())
            {
                foreach (var camera in raiz.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (var canvas in raiz.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
                foreach (var listener in raiz.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
            var go = new GameObject("CameraPreviaEstacoesRua");
            SceneManager.MoveGameObjectToScene(go, cena);
            var captura = go.AddComponent<Camera>();
            captura.enabled = false;
            captura.scene = cena;
            captura.fieldOfView = 54;
            captura.farClipPlane = 300;
            captura.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            alvo = new RenderTexture(1440, 900, 24);
            alvo.Create();
            Foto(captura, alvo, new Vector3(-8.2f, 3.0f, -15.4f), new Vector3(-15.25f, 1.9f, -11.5f), "oficina_derby");
            Foto(captura, alvo, new Vector3(8.2f, 2.9f, 9.5f), new Vector3(15.25f, 1.9f, 13.5f), "farmacia_bairro");
            Foto(captura, alvo, new Vector3(8.2f, 2.9f, -15.4f), new Vector3(15.25f, 1.9f, -11.5f), "deposito_recarga");
            Foto(captura, alvo, new Vector3(0, 4.0f, -21), new Vector3(0, 2.0f, 10), "rua_com_estacoes");
            File.WriteAllText(Pasta + "/previas-status.txt", "4 previas das estacoes renderizadas no Unity.\n");
        }
        finally { if (alvo != null) { alvo.Release(); Object.DestroyImmediate(alvo); } EditorSceneManager.ClosePreviewScene(cena); }
    }

    static void Foto(Camera camera, RenderTexture alvo, Vector3 posicao, Vector3 destino, string nome)
    {
        camera.transform.SetPositionAndRotation(posicao, Quaternion.LookRotation(destino - posicao));
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = alvo });
        var anterior = RenderTexture.active;
        var textura = new Texture2D(alvo.width, alvo.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = alvo;
            textura.ReadPixels(new Rect(0, 0, alvo.width, alvo.height), 0, 0);
            textura.Apply();
            File.WriteAllBytes("Docs/Previews/Rua/" + nome + ".png", textura.EncodeToPNG());
        }
        finally { RenderTexture.active = anterior; Object.DestroyImmediate(textura); }
    }

    [MenuItem("Derby/Rua TPS/Testar compras nas estacoes")]
    public static void Testar()
    {
        Directory.CreateDirectory(Pasta);
        File.WriteAllText(Pasta + "/testes-status.txt", "Executando compras e gatilhos em Play Mode...\n");
        File.WriteAllText(Pasta + "/testes-detalhes.txt", "");
        SessionState.SetBool("EstacoesRua.Testando", true);
        runner.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.PlayMode,
            testNames = new[] { "EstacoesRuaTests.OficinasCompramUpgradesComPontos", "EstacoesRuaTests.FarmaciaERecargaUsamOsComponentesOriginais" }
        }));
    }

    class Resultados : ICallbacks
    {
        public void RunStarted(ITestAdaptor teste) { }
        public void TestStarted(ITestAdaptor teste) { }
        public void TestFinished(ITestResultAdaptor resultado)
        {
            if (SessionState.GetBool("EstacoesRua.Testando", false) && !resultado.HasChildren)
                File.AppendAllText(Pasta + "/testes-detalhes.txt", resultado.FullName + ": " + resultado.ResultState + "\n" + resultado.Message + "\n" + resultado.StackTrace + "\n" + resultado.Output + "\n");
        }
        public void RunFinished(ITestResultAdaptor resultado)
        {
            if (!SessionState.GetBool("EstacoesRua.Testando", false)) return;
            File.WriteAllText(Pasta + "/testes-status.txt", $"{resultado.ResultState}; passou={resultado.PassCount}; falhou={resultado.FailCount}; ignorado={resultado.SkipCount}\n");
            TestRunnerApi.SaveResultToFile(resultado, Pasta + "/testes.xml");
            SessionState.SetBool("EstacoesRua.Testando", false);
        }
    }
}

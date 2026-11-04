using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Migra os assets pelo AssetDatabase, mantendo GUIDs e referencias serializadas.
// O pedido em Library permite executar no editor aberto sem alterar a cena por fora.
public static class OrganizadorProjeto
{
    const string Origem = "Assets/MEU_PROJETO";
    const string Destino = "Assets/DerbySegundoTempo";
    const string Relatorios = "Library/Organizacao";
    static readonly List<KeyValuePair<string, string>> movimentos = new List<KeyValuePair<string, string>>();

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        if (File.Exists(Relatorios + ".request")) EditorApplication.delayCall += ExecutarPedido;
        if (File.Exists(Relatorios + ".corrigir")) EditorApplication.delayCall += CorrigirInstalacao;
    }

    static void CorrigirInstalacao()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += CorrigirInstalacao; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Relatorios + ".corrigir");
        try
        {
            string caminhoRua = Destino + "/Cenas/Fases/Rua_TPS.unity";
            var ruaAberta = SceneManager.GetSceneByPath(caminhoRua);
            if (ruaAberta.isLoaded)
            {
                if (ruaAberta.isDirty) throw new InvalidOperationException("Salve a rua antes de corrigir a instalacao.");
                EditorSceneManager.CloseScene(ruaAberta, true);
            }
            // SaveScene faz a copia nativa das referencias entre objetos de raizes diferentes.
            string caminhoBase = Destino + "/Cenas/Fases/Estadio_TPS_Backup_Visual.unity";
            var cenaBase = SceneManager.GetSceneByPath(caminhoBase);
            bool abriuBase = !cenaBase.isLoaded;
            if (abriuBase) cenaBase = EditorSceneManager.OpenScene(caminhoBase, OpenSceneMode.Additive);
            EditorSceneManager.SaveScene(cenaBase, caminhoRua, true);
            if (abriuBase) EditorSceneManager.CloseScene(cenaBase, true);
            AssetDatabase.ImportAsset(caminhoRua, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            foreach (string nome in new[] { "Estadio_TPS_Backup_Visual", "Rua_TPS" })
            {
                string caminho = Destino + "/Cenas/Fases/" + nome + ".unity";
                var cena = SceneManager.GetSceneByPath(caminho);
                bool abriu = !cena.isLoaded;
                if (abriu) cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive);
                var anterior = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(cena);
                try
                {
                    if (nome == "Rua_TPS") MontarRua(cena);
                    OrganizarHierarquia(cena);
                    var controle = Componentes<GerenciadorDeCenas>(cena).Single();
                    controle.nomeDaTelaInicial = "TelaInicial";
                    controle.nomeDaProximaFase = nome == "Rua_TPS" ? "" : "Rua_TPS";
                    if (nome != "Rua_TPS") GerarNavegacao(cena, Componentes<NavMeshSurface>(cena).Single(), "Estadio");
                    var jogo = new SerializedObject(Componentes<GerenciadorDeJogo>(cena).Single());
                    foreach (string tela in new[] { "telaDePause", "telaDeGameOver", "telaDeVitoria", "telaDeTutorial" })
                        if (jogo.FindProperty(tela).objectReferenceValue == null)
                            throw new InvalidOperationException("Interface sem referencia: " + nome + "/" + tela);
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                }
                finally
                {
                    if (anterior.IsValid() && anterior.isLoaded) SceneManager.SetActiveScene(anterior);
                    if (abriu) EditorSceneManager.CloseScene(cena, true);
                }
            }
            Mover(Destino + "/Cenas/SampleScene", Destino + "/Cenas/Arquivo/SampleScene");
            AssetDatabase.SaveAssets();
            File.WriteAllText(Relatorios + "/correcao-status.txt", "HUD e telas preservados nas duas fases; NavMeshs compativeis com o perseguidor.\n");
            File.WriteAllText(Relatorios + ".testar", "");
            File.WriteAllText(Relatorios + ".preview", "");
        }
        catch (Exception erro)
        { File.WriteAllText(Relatorios + "/correcao-erro.txt", erro.ToString()); Debug.LogException(erro); }
    }

    static bool InterfaceDeTela(GameObject raiz) => raiz.GetComponentsInChildren<Canvas>(true)
        .Any(x => x.renderMode != RenderMode.WorldSpace);

    static bool EhGrupo(Transform objeto) => objeto.name.Length > 2
        && char.IsDigit(objeto.name[0]) && objeto.name[2] == '_';

    static void ExecutarPedido()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += ExecutarPedido; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Relatorios + ".request");
        try { Executar(); }
        catch (Exception erro)
        {
            Directory.CreateDirectory(Relatorios);
            File.WriteAllText(Relatorios + "/erro.txt", erro.ToString());
            Debug.LogException(erro);
        }
    }

    [MenuItem("Derby/Projeto/Organizar projeto e implementar Rua TPS")]
    public static void Executar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Saia do Play Mode antes de organizar.");
        Directory.CreateDirectory(Relatorios);
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Salve a cena aberta antes de organizar: " + SceneManager.GetSceneAt(i).path);
        if (AssetDatabase.IsValidFolder(Origem))
        {
            foreach (var nome in new[] { "Estadio_TPS_Backup_Visual", "Rua_TPS", "TelaInicial", "TelaPausa" })
            {
                string caminho = "Assets/Scenes/" + nome + ".unity";
                File.Copy(caminho, Relatorios + "/Antes_" + nome + ".unity", true);
                var cena = SceneManager.GetSceneByPath(caminho);
                bool abriu = !cena.isLoaded;
                if (abriu) cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive);
                var ativa = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(cena);
                try
                {
                    if (nome == "Rua_TPS") MontarRua(cena);
                    OrganizarHierarquia(cena);
                    foreach (var controle in Componentes<GerenciadorDeCenas>(cena))
                    {
                        controle.nomeDaTelaInicial = "TelaInicial";
                        controle.nomeDaProximaFase = nome == "Rua_TPS" ? ""
                            : nome == "Estadio_TPS_Backup_Visual" ? "Rua_TPS" : "Estadio_TPS_Backup_Visual";
                        EditorUtility.SetDirty(controle);
                    }
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                }
                finally
                {
                    if (ativa.IsValid() && ativa.isLoaded) SceneManager.SetActiveScene(ativa);
                    if (abriu) EditorSceneManager.CloseScene(cena, true);
                }
            }
            MoverArquivos();
        }
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Destino + "/Cenas/Menus/TelaInicial.unity", true),
            new EditorBuildSettingsScene(Destino + "/Cenas/Fases/Estadio_TPS_Backup_Visual.unity", true),
            new EditorBuildSettingsScene(Destino + "/Cenas/Fases/Rua_TPS.unity", true),
            new EditorBuildSettingsScene(Destino + "/Cenas/Menus/TelaPausa.unity", true)
        };
        AssetDatabase.SaveAssets();
        File.WriteAllText(Relatorios + "/status.txt", "Organizacao aplicada. Estadio: fase 1. Rua: fase 2. Menus preservados.\n");
        AssetDatabase.Refresh();
    }

    static IEnumerable<T> Componentes<T>(Scene cena) where T : Component =>
        cena.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true));

    static Transform Grupo(Scene cena, string nome, Transform pai = null)
    {
        var existente = pai == null ? cena.GetRootGameObjects().FirstOrDefault(x => x.name == nome)?.transform
            : pai.Find(nome);
        if (existente != null) return existente;
        var go = new GameObject(nome);
        SceneManager.MoveGameObjectToScene(go, cena);
        go.transform.SetParent(pai, false);
        return go.transform;
    }

    static void OrganizarHierarquia(Scene cena)
    {
        var raizes = cena.GetRootGameObjects();
        var sistemas = Grupo(cena, "00_Sistemas");
        var jogador = Grupo(cena, "01_Jogador");
        var cameras = Grupo(cena, "02_Cameras");
        var cenario = Grupo(cena, "03_Cenario");
        var jogabilidade = Grupo(cena, "04_Jogabilidade");
        var luzes = Grupo(cena, "05_Iluminacao");
        var interfaceJogo = Grupo(cena, "06_Interface");
        var audio = Grupo(cena, "07_Audio");
        foreach (var raiz in raizes)
        {
            if (raiz.name.Length > 2 && char.IsDigit(raiz.name[0]) && raiz.name[2] == '_') continue;
            Transform pai;
            if (raiz.GetComponent<MusicaDoJogo>() != null || raiz.name == "AudioManager") pai = audio;
            else if (InterfaceDeTela(raiz) || raiz.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) pai = interfaceJogo;
            else if (raiz.GetComponent<Camera>() != null || raiz.name.Contains("Cinemachine")) pai = cameras;
            else if (raiz.GetComponent<JogadorMovimento>() != null || raiz.GetComponentInChildren<EmissorLaser>(true) != null) pai = jogador;
            else if (raiz.GetComponent<Light>() != null || raiz.GetComponent<UnityEngine.Rendering.Volume>() != null) pai = luzes;
            else if (raiz.GetComponentInChildren<GerenciadorDeJogo>(true) != null || raiz.GetComponent<GerenciadorDeCenas>() != null) pai = sistemas;
            else if (raiz.GetComponent<GolEasterEgg>() != null || raiz.name.Contains("Spawn") || raiz.name.StartsWith("Estac") || raiz.name.Contains("Coletave")) pai = jogabilidade;
            else pai = cenario;
            raiz.transform.SetParent(pai, true);
            if (raiz.name == "soccer_stadium") raiz.name = "Estadio_Modelo";
            if (raiz.name == "Directional Light") raiz.name = "Sol";
            if (raiz.name == "Global Volume") raiz.name = "PosProcessamento";
            if (raiz.name == "NavMesh Surface") raiz.name = "Navegacao_Inimigos";
            if (raiz.name == "Canvas") raiz.name = "Interface_Principal";
        }
        foreach (var canvas in Componentes<Canvas>(cena).Where(x => x.renderMode != RenderMode.WorldSpace).ToArray())
        {
            var topo = canvas.transform;
            while (topo.parent != null && !EhGrupo(topo.parent)) topo = topo.parent;
            topo.SetParent(interfaceJogo, true);
        }
        var hordas = Componentes<GerenciadorDeOrdas>(cena).SingleOrDefault();
        if (hordas != null)
        {
            hordas.inimigosAtivos = Grupo(cena, "InimigosAtivos", jogabilidade);
            var pontos = Grupo(cena, "PontosDeSpawn", jogabilidade);
            for (int i = 0; i < hordas.pontosDeSpawn.Length; i++)
            {
                var ponto = hordas.pontosDeSpawn[i];
                if (ponto == null) throw new InvalidOperationException("Spawn sem referencia: " + cena.name);
                // Filhos de prefabs permanecem dentro do prefab para preservar a ligacao.
                if (!PrefabUtility.IsPartOfPrefabInstance(ponto))
                    ponto.SetParent(pontos, true);
            }
            EditorUtility.SetDirty(hordas);
            if (pontos.childCount == 0) Object.DestroyImmediate(pontos.gameObject);
        }
        foreach (var grupo in new[] { sistemas, jogador, cameras, cenario, jogabilidade, luzes, interfaceJogo, audio })
            if (grupo.childCount == 0) Object.DestroyImmediate(grupo.gameObject);
    }

    static void MontarRua(Scene cena)
    {
        // Aceita tambem uma copia da primeira fase ja organizada.
        foreach (var raiz in cena.GetRootGameObjects().Where(x => EhGrupo(x.transform)).ToArray())
        {
            foreach (var filho in raiz.transform.Cast<Transform>().ToArray()) filho.SetParent(null, true);
            Object.DestroyImmediate(raiz);
        }
        var jogador = Componentes<JogadorMovimento>(cena).Single();
        var hordas = Componentes<GerenciadorDeOrdas>(cena).Single();
        Vector3 deslocamento = new Vector3(0, .65f, -18) - jogador.transform.position;
        var arma = Componentes<EmissorLaser>(cena).SingleOrDefault();
        foreach (var raiz in cena.GetRootGameObjects())
        {
            bool manter = raiz.GetComponentInChildren<JogadorMovimento>(true) != null
                || raiz.GetComponentInChildren<GerenciadorDeJogo>(true) != null
                || InterfaceDeTela(raiz)
                || raiz.name == "Canvas" || raiz.name == "Interface_Principal"
                || raiz.GetComponent<UnityEngine.EventSystems.EventSystem>() != null
                || raiz.GetComponent<Camera>() != null || raiz.name.Contains("Cinemachine")
                || raiz.GetComponent<MusicaDoJogo>() != null || raiz.GetComponent<Light>() != null
                || raiz.GetComponent<UnityEngine.Rendering.Volume>() != null
                || (arma != null && arma.transform.root == raiz.transform);
            if (!manter) Object.DestroyImmediate(raiz);
            else if (raiz.GetComponentInChildren<JogadorMovimento>(true) != null
                || raiz.GetComponent<Camera>() != null || raiz.name.Contains("Cinemachine")
                || (arma != null && arma.transform.root == raiz.transform)) raiz.transform.position += deslocamento;
        }
        var cenario = Grupo(cena, "Rua_Ambiente");
        var piso = Grupo(cena, "Pisos_E_Calcadas", cenario);
        var predios = Grupo(cena, "Edificios", cenario);
        var sinalizacao = Grupo(cena, "Sinalizacao", cenario);
        var cobertura = Grupo(cena, "Coberturas", cenario);
        var limites = Grupo(cena, "Limites", cenario);
        var asfalto = MaterialRua("Asfalto", new Color(.10f, .12f, .14f));
        var concreto = MaterialRua("Concreto", new Color(.48f, .48f, .44f));
        var amarelo = MaterialRua("SinalizacaoAmarela", new Color(.95f, .67f, .12f));
        var branco = MaterialRua("SinalizacaoBranca", new Color(.85f, .84f, .76f));
        var metal = MaterialRua("Metal", new Color(.13f, .19f, .20f));
        var vidro = MaterialRua("Vidro", new Color(.16f, .32f, .38f));
        var tijolo = MaterialRua("Tijolo", new Color(.49f, .26f, .20f));
        var verde = MaterialRua("FachadaVerde", new Color(.21f, .36f, .31f));
        var bege = MaterialRua("FachadaBege", new Color(.62f, .54f, .39f));
        Cubo("Asfalto", piso, new Vector3(0, -.5f, 20), new Vector3(36, 1, 100), asfalto);
        foreach (int lado in new[] { -1, 1 })
        {
            Cubo("Calcada_" + (lado < 0 ? "Oeste" : "Leste"), piso, new Vector3(lado * 13.5f, .1f, 20), new Vector3(7, .2f, 100), concreto);
            Cubo("MeioFio_" + lado, piso, new Vector3(lado * 10.1f, .13f, 20), new Vector3(.25f, .26f, 100), branco);
            Cubo("LimiteLateral_" + lado, limites, new Vector3(lado * 18, 3, 20), new Vector3(1, 6, 100), tijolo);
            for (int i = 0; i < 8; i++)
            {
                float z = -24 + i * 12.5f;
                float altura = 9 + (i * 7 % 5) * 2.5f;
                var predio = Grupo(cena, "Edificio_" + (lado < 0 ? "Oeste_" : "Leste_") + (i + 1).ToString("00"), predios);
                Cubo("Estrutura", predio, new Vector3(lado * 22, altura / 2, z), new Vector3(10, altura, 12), new[] { tijolo, verde, bege }[(i + (lado + 1)) % 3]);
                Cubo("Marquise", predio, new Vector3(lado * 16.7f, 3.1f, z), new Vector3(1.6f, .25f, 11), metal);
                Cubo("PortaLoja", predio, new Vector3(lado * 16.94f, 1.5f, z), new Vector3(.1f, 2.6f, 5.5f), metal, false);
                for (int andar = 0; andar < (int)((altura - 4) / 3); andar++)
                    for (int janela = -1; janela <= 1; janela++)
                        Cubo("Janela_" + andar + "_" + (janela + 2), predio, new Vector3(lado * 16.94f, 5 + andar * 3, z + janela * 3.3f), new Vector3(.08f, 1.5f, 1.7f), vidro, false);
                Texto("Letreiro", predio, new[] { "OFICINA", "MERCADO", "LANCHES", "FARMACIA" }[i % 4], new Vector3(lado * 16.83f, 3.65f, z), Quaternion.Euler(0, lado < 0 ? -90 : 90, 0), .8f, branco.color);
            }
        }
        for (int i = 0; i < 20; i++)
            Cubo("FaixaCentral_" + (i + 1).ToString("00"), sinalizacao, new Vector3(0, .012f, -27 + i * 5), new Vector3(.16f, .02f, 2.4f), amarelo, false);
        foreach (float z in new[] { -10f, 45f })
            for (int i = 0; i < 10; i++)
                Cubo("FaixaPedestres_" + z + "_" + i, sinalizacao, new Vector3(-8.5f + i * 1.9f, .025f, z), new Vector3(1.15f, .03f, 3.2f), branco, false);
        foreach (float z in new[] { -30f, 70f })
        {
            Cubo("BloqueioDaRua_" + z, limites, new Vector3(0, 1.6f, z), new Vector3(36, 3.2f, 1), concreto);
            Texto("PlacaLimite", sinalizacao, z < 0 ? "DERBY / SAIDA DO ESTADIO" : "RUA TPS / FASE 02", new Vector3(0, 4.5f, z < 0 ? -29.4f : 69.4f), Quaternion.Euler(0, z < 0 ? 180 : 0, 0), 1.3f, branco.color);
        }
        var carroAzul = MaterialRua("CarroAzul", new Color(.10f, .30f, .44f));
        var carroVermelho = MaterialRua("CarroVermelho", new Color(.58f, .17f, .13f));
        for (int i = 0; i < 4; i++)
        {
            var carro = Grupo(cena, "Carro_" + (i + 1).ToString("00"), cobertura);
            Vector3 p = new Vector3(i % 2 == 0 ? -7 : 7, 0, i * 19 - 2);
            Cubo("Carroceria", carro, p + Vector3.up * .65f, new Vector3(2.2f, 1, 4.4f), i % 2 == 0 ? carroAzul : carroVermelho);
            Cubo("Cabine", carro, p + new Vector3(0, 1.35f, -.2f), new Vector3(1.9f, .7f, 2.2f), vidro);
            for (int eixo = -1; eixo <= 1; eixo += 2)
                for (int lado = -1; lado <= 1; lado += 2)
                    Cubo("Roda_" + eixo + "_" + lado, carro, p + new Vector3(lado * 1.06f, .4f, eixo * 1.35f), new Vector3(.28f, .65f, .7f), metal, false);
        }
        foreach (float z in new[] { 13f, 33f })
        {
            Cubo("Barreira_" + z, cobertura, new Vector3(z == 13 ? -3 : 3, .6f, z), new Vector3(4, 1.2f, .8f), concreto);
            Cubo("FaixaBarreira_" + z, cobertura, new Vector3(z == 13 ? -3 : 3, 1.22f, z), new Vector3(4, .06f, .8f), amarelo, false);
        }
        var iluminacao = Grupo(cena, "Postes", cenario);
        for (int i = 0; i < 6; i++)
        {
            var poste = Grupo(cena, "Poste_" + (i + 1).ToString("00"), iluminacao);
            Vector3 p = new Vector3(i % 2 == 0 ? -11 : 11, 0, -20 + i * 16);
            Cubo("Haste", poste, p + Vector3.up * 3.5f, new Vector3(.15f, 7, .15f), metal);
            Cubo("Luminaria", poste, p + new Vector3(0, 7, 0), new Vector3(1.2f, .18f, .6f), branco, false);
        }
        var estacoes = Grupo(cena, "Estacoes_Rua");
        string[] prefabs = { "EstacaoMedkit", "EstacaoMunicao", "PowerUp" };
        for (int i = 0; i < prefabs.Length; i++)
        {
            string caminhoPrefab = AssetDatabase.IsValidFolder(Origem) ? Origem + "/Prefabs/" + prefabs[i] + ".prefab"
                : Destino + "/Prefabs/Estacoes/" + new[] { "EstacaoCura", "EstacaoMunicao", "EstacaoUpgrades" }[i] + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
            if (prefab == null) throw new InvalidOperationException("Prefab ausente: " + prefabs[i]);
            var estacao = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cena);
            estacao.name = new[] { "Estacao_Cura", "Estacao_Municao", "Estacao_Upgrades" }[i];
            estacao.transform.SetParent(estacoes, false);
            estacao.transform.position = new Vector3(-13 + i * 13, .3f, -24);
            var malhas = estacao.GetComponentsInChildren<MeshRenderer>()
                .Where(x => x.GetComponent<TMP_Text>() == null).ToArray();
            if (malhas.Length > 0)
                estacao.transform.position += Vector3.up * ((i == 1 ? .03f : .23f) - malhas.Min(x => x.bounds.min.y));
        }
        var spawns = Grupo(cena, "PontosDeSpawn_Rua");
        Vector3[] posicoes = { new Vector3(-7, .1f, 59), new Vector3(7, .1f, 59), new Vector3(-7, .1f, 38), new Vector3(7, .1f, 24), new Vector3(-7, .1f, 20), new Vector3(7, .1f, 7), new Vector3(-7, .1f, -6), new Vector3(7, .1f, -9) };
        hordas.pontosDeSpawn = posicoes.Select((p, i) =>
        {
            var spawn = Grupo(cena, "Spawn_" + (i + 1).ToString("00"), spawns);
            spawn.position = p; return spawn;
        }).ToArray();
        var nav = cenario.gameObject.AddComponent<NavMeshSurface>();
        nav.collectObjects = CollectObjects.Children;
        nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        GerarNavegacao(cena, nav, "Rua");
        foreach (var spawn in hordas.pontosDeSpawn)
        {
            if (!NavMesh.SamplePosition(spawn.position, out var hit, 2, NavMesh.AllAreas))
                throw new InvalidOperationException("Spawn fora da navegacao: " + spawn.name);
            spawn.position = hit.position;
        }
        EditorUtility.SetDirty(hordas);
    }

    static Material MaterialRua(string nome, Color cor)
    {
        string pasta = (AssetDatabase.IsValidFolder(Origem) ? Origem : Destino) + "/Cenarios/Rua/Materiais";
        Pasta(pasta);
        string caminho = pasta + "/" + nome + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nome, color = cor };
        mat.SetFloat("_Smoothness", .15f);
        AssetDatabase.CreateAsset(mat, caminho);
        return mat;
    }

    static void GerarNavegacao(Scene cena, NavMeshSurface surface, string nome)
    {
        string inimigo = AssetDatabase.IsValidFolder(Origem) ? Origem + "/Prefabs/Inimigo_perseguidor.prefab"
            : Destino + "/Prefabs/Inimigos/InimigoPerseguidor.prefab";
        surface.agentTypeID = AssetDatabase.LoadAssetAtPath<GameObject>(inimigo).GetComponent<NavMeshAgent>().agentTypeID;
        if (nome == "Estadio")
        {
            surface.collectObjects = CollectObjects.Volume;
            surface.center = surface.transform.InverseTransformPoint(new Vector3(11, -23, 3909));
            surface.size = new Vector3(135, 15, 82);
            surface.layerMask = 1 << 6;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("Falha na navegacao: " + nome);
        string pasta = Path.GetDirectoryName(cena.path).Replace('\\', '/') + "/" + cena.name;
        Pasta(pasta);
        string caminho = AssetDatabase.GenerateUniqueAssetPath(pasta + "/Navegacao_" + nome + "_Perseguidores.asset");
        AssetDatabase.CreateAsset(surface.navMeshData, caminho);
        EditorUtility.SetDirty(surface);
        var filtro = new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };
        var hordas = Componentes<GerenciadorDeOrdas>(cena).Single();
        foreach (var ponto in hordas.pontosDeSpawn.Where(x => x != null))
            if (!NavMesh.SamplePosition(ponto.position, out _, 2, filtro))
                throw new InvalidOperationException("Spawn fora da NavMesh do perseguidor: " + cena.name + "/" + ponto.name);
    }

    static GameObject Cubo(string nome, Transform pai, Vector3 posicao, Vector3 escala, Material material, bool colisao = true)
    {
        var objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objeto.name = nome;
        objeto.layer = 6; // Mesma camada de cenario utilizada pelo estadio.
        if (colisao && (pai.name == "Pisos_E_Calcadas" || escala.y <= 1.5f)) objeto.tag = "Chão";
        objeto.transform.SetParent(pai, false);
        objeto.transform.position = posicao;
        objeto.transform.localScale = escala;
        objeto.GetComponent<Renderer>().sharedMaterial = material;
        if (!colisao) Object.DestroyImmediate(objeto.GetComponent<Collider>());
        return objeto;
    }

    static void Texto(string nome, Transform pai, string conteudo, Vector3 posicao, Quaternion rotacao, float tamanho, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.transform.SetPositionAndRotation(posicao, rotacao);
        var texto = go.AddComponent<TextMeshPro>();
        texto.text = conteudo;
        texto.fontSize = tamanho * 10;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = cor;
        texto.rectTransform.sizeDelta = new Vector2(12, 2);
    }

    static void Pasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;
        string pai = caminho.Substring(0, caminho.LastIndexOf('/'));
        Pasta(pai);
        AssetDatabase.CreateFolder(pai, caminho.Substring(pai.Length + 1));
    }

    static void Mover(string origem, string destino)
    {
        if (AssetDatabase.LoadMainAssetAtPath(origem) == null && !AssetDatabase.IsValidFolder(origem)) return;
        Pasta(destino.Substring(0, destino.LastIndexOf('/')));
        string guid = AssetDatabase.AssetPathToGUID(origem);
        string erro = AssetDatabase.MoveAsset(origem, destino);
        if (!string.IsNullOrEmpty(erro)) throw new InvalidOperationException(erro);
        if (AssetDatabase.AssetPathToGUID(destino) != guid)
            throw new InvalidOperationException("GUID alterado ao mover: " + origem);
        movimentos.Add(new KeyValuePair<string, string>(origem, destino));
    }

    static void MoverArquivos()
    {
        movimentos.Clear();
        // Pacotes externos preservam nomes e estrutura do fornecedor.
        Mover(Origem + "/Packages", "Assets/Terceiros");
        Mover(Origem, Destino);
        Mover("Assets/Scenes", Destino + "/Cenas");
        foreach (var nome in new[] { "TelaInicial", "TelaPausa" })
            Mover(Destino + "/Cenas/" + nome + ".unity", Destino + "/Cenas/Menus/" + nome + ".unity");
        foreach (var nome in new[] { "Estadio_TPS_Backup_Visual", "Rua_TPS" })
        {
            Mover(Destino + "/Cenas/" + nome + ".unity", Destino + "/Cenas/Fases/" + nome + ".unity");
            Mover(Destino + "/Cenas/" + nome, Destino + "/Cenas/Fases/" + nome);
        }
        foreach (var nome in new[] { "Estadio_Futebol", "Estadio_TPS" })
        {
            Mover(Destino + "/Cenas/" + nome + ".unity", Destino + "/Cenas/Arquivo/" + nome + ".unity");
            Mover(Destino + "/Cenas/" + nome, Destino + "/Cenas/Arquivo/" + nome);
        }
        Mover(Destino + "/CenarioTPS", Destino + "/Cenarios/Estadio");
        Mover(Destino + "/ArmaLaser", Destino + "/Armas/Laser");
        foreach (string nome in new[] { "Modelos", "Materiais", "Animacoes", "Imagens" })
            Mover(Destino + "/" + nome, Destino + "/Arte/" + nome);
        Mover(Destino + "/Arte/Modelos/Imagens", Destino + "/Arte/Imagens/Interface");
        Mover(Destino + "/Sons", Destino + "/Audio");
        Mover("Assets/Settings", Destino + "/Configuracoes/Renderizacao");
        Mover("Assets/InputSystem_Actions.inputactions", Destino + "/Configuracoes/Entrada/Controles.inputactions");
        var categorias = new Dictionary<string, string[]>
        {
            ["Jogador"] = new[] { "JogadorVida", "JogadorMovimento", "JogadorMovimentoCamera", "JogadorTroncoCamera", "JogadorPulo", "JogadorAnimador", "JogadorInteracao", "JogadorSons" },
            ["Combate"] = new[] { "Arma", "ArmaNaMao", "EmissorLaser", "Municao" },
            ["Inimigos"] = new[] { "InimigoVida", "InimigoAtaque", "InimigoPerseguidor", "InimigoPatrulheiro", "InimigoPatrulheiroAtacante", "InimigoSomDePassos" },
            ["Sistemas"] = new[] { "GerenciadorDeCenas", "GerenciadorDeJogo", "GerenciadorDeOrdas", "SistemaDePontos", "Cronometro" },
            ["Interface"] = new[] { "HUDJogador", "BarraVidaInimigo", "OlharParaCamera", "ContadorFPS" },
            ["Interacao"] = new[] { "IInteragivel", "EstacaoDeCura", "EstacaoDeMunicao", "EstacaoDeUpgrade", "LojaDeUpgrades", "Medkit" },
            ["Cenario"] = new[] { "BolaFutebol", "GolEasterEgg", "TriggerGol", "IluminacaoPorOrda" },
            ["Cameras"] = new[] { "AlternarCamera" },
            ["Audio"] = new[] { "MusicaDoJogo" }
        };
        foreach (var categoria in categorias)
            foreach (string nome in categoria.Value)
                Mover(Destino + "/Scripts/" + nome + ".cs", Destino + "/Scripts/Runtime/" + categoria.Key + "/" + (nome == "InimigoPatrulheiroAtacante" ? "InimigoPatrulheiroAtaque" : nome) + ".cs");
        var prefabs = new Dictionary<string, string>
        {
            ["Jogador"] = "Jogador/Jogador", ["Inimigo_perseguidor"] = "Inimigos/InimigoPerseguidor",
            ["EstacaoMedkit"] = "Estacoes/EstacaoCura", ["EstacaoMunicao"] = "Estacoes/EstacaoMunicao",
            ["PowerUp"] = "Estacoes/EstacaoUpgrades", ["MedkitColetavel"] = "Coletaveis/MedkitColetavel",
            ["MunicaoColetavel"] = "Coletaveis/MunicaoColetavel", ["BancoReservas"] = "Cenario/BancoReservas",
            ["Obstaculos"] = "Cenario/Obstaculos", ["soccer_stadium"] = "Cenario/Estadio"
        };
        foreach (var prefab in prefabs)
            Mover(Destino + "/Prefabs/" + prefab.Key + ".prefab", Destino + "/Prefabs/" + prefab.Value + ".prefab");
        Mover(Destino + "/Arte/Materiais/Material_Fisico/New Physics Material.physicMaterial", Destino + "/Arte/Materiais/Fisica/ContatoPadrao.physicMaterial");
        Mover("Assets/Readme.asset", Destino + "/Documentacao/ReadmeTemplate.asset");
        Mover("Assets/TutorialInfo", Destino + "/Documentacao/TutorialTemplate");
        AtualizarCaminhos();
        File.WriteAllLines(Relatorios + "/movimentos.tsv", movimentos.Select(x => x.Key + "\t" + x.Value));
    }

    static void AtualizarCaminhos()
    {
        var arquivos = Directory.GetFiles(Destino, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles("Assets/Tests", "*.cs", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles("Tools", "*.py"))
            .Concat(Directory.GetFiles(Destino, "*.md", SearchOption.AllDirectories));
        foreach (var arquivo in arquivos)
        {
            if (arquivo.EndsWith("OrganizadorProjeto.cs")) continue;
            string original = File.ReadAllText(arquivo);
            string texto = original;
            foreach (var movimento in movimentos) texto = texto.Replace(movimento.Key, movimento.Value);
            // O caminho de pasta termina antes da extensao nos geradores.
            texto = texto.Replace("Assets/Scenes/Estadio_TPS_Backup_Visual", Destino + "/Cenas/Fases/Estadio_TPS_Backup_Visual")
                .Replace("Assets/Scenes/Estadio_TPS", Destino + "/Cenas/Arquivo/Estadio_TPS");
            if (texto != original) File.WriteAllText(arquivo, texto);
        }
    }
}

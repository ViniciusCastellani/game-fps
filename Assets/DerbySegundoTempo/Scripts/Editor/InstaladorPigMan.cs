using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class InstaladorPigMan
{
    public const string Modelo = "Assets/DerbySegundoTempo/Arte/Modelos/pigman/source/PigMan_SketchFab.fbx";
    public const string Prefab = "Assets/DerbySegundoTempo/Prefabs/Inimigos/PigManMiniBoss.prefab";
    public const string Pasta = "Assets/DerbySegundoTempo/Animacoes/PigMan";
    public const string Human = "Assets/Terceiros/Kevin Iglesias/Human Animations/";
    const string Materiais = "Assets/DerbySegundoTempo/Arte/Materiais/PigMan";
    const string Texturas = "Assets/DerbySegundoTempo/Arte/Modelos/pigman/textures/";
    const string Porco = "Assets/DerbySegundoTempo/Prefabs/Inimigos/InimigoPerseguidor.prefab";
    public static readonly string[] Cenas = {
        "Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity",
        "Assets/DerbySegundoTempo/Cenas/Fases/Rua_TPS.unity"
    };
    public static string Relatorios => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PigMan"));

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        if (File.Exists(Relatorios + ".request")) EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Executar;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Relatorios + ".request");
        Directory.CreateDirectory(Relatorios);
        try
        {
            Instalar();
            File.WriteAllText(Relatorios + "/status.txt", "Instalacao: OK");
        }
        catch (Exception erro)
        {
            File.WriteAllText(Relatorios + "/error.txt", erro.ToString());
            Debug.LogException(erro);
        }
    }

    static void CriarPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;
        string pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
        CriarPasta(pai);
        AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
    }

    static void ConfigurarAvatar()
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Modelo);
        var ossos = new Dictionary<string, string> {
            { "Hips", "hip" }, { "Spine", "spine" }, { "Chest", "chest" },
            { "Neck", "neck" }, { "Head", "head" }
        };
        foreach (string lado in new[] { "Left", "Right" })
        {
            string sufixo = lado == "Left" ? ".L" : ".R";
            ossos[lado + "Shoulder"] = "shoulder" + sufixo;
            ossos[lado + "UpperArm"] = "arm" + sufixo;
            ossos[lado + "LowerArm"] = "forearm" + sufixo;
            ossos[lado + "Hand"] = "hand" + sufixo;
            ossos[lado + "UpperLeg"] = "leg1" + sufixo;
            ossos[lado + "LowerLeg"] = "leg2" + sufixo;
            ossos[lado + "Foot"] = "leg3" + sufixo;
            ossos[lado + "Toes"] = "hoof" + sufixo;
            foreach (var dedo in new[] { ("Thumb", "thumb"), ("Index", "index"), ("Little", "lit") })
            {
                string[] partes = { "Proximal", "Intermediate", "Distal" };
                for (int i = 0; i < partes.Length; i++)
                    ossos[lado + dedo.Item1 + partes[i]] = dedo.Item2 + (dedo.Item2 == "thumb" && i == 0 ? "" : (i + 1).ToString()) + sufixo;
            }
        }
        var importer = (ModelImporter)AssetImporter.GetAtPath(Modelo);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.optimizeGameObjects = false;
        var descricao = importer.humanDescription;
        descricao.human = ossos.Select(par => new HumanBone {
            humanName = HumanTrait.BoneName.First(n => n.Replace(" ", "") == par.Key),
            boneName = par.Value, limit = new HumanLimit { useDefaultValues = true }
        }).ToArray();
        descricao.skeleton = modelo.GetComponentsInChildren<Transform>(true).Select(t => new SkeletonBone {
            name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale
        }).ToArray();
        // O FBX foi publicado em pose de combate. Calibra a pose T antes do retarget humanoide.
        var instancia = Object.Instantiate(modelo);
        instancia.name = modelo.name;
        instancia.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var setup = typeof(Editor).Assembly.GetType("UnityEditor.AvatarSetupTool", true);
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            setup.GetMethod("SampleBindPose", flags).Invoke(null, new object[] { instancia });
            var cabeca = instancia.GetComponentsInChildren<Transform>().Single(t => t.name == "head");
            Quaternion orientacaoDaCabeca = cabeca.rotation;
            var modeloOssos = setup.GetMethod("GetModelBones", flags).Invoke(null, new object[] { instancia.transform, false, null });
            var mapear = setup.GetMethods(flags).Single(m => m.Name == "GetHumanBones" && m.GetParameters()[0].ParameterType == typeof(Dictionary<string, string>));
            var humanos = mapear.Invoke(null, new[] { descricao.human.ToDictionary(h => h.humanName, h => h.boneName), modeloOssos });
            setup.GetMethod("MakePoseValid", flags).Invoke(null, new[] { humanos });
            setup.GetMethod("MakeCharacterPositionValid", flags).Invoke(null, new[] { humanos });
            // O focinho usa a orientacao do modelo, diferente da cabeca humana usada pela pose automatica.
            cabeca.rotation = orientacaoDaCabeca;
            descricao.skeleton = (SkeletonBone[])setup.GetMethod("GetSkeletonBones", flags).Invoke(null, new object[] { instancia.transform });
            File.WriteAllText(Relatorios + "/pose.txt", "Erro da pose T: " + setup.GetMethod("GetPoseError", flags).Invoke(null, new[] { humanos }));
        }
        finally { Object.DestroyImmediate(instancia); }
        descricao.upperArmTwist = .5f;
        descricao.lowerArmTwist = .5f;
        descricao.upperLegTwist = .5f;
        descricao.lowerLegTwist = .5f;
        descricao.armStretch = .05f;
        descricao.legStretch = .05f;
        importer.humanDescription = descricao;
        importer.SaveAndReimport();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(Modelo).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("O avatar humanoide do PigMan nao e valido.");
    }

    static Texture2D Textura(string nome, bool normal = false, bool linear = false)
    {
        string caminho = Texturas + nome + ".png";
        var importer = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (importer == null) throw new FileNotFoundException(caminho);
        var tipo = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (importer.textureType != tipo || importer.sRGBTexture == (normal || linear))
        {
            importer.textureType = tipo;
            importer.sRGBTexture = !normal && !linear;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
    }

    static Texture2D LerMapa(Texture textura, int tamanho)
    {
        var anterior = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(tamanho, tamanho, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        try
        {
            Graphics.Blit(textura, rt);
            RenderTexture.active = rt;
            var copia = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false, true);
            copia.ReadPixels(new Rect(0, 0, tamanho, tamanho), 0, 0);
            copia.Apply();
            return copia;
        }
        finally { RenderTexture.active = anterior; RenderTexture.ReleaseTemporary(rt); }
    }

    static Texture2D MetalSuavidade(string nome, string metal, string rugosidade)
    {
        const int tamanho = 1024;
        var m = LerMapa(Textura(metal, linear: true), tamanho);
        var r = LerMapa(Textura(rugosidade, linear: true), tamanho);
        var mapa = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false, true);
        try
        {
            var metais = m.GetPixels32();
            var rugosos = r.GetPixels32();
            for (int i = 0; i < metais.Length; i++)
                metais[i] = new Color32(metais[i].r, metais[i].r, metais[i].r, (byte)(255 - rugosos[i].r));
            mapa.SetPixels32(metais);
            mapa.Apply();
            string caminho = Materiais + "/Mapas/" + nome + "_MetallicSmoothness.png";
            File.WriteAllBytes(caminho, mapa.EncodeToPNG());
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(caminho);
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        }
        finally { Object.DestroyImmediate(m); Object.DestroyImmediate(r); Object.DestroyImmediate(mapa); }
    }

    static Material Material(string nome)
    {
        string caminho = Materiais + "/" + nome + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, caminho);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    static Dictionary<string, Material> CriarMateriais()
    {
        CriarPasta(Materiais + "/Mapas");
        var resultado = new Dictionary<string, Material>();
        foreach (var conjunto in new[] { ("PigMan_Body", "Body_and_head", "Roughness2"),
            ("PigMan_ArmsLegs", "Arms_and_Legs", "Roughness2"), ("PigMan_Armor", "Armor", "Roughness") })
        {
            var material = Material(conjunto.Item1);
            material.SetTexture("_BaseMap", Textura(conjunto.Item2 + "_Base_Color"));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", Textura(conjunto.Item2 + "_Normal_OpenGL", normal: true));
            material.SetTexture("_OcclusionMap", Textura(conjunto.Item2 + "_Mixed_AO", linear: true));
            material.SetTexture("_MetallicGlossMap", MetalSuavidade(conjunto.Item1, conjunto.Item2 + "_Metallic", conjunto.Item2 + "_" + conjunto.Item3));
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_OcclusionStrength", .8f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            resultado[conjunto.Item1] = material;
        }
        var pelos = Material("PigMan_Pelos");
        pelos.SetTexture("_BaseMap", Textura("H6_dif_alpha"));
        pelos.SetTexture("_BumpMap", Textura("H6_dif_nrml", normal: true));
        pelos.SetColor("_BaseColor", Color.white);
        pelos.SetFloat("_AlphaClip", 1);
        pelos.SetFloat("_Cutoff", .45f);
        pelos.SetFloat("_Cull", (float)CullMode.Off);
        pelos.SetFloat("_Smoothness", .15f);
        pelos.EnableKeyword("_ALPHATEST_ON");
        pelos.EnableKeyword("_NORMALMAP");
        pelos.SetOverrideTag("RenderType", "TransparentCutout");
        pelos.renderQueue = (int)RenderQueue.AlphaTest;
        resultado["Pig_fur"] = pelos;
        var espada = Material("PigMan_Espada");
        espada.SetColor("_BaseColor", new Color(.48f, .51f, .55f));
        espada.SetFloat("_Metallic", .8f);
        espada.SetFloat("_Smoothness", .5f);
        resultado["Espada"] = espada;
        return resultado;
    }

    public static AnimationClip Clipe(string caminho)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Human + "Animations/Male/" + caminho + ".fbx")
            .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
    }

    static AnimatorController CriarController()
    {
        CriarPasta(Pasta);
        string caminho = Pasta + "/PigManMiniBoss.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(caminho);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(caminho);
        else
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(caminho))
                if (asset != controller) Object.DestroyImmediate(asset, true);
            controller.layers = new[] { new AnimatorControllerLayer {
                name = "Base Layer", defaultWeight = 1f, stateMachine = new AnimatorStateMachine { name = "Base Layer" }
            } };
            AssetDatabase.AddObjectToAsset(controller.layers[0].stateMachine, controller);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
        }
        controller.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
        controller.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        var sm = controller.layers[0].stateMachine;
        var idle = sm.AddState("ProntoComEspada");
        idle.motion = Clipe("Combat/1H/HumanM@CombatIdle1H01");
        var andar = sm.AddState("Andar");
        // O pacote inclui corrida: a reproducao reduzida acompanha a velocidade de 2 m/s.
        andar.motion = Clipe("Movement/Run/HumanM@Run01_Forward");
        andar.speed = .48f;
        var golpe = sm.AddState("GolpeDeEspada");
        golpe.motion = Clipe("Combat/1H/HumanM@Attack1H01_R");
        golpe.speed = .8f;
        sm.defaultState = idle;
        var t = idle.AddTransition(andar);
        t.hasExitTime = false; t.duration = .15f;
        t.AddCondition(AnimatorConditionMode.Greater, .15f, "Velocidade");
        t = andar.AddTransition(idle);
        t.hasExitTime = false; t.duration = .15f;
        t.AddCondition(AnimatorConditionMode.Less, .15f, "Velocidade");
        t = sm.AddAnyStateTransition(golpe);
        t.hasExitTime = false; t.duration = .08f; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, "Atacar");
        t = golpe.AddTransition(idle);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = .12f;
        string mascaraPath = Pasta + "/BracosEspada.mask";
        var mascara = AssetDatabase.LoadAssetAtPath<AvatarMask>(mascaraPath);
        if (mascara == null) { mascara = new AvatarMask(); AssetDatabase.CreateAsset(mascara, mascaraPath); }
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            var parte = (AvatarMaskBodyPart)i;
            mascara.SetHumanoidBodyPartActive(parte, parte == AvatarMaskBodyPart.LeftArm || parte == AvatarMaskBodyPart.RightArm
                || parte == AvatarMaskBodyPart.LeftFingers || parte == AvatarMaskBodyPart.RightFingers);
        }
        controller.AddLayer("PosturaEspada");
        var camadas = controller.layers;
        var postura = camadas[1];
        postura.defaultWeight = 1;
        postura.avatarMask = mascara;
        postura.stateMachine.defaultState = postura.stateMachine.AddState("SegurarEspada");
        postura.stateMachine.defaultState.motion = idle.motion;
        controller.layers = camadas;
        EditorUtility.SetDirty(mascara);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void ConfigurarSons(GameObject raiz)
    {
        const string pastaAudio = "Assets/DerbySegundoTempo/Audio/PigMan/";
        AudioClip Carregar(string nome)
        {
            var clipe = AssetDatabase.LoadAssetAtPath<AudioClip>(pastaAudio + nome + ".wav");
            if (clipe == null) throw new InvalidOperationException("Som do PigMan ausente: " + nome);
            return clipe;
        }

        var ataque = raiz.GetComponent<InimigoAtaque>();
        ataque.somDoGolpe = Carregar("PigMan_GolpeEspada");
        ataque.somDeAtaque = Carregar("PigMan_AtaqueSucedido");
        ataque.destacarSomDeAcerto = true;
        ataque.volumeDoAcerto = .85f;
        var fonte = ataque.fonteDeAudio;
        fonte.playOnAwake = false;
        fonte.volume = .56f;
        fonte.spatialBlend = 1f;
        fonte.rolloffMode = AudioRolloffMode.Linear;
        fonte.minDistance = 3f;
        fonte.maxDistance = 28f;
        fonte.dopplerLevel = 0f;

        var passos = raiz.GetComponent<InimigoSomDePassos>();
        passos.sonsDePasso = new[] {
            Carregar("PigMan_PassoPesado01"), Carregar("PigMan_PassoPesado02"), Carregar("PigMan_PassoPesado03")
        };
        passos.volume = .82f;
        passos.intervaloEntrePassos = .7f;
        passos.distanciaMinima = 3f;
        passos.distanciaMaxima = 28f;
    }

    static GameObject CriarPrefab(AnimatorController controller, Dictionary<string, Material> materiais)
    {
        var raiz = PrefabUtility.LoadPrefabContents(Porco);
        try
        {
            raiz.name = "PigManMiniBoss";
            var barra = raiz.GetComponent<InimigoVida>().barraVida;
            foreach (Transform filho in raiz.transform.Cast<Transform>().ToArray())
                if (filho != barra.transform) Object.DestroyImmediate(filho.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Modelo), raiz.transform);
            visual.name = "PigMan";
            visual.transform.localPosition = Vector3.up * .15f;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 1.15f;
            var pose = ((ModelImporter)AssetImporter.GetAtPath(Modelo)).humanDescription.skeleton.ToDictionary(b => b.name);
            foreach (var osso in visual.GetComponentsInChildren<Transform>(true))
                if (osso != visual.transform && pose.TryGetValue(osso.name, out var configuracao))
                {
                    osso.localPosition = configuracao.position;
                    osso.localRotation = configuracao.rotation;
                    osso.localScale = configuracao.scale;
                }
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.StartsWith("Axe")) { renderer.gameObject.SetActive(false); continue; }
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materiais.TryGetValue(m.name, out var novo) ? novo : m).ToArray();
                if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
            }
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(Modelo).OfType<Avatar>().First();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var mao = visual.GetComponentsInChildren<Transform>().Single(t => t.name == "hand.R");
            var espada = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                Human + "Unity Demo Scenes/Human Melee Animations/Prefabs/Weapons/Human_Sword.prefab"), mao);
            espada.name = "EspadaPigMan";
            espada.transform.localPosition = new Vector3(0, .0017f, 0);
            var referencia = AssetDatabase.LoadAssetAtPath<GameObject>(Human + "Models/HumanM_Model.fbx");
            var referenciaProp = referencia.GetComponentsInChildren<Transform>().Single(t => t.name == "B-handProp.R");
            espada.transform.rotation = referenciaProp.rotation;
            espada.transform.localScale = Vector3.one * .012f;
            foreach (var r in espada.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = r.sharedMaterials.Select(_ => materiais["Espada"]).ToArray();
            var agente = raiz.GetComponent<NavMeshAgent>();
            agente.speed = 2f;
            agente.acceleration = 5f;
            agente.angularSpeed = 160;
            agente.radius = .8f;
            agente.height = 3.5f;
            agente.baseOffset = 0;
            agente.stoppingDistance = 2.3f;
            agente.avoidancePriority = 35;
            var collider = raiz.GetComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 1.75f, 0);
            collider.height = 3.5f;
            collider.radius = .85f;
            var vida = raiz.GetComponent<InimigoVida>();
            vida.vida = 500;
            vida.pontosAoMorrer = 150;
            vida.chanceDeDrop = 1;
            vida.offsetParticulaMorte = Vector3.up * 1.5f;
            var ataque = raiz.GetComponent<InimigoAtaque>();
            ataque.animator = animator;
            ataque.alcanceDeAtaque = 2.6f;
            ataque.alcanceDoGolpe = 3f;
            ataque.anguloDoGolpe = 100;
            ataque.dano = 25;
            ataque.atrasoDoDano = .65f;
            ataque.duracaoDaAnimacao = ((AnimationClip)controller.layers[0].stateMachine.states.Single(s => s.state.name == "GolpeDeEspada").state.motion).length / .8f + .12f;
            ataque.tempoEntreAtaques = 1.2f;
            raiz.AddComponent<PigManAnimador>().animator = animator;
            ConfigurarSons(raiz);
            barra.transform.localPosition = new Vector3(0, 3.8f, 0);
            barra.transform.localScale = Vector3.one * .065f;
            barra.gameObject.AddComponent<OlharParaCamera>();
            barra.barra.fillRect.GetComponent<Image>().color = new Color(.7f, .12f, .08f);
            PrefabUtility.SaveAsPrefabAsset(raiz, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(raiz); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
    }

    static void AtualizarHordas(GameObject miniBoss)
    {
        foreach (string caminho in Cenas)
        {
            var cena = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(caminho);
            bool abriu = !cena.isLoaded;
            if (abriu) cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive);
            try
            {
                string backup = Relatorios + "/" + Path.GetFileNameWithoutExtension(caminho) + ".antes.unity";
                if (!File.Exists(backup)) File.Copy(caminho, backup);
                var hordas = cena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GerenciadorDeOrdas>(true)).Single();
                if (hordas.ordas.Length != 2) throw new InvalidOperationException("Esperadas duas hordas em " + caminho);
                for (int i = 0; i < hordas.ordas.Length; i++)
                {
                    var inimigos = hordas.ordas[i].prefabsInimigos.Where(p => p != miniBoss).ToList();
                    if (i == 1) inimigos.Add(miniBoss);
                    hordas.ordas[i].prefabsInimigos = inimigos.ToArray();
                }
                EditorUtility.SetDirty(hordas);
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }
            finally { if (abriu) EditorSceneManager.CloseScene(cena, true); }
        }
    }

    [MenuItem("Derby/PigMan/Criar mini-boss e configurar hordas")]
    public static void Instalar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play Mode antes de instalar o PigMan.");
        Directory.CreateDirectory(Relatorios);
        ConfigurarAvatar();
        var materiais = CriarMateriais();
        var controller = CriarController();
        AssetDatabase.SaveAssets();
        var prefab = CriarPrefab(controller, materiais);
        AtualizarHordas(prefab);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Relatorios + "/instalacao.txt", "PigManMiniBoss: vida=500; velocidade=2 (porcos=3.5); dano=25; pontos=150.\nHorda 1: 5 porcos. Horda 2: 10 porcos + 1 PigMan em cada fase.\n");
        Debug.Log("[PigMan] Mini-boss criado e incluido na horda 2 do estadio e da rua.");
    }

}

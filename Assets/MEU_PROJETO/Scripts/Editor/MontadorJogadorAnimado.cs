using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MontadorJogadorAnimado
{
    const string CaminhoModelo = "Assets/MEU_PROJETO/Modelos/Character/CharacterAnimado.fbx";
    const string CaminhoController = "Assets/MEU_PROJETO/Animacoes/Jogador.controller";
    const string CaminhoPrefab = "Assets/MEU_PROJETO/Prefabs/Jogador.prefab";
    const string CaminhoBackup = "Assets/MEU_PROJETO/Prefabs/Jogador_backup.prefab";
    const string CaminhoMascara = "Assets/MEU_PROJETO/Animacoes/MascaraBracos.mask";
    const string NomeFilho = "PersonagemAnimado";

    const float AlturaLocal = 2f;
    const float ComprimentoArmaMetros = 0.30f;
    const float CameraAvancoLocal = 0.20f;

    static readonly string[] AcoesBlender = { "Robber Idle", "Robber Walk", "Robber Run", "Robber JumpStart", "Robber JumpAir", "Robber JumpLand" };
    static readonly string[] NomesClipe = { "Idle", "Walk", "Run", "JumpStart", "JumpAir", "JumpLand" };
    static readonly bool[] Loop = { true, true, true, false, true, false };

    [MenuItem("Ferramentas/Jogador Animado/Fazer tudo (1 + 2 + 3)")]
    public static void FazerTudo()
    {
        if (!ConfigurarImportacao()) return;
        if (!CriarController()) return;
        MontarPrefab();
    }

    [MenuItem("Ferramentas/Jogador Animado/1 - Configurar importação do Character")]
    public static void Passo1() { ConfigurarImportacao(); }

    [MenuItem("Ferramentas/Jogador Animado/2 - Criar Animator Controller")]
    public static void Passo2() { CriarController(); }

    [MenuItem("Ferramentas/Jogador Animado/3 - Montar prefab do Jogador")]
    public static void Passo3() { MontarPrefab(); }

    static bool ConfigurarImportacao()
    {
        AssetDatabase.ImportAsset(CaminhoModelo, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(CaminhoModelo) as ModelImporter;
        if (imp == null)
        {
            Debug.LogError("[Jogador Animado] Não achei o modelo em " + CaminhoModelo);
            return false;
        }

        imp.animationType = ModelImporterAnimationType.Human;
        imp.importAnimation = true;
        if (imp.avatarSetup == ModelImporterAvatarSetup.NoAvatar)
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

        var padrao = imp.defaultClipAnimations;
        if (padrao == null || padrao.Length == 0)
        {
            Debug.LogError("[Jogador Animado] O Character.blend não trouxe nenhuma animação. " +
                           "Confirme que o Blender está instalado/configurado na Unity e reimporte o arquivo.");
            return false;
        }

        var lista = new List<ModelImporterClipAnimation>();
        var encontrados = new HashSet<string>();
        foreach (var c in padrao)
        {
            int idx = -1;
            for (int i = 0; i < AcoesBlender.Length; i++)
            {
                if (c.name.EndsWith(AcoesBlender[i])) { idx = i; break; }
            }

            if (idx >= 0)
            {
                c.name = NomesClipe[idx];
                c.loopTime = Loop[idx];
                c.loopPose = Loop[idx];
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = false;
                encontrados.Add(NomesClipe[idx]);
            }
            lista.Add(c);
        }

        var faltando = NomesClipe.Where(n => !encontrados.Contains(n)).ToArray();
        if (faltando.Length > 0)
        {
            Debug.LogError("[Jogador Animado] Faltam clipes: " + string.Join(", ", faltando) +
                           ". Clipes encontrados no arquivo: " + string.Join(", ", padrao.Select(p => p.name)));
            return false;
        }

        imp.clipAnimations = lista.ToArray();
        imp.SaveAndReimport();
        Debug.Log("[Jogador Animado] Importação configurada (Humanoid + 6 clipes).");
        return true;
    }

    static bool CriarController()
    {
        var clipes = new Dictionary<string, AnimationClip>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(CaminhoModelo))
        {
            var c = o as AnimationClip;
            if (c != null && !c.name.StartsWith("__preview__")) clipes[c.name] = c;
        }
        foreach (var n in NomesClipe)
        {
            if (!clipes.ContainsKey(n))
            {
                Debug.LogError("[Jogador Animado] Clipe '" + n + "' não encontrado. Rode o passo 1 primeiro.");
                return false;
            }
        }

        float andar = 5f, correr = 6.5f;
        var prefabAtual = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
        if (prefabAtual != null)
        {
            var mov = prefabAtual.GetComponent<JogadorMovimento>();
            if (mov != null) { andar = mov.velocidadeAndar; correr = mov.velocidadeCorrer; }
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CaminhoController) != null)
            AssetDatabase.DeleteAsset(CaminhoController);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CaminhoController);

        ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("NoChao", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Pular", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;

        BlendTree arvore;
        var loco = ctrl.CreateBlendTreeInController("Locomocao", out arvore);
        arvore.blendType = BlendTreeType.Simple1D;
        arvore.blendParameter = "Velocidade";
        arvore.useAutomaticThresholds = false;
        arvore.AddChild(clipes["Idle"], 0f);
        arvore.AddChild(clipes["Walk"], andar);
        arvore.AddChild(clipes["Run"], correr);
        sm.defaultState = loco;

        var puloInicio = sm.AddState("PuloInicio"); puloInicio.motion = clipes["JumpStart"];
        var noAr = sm.AddState("NoAr");             noAr.motion = clipes["JumpAir"];
        var pouso = sm.AddState("Pouso");           pouso.motion = clipes["JumpLand"];

        var t = loco.AddTransition(puloInicio);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "Pular");

        t = loco.AddTransition(noAr);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "NoChao");

        t = puloInicio.AddTransition(noAr);
        t.hasExitTime = true; t.exitTime = 0.95f; t.hasFixedDuration = true; t.duration = 0.05f;

        t = noAr.AddTransition(pouso);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "NoChao");

        t = pouso.AddTransition(puloInicio);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "Pular");

        t = pouso.AddTransition(noAr);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "NoChao");

        t = pouso.AddTransition(loco);
        t.hasExitTime = true; t.exitTime = 0.9f; t.hasFixedDuration = true; t.duration = 0.1f;

        var mascara = AssetDatabase.LoadAssetAtPath<AvatarMask>(CaminhoMascara);
        if (mascara == null)
        {
            mascara = new AvatarMask();
            AssetDatabase.CreateAsset(mascara, CaminhoMascara);
        }
        foreach (AvatarMaskBodyPart parte in System.Enum.GetValues(typeof(AvatarMaskBodyPart)))
        {
            if (parte == AvatarMaskBodyPart.LastBodyPart) continue;
            bool ativo = parte == AvatarMaskBodyPart.LeftArm || parte == AvatarMaskBodyPart.RightArm
                      || parte == AvatarMaskBodyPart.LeftFingers || parte == AvatarMaskBodyPart.RightFingers
                      || parte == AvatarMaskBodyPart.LeftHandIK || parte == AvatarMaskBodyPart.RightHandIK;
            mascara.SetHumanoidBodyPartActive(parte, ativo);
        }
        EditorUtility.SetDirty(mascara);

        ctrl.AddLayer("Bracos");
        var camadas = ctrl.layers;
        var camadaBracos = camadas[camadas.Length - 1];
        camadaBracos.avatarMask = mascara;
        camadaBracos.defaultWeight = 1f;
        camadaBracos.blendingMode = AnimatorLayerBlendingMode.Override;
        var estadoBracos = camadaBracos.stateMachine.AddState("IdleBracos");
        estadoBracos.motion = clipes["Idle"];
        camadaBracos.stateMachine.defaultState = estadoBracos;
        ctrl.layers = camadas;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("[Jogador Animado] Animator Controller criado em " + CaminhoController +
                  " (andar=" + andar + ", correr=" + correr + ").");
        return true;
    }

    static void MontarPrefab()
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoModelo);
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CaminhoController);
        if (modelo == null || ctrl == null)
        {
            Debug.LogError("[Jogador Animado] Rode os passos 1 e 2 antes do passo 3.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoBackup) == null)
            AssetDatabase.CopyAsset(CaminhoPrefab, CaminhoBackup);

        var raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
        try
        {
            var antigo = raiz.transform.Find(NomeFilho);
            if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
            go.name = NomeFilho;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Transform hips = Achar(go.transform, "mixamorig:Hips");
            Transform topo = Achar(go.transform, "mixamorig:HeadTop_End");
            Transform cabeca = Achar(go.transform, "mixamorig:Head");
            Transform mao = Achar(go.transform, "mixamorig:RightHand");
            Transform pe = Achar(go.transform, "mixamorig:LeftFoot");
            Transform dedo = Achar(go.transform, "mixamorig:LeftToeBase");
            if (hips == null || topo == null || cabeca == null || mao == null || pe == null || dedo == null)
            {
                Debug.LogError("[Jogador Animado] Não encontrei os ossos mixamorig:* no modelo instanciado.");
                return;
            }

            Vector3 frente = Vector3.ProjectOnPlane(dedo.position - pe.position, Vector3.up);
            Transform quadrilEsq = Achar(go.transform, "mixamorig:LeftUpLeg");
            Transform quadrilDir = Achar(go.transform, "mixamorig:RightUpLeg");
            if (quadrilEsq != null && quadrilDir != null)
            {
                Vector3 lado = Vector3.ProjectOnPlane(quadrilDir.position - quadrilEsq.position, Vector3.up);
                Vector3 frenteQuadril = Vector3.Cross(lado, Vector3.up);
                if (frenteQuadril.sqrMagnitude > 1e-6f && Vector3.Dot(frenteQuadril, frente) > 0f)
                    frente = frenteQuadril;
            }
            if (frente.sqrMagnitude > 1e-6f)
            {
                float ang = Vector3.SignedAngle(frente, raiz.transform.forward, Vector3.up);
                go.transform.rotation = Quaternion.Euler(0f, ang, 0f) * go.transform.rotation;
            }

            float chao = MenorY(go.transform);
            float alturaMundo = topo.position.y - chao;
            float alvoMundo = AlturaLocal * raiz.transform.lossyScale.y;
            if (alturaMundo > 1e-4f)
                go.transform.localScale = Vector3.one * (alvoMundo / alturaMundo);

            chao = MenorY(go.transform);
            Vector3 desloc = new Vector3(
                raiz.transform.position.x - hips.position.x,
                raiz.transform.position.y - chao,
                raiz.transform.position.z - hips.position.z);
            go.transform.position += desloc;

            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            if (anim.avatar == null)
                anim.avatar = AssetDatabase.LoadAllAssetsAtPath(CaminhoModelo).OfType<Avatar>().FirstOrDefault();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var skins = raiz.transform.Find("Skins");
            if (skins != null)
                foreach (var r in skins.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            Transform arma = Achar(raiz.transform, "arma_papelao");
            Transform bracoEsq = Achar(raiz.transform, "Braco_esquerdo");
            if (arma != null)
            {
                if (arma.parent != raiz.transform) arma.SetParent(raiz.transform, true);

                float comp = 19.2f;
                Transform corpoArma = Achar(arma, "Gun_body");
                var mf = corpoArma != null ? corpoArma.GetComponent<MeshFilter>() : null;
                if (mf != null && mf.sharedMesh != null)
                {
                    Vector3 s = mf.sharedMesh.bounds.size;
                    comp = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
                }
                float escala = ComprimentoArmaMetros / (comp * Mathf.Max(raiz.transform.lossyScale.x, 1e-4f));
                arma.localScale = Vector3.one * escala;
            }
            else
            {
                Debug.LogWarning("[Jogador Animado] arma_papelao não encontrada no prefab.");
            }
            if (bracoEsq != null)
                foreach (var r in bracoEsq.GetComponents<Renderer>()) r.enabled = false;

            Transform cam = Achar(raiz.transform, "Camera");
            if (cam != null)
            {
                Vector3 olhos = Vector3.Lerp(cabeca.position, topo.position, 0.55f);
                Vector3 local = raiz.transform.InverseTransformPoint(olhos);
                cam.localPosition = new Vector3(0f, local.y, local.z + CameraAvancoLocal);
            }

            if (cam != null)
            {
                var camComp = cam.GetComponent<Camera>();
                if (camComp != null) camComp.nearClipPlane = 0.05f;
            }

            var ja = raiz.GetComponent<JogadorAnimador>();
            if (ja == null) ja = raiz.AddComponent<JogadorAnimador>();
            ja.animator = anim;
            ja.pulo = raiz.GetComponent<JogadorPulo>();

            var tronco = raiz.GetComponent<JogadorTroncoCamera>();
            if (tronco == null) tronco = raiz.AddComponent<JogadorTroncoCamera>();
            tronco.animator = anim;
            tronco.cameraJogador = cam;

            var an = raiz.GetComponent<ArmaNaMao>();
            if (an == null) an = raiz.AddComponent<ArmaNaMao>();
            an.arma = arma;
            an.mao = mao;
            an.cameraJogador = cam;

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
            Debug.Log("[Jogador Animado] Prefab do Jogador atualizado. Backup em " + CaminhoBackup);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    static Transform Achar(Transform raiz, string nome)
    {
        if (raiz.name == nome) return raiz;
        foreach (Transform filho in raiz)
        {
            var r = Achar(filho, nome);
            if (r != null) return r;
        }
        return null;
    }

    static float MenorY(Transform modelo)
    {
        string[] nomes = { "mixamorig:LeftFoot", "mixamorig:RightFoot", "mixamorig:LeftToeBase", "mixamorig:RightToeBase",
                           "mixamorig:LeftToe_End", "mixamorig:RightToe_End" };
        float menor = float.MaxValue;
        foreach (var n in nomes)
        {
            var t = Achar(modelo, n);
            if (t != null) menor = Mathf.Min(menor, t.position.y);
        }
        return menor == float.MaxValue ? modelo.position.y : menor;
    }
}

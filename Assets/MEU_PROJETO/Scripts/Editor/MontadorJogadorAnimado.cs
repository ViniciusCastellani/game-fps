using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Menu: Ferramentas > Jogador Animado
//  1) Configura a importação do Character.blend (Humanoid + loops dos clipes)
//  2) Cria o Animator Controller (Idle / Walk / Run / Pulo)
//  3) Monta o prefab do Jogador com o personagem animado e a arma na mão
public static class MontadorJogadorAnimado
{
    const string CaminhoModelo = "Assets/MEU_PROJETO/Modelos/Character/CharacterAnimado.fbx";
    const string CaminhoController = "Assets/MEU_PROJETO/Animacoes/Jogador.controller";
    const string CaminhoPrefab = "Assets/MEU_PROJETO/Prefabs/Jogador.prefab";
    const string CaminhoBackup = "Assets/MEU_PROJETO/Prefabs/Jogador_backup.prefab";
    const string CaminhoMascara = "Assets/MEU_PROJETO/Animacoes/MascaraBracos.mask";
    const string NomeFilho = "PersonagemAnimado";

    // Altura do personagem em unidades LOCAIS do prefab (o CapsuleCollider do Jogador tem altura 2)
    const float AlturaLocal = 2f;
    // Comprimento da arma no mundo, em metros
    const float ComprimentoArmaMetros = 0.30f;
    // Posição da câmera em relação à cabeça (unidades locais do prefab)
    const float CameraAvancoLocal = 0.20f;

    // nome da ação no Blender -> nome do clipe na Unity, loop?
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

    // ------------------------------------------------------------------ 1
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
                // o movimento é feito pela física (Rigidbody), então travamos o root dentro da pose
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true;
                c.keepOriginalPositionY = true;
                c.keepOriginalPositionXZ = false; // Center of Mass: mantém o corpo centrado no eixo do Jogador
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

    // ------------------------------------------------------------------ 2
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

        // velocidades reais do jogador (o prefab sobrescreve os valores padrão do script)
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

        // Locomoção: Idle (0) -> Walk (andar) -> Run (correr)
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

        // Locomoção -> pulo (gatilho) ; precisa vir ANTES da transição de queda
        var t = loco.AddTransition(puloInicio);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "Pular");

        // Locomoção -> no ar (caiu de uma borda)
        t = loco.AddTransition(noAr);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "NoChao");

        // Início do pulo -> no ar (ao terminar o clipe)
        t = puloInicio.AddTransition(noAr);
        t.hasExitTime = true; t.exitTime = 0.95f; t.hasFixedDuration = true; t.duration = 0.05f;

        // No ar -> pouso (tocou o chão)
        t = noAr.AddTransition(pouso);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "NoChao");

        // Pouso -> novo pulo imediato
        t = pouso.AddTransition(puloInicio);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "Pular");

        // Pouso -> volta a cair
        t = pouso.AddTransition(noAr);
        t.hasExitTime = false; t.hasFixedDuration = true; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "NoChao");

        // Pouso -> locomoção (ao terminar o clipe)
        t = pouso.AddTransition(loco);
        t.hasExitTime = true; t.exitTime = 0.9f; t.hasFixedDuration = true; t.duration = 0.1f;

        // Camada "Bracos": mantém os braços na pose do Idle (arma levantada) ao andar, correr e pular.
        // A máscara libera só braços/mãos; pernas e tronco continuam com a camada base.
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

    // ------------------------------------------------------------------ 3
    static void MontarPrefab()
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoModelo);
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CaminhoController);
        if (modelo == null || ctrl == null)
        {
            Debug.LogError("[Jogador Animado] Rode os passos 1 e 2 antes do passo 3.");
            return;
        }

        // backup de segurança do prefab original (só na primeira vez)
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoBackup) == null)
            AssetDatabase.CopyAsset(CaminhoPrefab, CaminhoBackup);

        var raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
        try
        {
            // remove montagem anterior (permite rodar de novo)
            var antigo = raiz.transform.Find(NomeFilho);
            if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

            // ---- instancia o personagem
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

            // 1) orienta para que a frente do personagem coincida com a frente (+Z) do Jogador
            // a frente vem da linha dos quadris (os pés ficam abertos no Idle e davam ~8 graus de erro);
            // a direção dos pés só decide o sentido e serve de reserva
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

            // 2) escala para a altura do CapsuleCollider
            float chao = MenorY(go.transform);
            float alturaMundo = topo.position.y - chao;
            float alvoMundo = AlturaLocal * raiz.transform.lossyScale.y;
            if (alturaMundo > 1e-4f)
                go.transform.localScale = Vector3.one * (alvoMundo / alturaMundo);

            // 3) centraliza (quadril no eixo do Jogador) e apoia os pés no chão (y = 0 local)
            chao = MenorY(go.transform);
            Vector3 desloc = new Vector3(
                raiz.transform.position.x - hips.position.x,
                raiz.transform.position.y - chao,
                raiz.transform.position.z - hips.position.z);
            go.transform.position += desloc;

            // ---- Animator
            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            if (anim.avatar == null)
                anim.avatar = AssetDatabase.LoadAllAssetsAtPath(CaminhoModelo).OfType<Avatar>().FirstOrDefault();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // ---- esconde o corpo/braços antigos (sem apagar nada)
            var skins = raiz.transform.Find("Skins");
            if (skins != null)
                foreach (var r in skins.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            // ---- arma
            Transform arma = Achar(raiz.transform, "arma_papelao");
            Transform bracoEsq = Achar(raiz.transform, "Braco_esquerdo");
            if (arma != null)
            {
                if (arma.parent != raiz.transform) arma.SetParent(raiz.transform, true);

                float comp = 19.2f; // comprimento do mesh em unidades originais do modelo
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

            // ---- câmera na altura dos olhos
            Transform cam = Achar(raiz.transform, "Camera");
            if (cam != null)
            {
                Vector3 olhos = Vector3.Lerp(cabeca.position, topo.position, 0.55f);
                Vector3 local = raiz.transform.InverseTransformPoint(olhos);
                cam.localPosition = new Vector3(0f, local.y, local.z + CameraAvancoLocal);
            }

            // near clip pequeno: sem isso o braço/arma de perto são cortados pela câmera
            if (cam != null)
            {
                var camComp = cam.GetComponent<Camera>();
                if (camComp != null) camComp.nearClipPlane = 0.05f;
            }

            // ---- scripts de animação
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

    // ------------------------------------------------------------------ util
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

    // menor altura (mundo) entre os ossos dos pés/dedos
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

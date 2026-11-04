using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class AplicadorParedeDerby
{
    const string Cena = "Assets/DerbySegundoTempo/Cenas/Fases/Rua_TPS.unity";
    const string Textura = "Assets/DerbySegundoTempo/Cenarios/Rua/Texturas/Parede_Derby_Corinthians_Palmeiras.png";
    const string Material = "Assets/DerbySegundoTempo/Cenarios/Rua/Materiais/ParedeDerbyPichacao.mat";
    const string Relatorio = "Library/ParedeRua";
    static double proximaVerificacao;

    [InitializeOnLoadMethod]
    static void Inicializar() { EditorApplication.update += VerificarPedido; }

    static void VerificarPedido()
    {
        if (EditorApplication.timeSinceStartup < proximaVerificacao) return;
        proximaVerificacao = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Relatorio + ".request") || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Relatorio + ".request");
        try { Aplicar(); }
        catch (Exception erro)
        { Directory.CreateDirectory(Relatorio); File.WriteAllText(Relatorio + "/erro.txt", erro.ToString()); Debug.LogException(erro); }
    }

    [MenuItem("Derby/Rua TPS/Aplicar pichacao no bloqueio 70")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Aplique fora do Play Mode.");
        Directory.CreateDirectory(Relatorio);
        File.Copy(Cena, Relatorio + "/RuaAntesDaTextura.unity", true);
        AssetDatabase.ImportAsset(Textura, ImportAssetOptions.ForceSynchronousImport);
        var importador = (TextureImporter)AssetImporter.GetAtPath(Textura);
        importador.textureType = TextureImporterType.Default;
        importador.sRGBTexture = true;
        importador.alphaSource = TextureImporterAlphaSource.None;
        importador.wrapMode = TextureWrapMode.Clamp;
        importador.filterMode = FilterMode.Trilinear;
        importador.anisoLevel = 4;
        importador.mipmapEnabled = true;
        importador.maxTextureSize = 2048;
        importador.textureCompression = TextureImporterCompression.CompressedHQ;
        importador.SaveAndReimport();
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(Textura);
        var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(Material);
        if (material == null)
        {
            material = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ParedeDerbyPichacao" };
            AssetDatabase.CreateAsset(material, Material);
        }
        material.SetTexture("_BaseMap", textura);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", .06f);
        material.SetFloat("_Metallic", 0);
        // A face -Z do cubo nativo tem o UV girado em relacao a frente da rua.
        material.SetTextureScale("_BaseMap", new Vector2(-1, -1));
        material.SetTextureOffset("_BaseMap", Vector2.one);
        EditorUtility.SetDirty(material);
        var cena = SceneManager.GetSceneByPath(Cena);
        bool abriu = !cena.isLoaded;
        if (abriu) cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Additive);
        try
        {
            var parede = cena.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .Single(x => x.name == "BloqueioDaRua_70");
            string transformacao = EditorJsonUtility.ToJson(parede);
            var collider = parede.GetComponent<BoxCollider>();
            string colisao = EditorJsonUtility.ToJson(collider);
            var malha = parede.GetComponent<MeshFilter>().sharedMesh;
            parede.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (transformacao != EditorJsonUtility.ToJson(parede) || colisao != EditorJsonUtility.ToJson(collider)
                || parede.GetComponent<MeshFilter>().sharedMesh != malha)
                throw new InvalidOperationException("A textura deve preservar a geometria e o colisor.");
            EditorUtility.SetDirty(parede.GetComponent<MeshRenderer>());
            EditorSceneManager.MarkSceneDirty(cena);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(cena);
            File.WriteAllText(Relatorio + "/status.txt", "Material aplicado somente em BloqueioDaRua_70.\n"
                + "Posicao local: " + parede.localPosition.ToString("F5") + "\nEscala local: " + parede.localScale.ToString("F5")
                + "\nTransformacao, malha e BoxCollider preservados.\nTextura: " + Textura + "\n");
        }
        finally { if (abriu) EditorSceneManager.CloseScene(cena, true); }
        Capturar();
        File.Delete(Relatorio + "/erro.txt");
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
                foreach (var audio in raiz.GetComponentsInChildren<AudioListener>(true)) audio.enabled = false;
            }
            var go = new GameObject("CameraPreviaParedeDerby");
            SceneManager.MoveGameObjectToScene(go, cena);
            var captura = go.AddComponent<Camera>();
            captura.scene = cena;
            captura.enabled = false;
            captura.fieldOfView = 55;
            captura.farClipPlane = 300;
            captura.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            alvo = new RenderTexture(1440, 900, 24);
            alvo.Create();
            Foto(captura, alvo, new Vector3(0, 11.5f, 38), new Vector3(0, 10.94f, 70), "parede_derby");
            Foto(captura, alvo, new Vector3(0, 3.2f, -23), new Vector3(0, 5, 70), "rua_parede_derby");
        }
        finally
        {
            if (alvo != null) { alvo.Release(); Object.DestroyImmediate(alvo); }
            EditorSceneManager.ClosePreviewScene(cena);
        }
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
}

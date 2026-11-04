using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PreviaRuaTPS
{
    static double proximaVerificacao;
    [InitializeOnLoadMethod]
    static void Inicializar() { EditorApplication.update += Verificar; }

    static void Verificar()
    {
        if (EditorApplication.timeSinceStartup < proximaVerificacao) return;
        proximaVerificacao = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists("Library/Organizacao.preview") || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Library/Organizacao.preview");
        try { Capturar(); }
        catch (Exception erro) { File.WriteAllText("Library/Organizacao/previa-erro.txt", erro.ToString()); Debug.LogException(erro); }
    }

    [MenuItem("Derby/Projeto/Gerar previas da Rua TPS")]
    public static void Capturar()
    {
        Directory.CreateDirectory("Docs/Previews/Rua");
        var cena = EditorSceneManager.OpenPreviewScene("Assets/DerbySegundoTempo/Cenas/Fases/Rua_TPS.unity");
        RenderTexture alvo = null;
        try
        {
            foreach (var raiz in cena.GetRootGameObjects())
            {
                foreach (var camera in raiz.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (var canvas in raiz.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
                foreach (var listener in raiz.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
            var go = new GameObject("CameraPreviaRua");
            SceneManager.MoveGameObjectToScene(go, cena);
            var captura = go.AddComponent<Camera>();
            captura.enabled = false;
            captura.scene = cena;
            captura.fieldOfView = 65;
            captura.farClipPlane = 300;
            captura.clearFlags = CameraClearFlags.Skybox;
            captura.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            alvo = new RenderTexture(1440, 900, 24);
            alvo.Create();
            Foto(captura, alvo, new Vector3(0, 3.2f, -23), new Vector3(0, 3, 20), "rua");
            Foto(captura, alvo, new Vector3(42, 48, -42), new Vector3(0, 0, 18), "visao_geral");
            File.WriteAllText("Library/Organizacao/previa-status.txt", "Duas previas da rua geradas pelo Unity.\n");
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

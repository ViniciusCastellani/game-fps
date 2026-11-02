using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ValidadorIluminacaoPorOrda
{
    static string Pasta => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/IluminacaoPorOrda"));
    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        if (File.Exists(Pasta + ".preview.request")) EditorApplication.delayCall += Executar;
    }
    static void Executar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Executar;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Pasta + ".preview.request");
        try { Capturar(); }
        catch (Exception e) { Directory.CreateDirectory(Pasta); File.WriteAllText(Pasta + "/preview-error.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Derby/Cenario TPS/Comparar iluminacao das hordas")]
    public static void Capturar()
    {
        Directory.CreateDirectory(Pasta);
        var ceu = RenderSettings.skybox;
        var sol = RenderSettings.sun;
        var modo = RenderSettings.ambientMode;
        var a = RenderSettings.ambientSkyColor;
        var b = RenderSettings.ambientEquatorColor;
        var c = RenderSettings.ambientGroundColor;
        var cena = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Estadio_TPS_Backup_Visual.unity");
        RenderTexture alvo = null;
        try
        {
            var raizes = cena.GetRootGameObjects();
            foreach (var raiz in raizes)
            {
                foreach (var cam in raiz.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
                foreach (var canvas in raiz.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
                foreach (var listener in raiz.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
            var controle = raizes.SelectMany(x => x.GetComponentsInChildren<IluminacaoPorOrda>(true)).Single();
            var hordas = raizes.SelectMany(x => x.GetComponentsInChildren<GerenciadorDeOrdas>(true)).Single();
            if (hordas.iluminacao != controle) throw new InvalidOperationException("Referencia de iluminacao ausente no gerenciador de hordas.");
            var relatorio = new StringBuilder();
            relatorio.AppendLine("Cena: " + cena.path + "; referencia das hordas OK; controle ativo=" + controle.isActiveAndEnabled);
            foreach (var material in raizes.SelectMany(x => x.GetComponentsInChildren<Renderer>(true))
                .SelectMany(x => x.sharedMaterials).Where(x => x != null).Distinct())
                relatorio.AppendLine(material.name + " | " + material.shader.name);
            var objeto = new GameObject("CameraComparacaoHordas");
            SceneManager.MoveGameObjectToScene(objeto, cena);
            var camera = objeto.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = cena;
            camera.fieldOfView = 65;
            camera.farClipPlane = 350;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            alvo = new RenderTexture(1440, 900, 24);
            alvo.Create();
            camera.targetTexture = alvo;
            var captura = typeof(PreviaCenarioTPS).GetMethod("Shot", BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 1; i <= 2; i++)
            {
                controle.AplicarOrda(i, true);
                relatorio.AppendLine($"Horda {i}: sol={controle.sol.intensity}; cor={controle.sol.color}; rotacao={controle.sol.transform.eulerAngles}");
                captura.Invoke(null, new object[] { camera, alvo, "horda_" + i, new Vector3(10, -19.8f, 3896), new Vector3(8, -21.3f, 3874) });
                File.Copy(Path.Combine(Application.dataPath, "../Library/CenarioTPSPreview/horda_" + i + ".png"), Pasta + "/horda_" + i + ".png", true);
            }
            File.WriteAllText(Pasta + "/preview.txt", relatorio.ToString());
        }
        finally
        {
            if (alvo != null) { alvo.Release(); UnityEngine.Object.DestroyImmediate(alvo); }
            EditorSceneManager.ClosePreviewScene(cena);
            RenderSettings.skybox = ceu;
            RenderSettings.sun = sol;
            RenderSettings.ambientMode = modo;
            RenderSettings.ambientSkyColor = a;
            RenderSettings.ambientEquatorColor = b;
            RenderSettings.ambientGroundColor = c;
        }
    }
}

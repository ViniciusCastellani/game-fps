using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class InstaladorIluminacaoPorOrda
{
    const string Cena = "Assets/Scenes/Estadio_TPS_Backup_Visual.unity";
    static string Pasta => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/IluminacaoPorOrda"));

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        if (File.Exists(Pasta + ".request")) EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Executar;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Pasta);
        File.Delete(Pasta + ".request");
        try { Instalar(); }
        catch (Exception erro) { File.WriteAllText(Pasta + "/error.txt", erro.ToString()); Debug.LogException(erro); }
    }

    [MenuItem("Derby/Cenario TPS/Instalar iluminacao por orda")]
    public static void Instalar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play Mode antes de instalar.");
        Directory.CreateDirectory(Pasta);
        File.Copy(Cena, Pasta + "/CenaAntesDaIluminacao.unity", true);
        var cena = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Cena);
        bool abriu = !cena.isLoaded;
        if (abriu) cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Additive);
        try
        {
            var raizes = cena.GetRootGameObjects();
            var hordas = raizes.SelectMany(x => x.GetComponentsInChildren<GerenciadorDeOrdas>(true)).Single();
            var sol = raizes.SelectMany(x => x.GetComponentsInChildren<Light>(true)).Single(x => x.type == LightType.Directional);
            var controle = sol.GetComponent<IluminacaoPorOrda>();
            if (controle == null) controle = sol.gameObject.AddComponent<IluminacaoPorOrda>();
            controle.sol = sol;
            controle.gerenciadorDeOrdas = hordas;
            hordas.iluminacao = controle;
            sol.useColorTemperature = false;
            sol.color = controle.dia.corSol;
            sol.intensity = controle.dia.intensidadeSol;
            sol.transform.rotation = Quaternion.Euler(controle.dia.rotacaoSol);
            // RenderSettings pertence a cena ativa. Preserva a selecao do usuario.
            var ativa = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(cena);
            try
            {
                RenderSettings.sun = sol;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = controle.dia.ceu;
                RenderSettings.ambientEquatorColor = controle.dia.horizonte;
                RenderSettings.ambientGroundColor = controle.dia.solo;
                EditorUtility.SetDirty(controle);
                EditorUtility.SetDirty(hordas);
                EditorUtility.SetDirty(sol);
                EditorUtility.SetDirty(sol.transform);
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }
            finally { UnityEngine.SceneManagement.SceneManager.SetActiveScene(ativa); }
            File.WriteAllText(Pasta + "/status.txt", "Instalado na cena de backup: horda 1 dia; horda 2+ tarde; transicao 3s; reinicio direto preserva o periodo.\n");
        }
        finally { if (abriu) EditorSceneManager.CloseScene(cena, true); }
    }
}

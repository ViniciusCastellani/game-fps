using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InspecaoOrganizacao
{
    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        if (File.Exists("Library/Organizacao.inspect")) EditorApplication.delayCall += Inspecionar;
    }

    static void Inspecionar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Inspecionar; return; }
        File.Delete("Library/Organizacao.inspect");
        var report = new StringBuilder();
        try
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                report.AppendLine(scene.path + " dirty=" + scene.isDirty);
                foreach (var root in scene.GetRootGameObjects()) Descrever(root.transform, report, 0);
            }
            foreach (var path in AssetDatabase.GetAllAssetPaths().Where(x => x.Contains("/Prefabs/") && x.EndsWith(".prefab") && !x.Contains("/Packages/")))
                report.AppendLine("PREFAB " + path);
            foreach (var path in AssetDatabase.GetAllAssetPaths().Where(x => x.Contains("street-city") && x.EndsWith(".fbx")))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var bounds = new Bounds(); bool first = true;
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
                report.AppendLine("CITY " + path + " bounds=" + bounds + " renderers=" + model.GetComponentsInChildren<Renderer>(true).Length);
            }
        }
        catch (Exception error) { report.AppendLine(error.ToString()); }
        File.WriteAllText("Library/Organizacao-inspecao.txt", report.ToString());
    }

    static void Descrever(Transform item, StringBuilder report, int depth)
    {
        if (depth > 2) return;
        report.AppendLine(new string(' ', depth * 2) + item.name + " pos=" + item.position + " components=" + string.Join(",", item.GetComponents<Component>().Select(x => x == null ? "MISSING" : x.GetType().Name)));
        foreach (Transform child in item) Descrever(child, report, depth + 1);
    }
}

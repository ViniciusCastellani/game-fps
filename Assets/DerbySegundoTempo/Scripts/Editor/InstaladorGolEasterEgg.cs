using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstaladorGolEasterEgg
{
    const string Cena = "Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity";
    static string Pasta => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/GolEasterEgg"));

    [InitializeOnLoadMethod]
    static void Inicializar()
    {
        if (File.Exists(Pasta + ".request")) EditorApplication.delayCall += ExecutarPedido;
    }

    static void ExecutarPedido()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ExecutarPedido;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Pasta + ".request");
        Directory.CreateDirectory(Pasta);
        try { Instalar(); }
        catch (Exception erro) { File.WriteAllText(Pasta + "/error.txt", erro.ToString()); Debug.LogException(erro); }
    }

    [MenuItem("Derby/Bola de futebol/Instalar easter egg de gol na cena de backup")]
    public static void Instalar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play Mode antes de instalar.");
        Directory.CreateDirectory(Pasta);
        File.Copy(Cena, Pasta + "/CenaAntesDoGol.unity", true);
        var cena = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Cena);
        bool abriu = !cena.isLoaded;
        if (abriu) cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Additive);
        try
        {
            var raizes = cena.GetRootGameObjects();
            var bola = raizes.SelectMany(x => x.GetComponentsInChildren<BolaFutebol>(true)).Single();
            var pontos = raizes.SelectMany(x => x.GetComponentsInChildren<SistemaDePontos>(true)).Single();
            var gol = raizes.SelectMany(x => x.GetComponentsInChildren<GolEasterEgg>(true)).SingleOrDefault();
            if (gol == null)
            {
                var raiz = new GameObject("EasterEgg_Gol");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(raiz, cena);
                gol = raiz.AddComponent<GolEasterEgg>();
                var centro = new GameObject("CentroDoCampo_RetornoBola");
                centro.transform.SetParent(raiz.transform, false);
                // Media dos quatro cantos das linhas do gramado; raio da bola + folga sobre o piso.
                centro.transform.position = new Vector3(11.34673f, -22.57699f, 3908.7292f);
                gol.centroDoCampo = centro.transform;
                CriarTrigger(gol, "TriggerGol_Oeste", -50.25f);
                CriarTrigger(gol, "TriggerGol_Leste", 72.6f);
            }
            gol.bola = bola;
            gol.sistemaDePontos = pontos;
            const string materialPath = "Assets/DerbySegundoTempo/Cenarios/Estadio/Materiais/ConfetesGol.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) throw new InvalidOperationException("Shader URP de particulas ausente.");
                material = new Material(shader);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Cull", 0);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            gol.materialConfete = material;
            var audio = gol.GetComponent<AudioSource>();
            if (audio == null) audio = gol.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 0f;
            gol.somVuvuzela = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/DerbySegundoTempo/Cenarios/Estadio/Audio/VuvuzelaGol.wav");
            if (gol.somVuvuzela == null) throw new InvalidOperationException("Audio VuvuzelaGol.wav ausente.");
            EditorUtility.SetDirty(audio);
            EditorUtility.SetDirty(gol);
            EditorSceneManager.MarkSceneDirty(cena);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(cena);
            File.WriteAllText(Pasta + "/status.txt", "Instalado: dois triggers, pontos configuraveis, confetes, vuvuzela e retorno ao centro.\n");
        }
        finally { if (abriu) EditorSceneManager.CloseScene(cena, true); }
    }

    static void CriarTrigger(GolEasterEgg gol, string nome, float x)
    {
        var objeto = new GameObject(nome);
        objeto.transform.SetParent(gol.transform, false);
        // Volumes internos, recuados da linha e menores que a abertura das traves.
        objeto.transform.position = new Vector3(x, -20.25f, 3909.001f);
        var caixa = objeto.AddComponent<BoxCollider>();
        caixa.isTrigger = true;
        caixa.size = new Vector3(1.5f, 5f, 10.2f);
        objeto.AddComponent<TriggerGol>().easterEgg = gol;
    }
}

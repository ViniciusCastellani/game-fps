using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class InstaladorArmaLaser
{
    const string Root = "Assets/DerbySegundoTempo/Armas/Laser";
    const string ScenePath = "Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity";
    static string RequestPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/ArmaLaserInstall.request"));
    static string ReportPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/ArmaLaserInstall"));

    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        if (File.Exists(RequestPath)) EditorApplication.delayCall += RunRequested;
    }

    static void RunRequested()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequested;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(RequestPath);
        Directory.CreateDirectory(ReportPath);
        try { Install(); }
        catch (Exception error) { File.WriteAllText(ReportPath + "/error.txt", error.ToString()); Debug.LogException(error); }
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }

    static Material Material(string name, Color color, bool glow = false, bool beam = false)
    {
        string path = Root + "/Materiais/" + name + ".mat";
        var shader = Shader.Find(beam ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP shader is missing.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        material.SetColor("_BaseColor", color);
        if (!beam)
        {
            material.SetFloat("_Metallic", .05f);
            material.SetFloat("_Smoothness", .35f);
        }
        if (glow)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 3);
        }
        if (beam)
        {
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 2);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Vector3 euler = default)
    {
        var item = GameObject.CreatePrimitive(type);
        SceneManager.MoveGameObjectToScene(item, parent.gameObject.scene);
        item.name = name;
        item.layer = 2;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localRotation = Quaternion.Euler(euler);
        item.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    static Mesh Housing()
    {
        const string path = Root + "/Malhas/CorpoChanfrado.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        var points = new[] {
            new Vector2(-.065f,-.045f),new Vector2(-.04f,-.075f),new Vector2(.04f,-.075f),new Vector2(.075f,-.04f),
            new Vector2(.075f,.04f),new Vector2(.04f,.075f),new Vector2(-.04f,.075f),new Vector2(-.065f,.045f)
        };
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        Action<Vector3,Vector3,Vector3> add = (a,b,c) => { int i=vertices.Count; vertices.Add(a);vertices.Add(b);vertices.Add(c); triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2); };
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a=points[i], b=points[(i+1)%points.Length];
            var a0=new Vector3(-.11f,a.x+.08f,a.y); var b0=new Vector3(-.11f,b.x+.08f,b.y);
            var a1=new Vector3(.22f,a.x+.08f,a.y); var b1=new Vector3(.22f,b.x+.08f,b.y);
            add(a0,b0,b1);add(a0,b1,a1);
            add(new Vector3(-.11f,.08f,0),b0,a0);
            add(new Vector3(.22f,.08f,0),a1,b1);
        }
        mesh = new Mesh { name = "CorpoChanfrado" };
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path);
        return mesh;
    }

    static GameObject BuildPrefab()
    {
        Folder(Root + "/Materiais");Folder(Root + "/Malhas");Folder(Root + "/Prefabs");Folder(Root + "/Audio");
        var white=Material("CarcacaMarfim",new Color(.82f,.84f,.84f));
        var dark=Material("EstruturaGrafite",new Color(.035f,.045f,.055f));
        var metal=Material("MetalFosco",new Color(.16f,.2f,.23f));
        var cyan=Material("EnergiaCiano",new Color(.04f,.8f,1),true);
        var amber=Material("TravaAmbar",new Color(.95f,.57f,.06f));
        var beam=Material("FeixeCiano",new Color(.04f,.85f,1,.72f),false,true);
        var core=Material("NucleoClaro",new Color(.8f,1,1,.95f),false,true);
        Scene preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("ArmaLaser");root.layer=2;
            SceneManager.MoveGameObjectToScene(root,preview);
            var body=new GameObject("CarcacaChanfrada");body.layer=2;
            SceneManager.MoveGameObjectToScene(body,preview);body.transform.SetParent(root.transform,false);
            body.AddComponent<MeshFilter>().sharedMesh=Housing();body.AddComponent<MeshRenderer>().sharedMaterial=white;
            Part(root.transform,"Empunhadura",PrimitiveType.Cube,new Vector3(-.02f,-.055f,0),new Vector3(.085f,.18f,.095f),dark,new Vector3(0,0,-12));
            Part(root.transform,"BaseEmpunhadura",PrimitiveType.Cube,new Vector3(-.04f,-.15f,0),new Vector3(.105f,.025f,.105f),metal);
            Part(root.transform,"TrilhoSuperior",PrimitiveType.Cube,new Vector3(.055f,.17f,0),new Vector3(.29f,.025f,.05f),dark);
            Part(root.transform,"Mira",PrimitiveType.Cube,new Vector3(-.055f,.2f,0),new Vector3(.035f,.04f,.055f),metal);
            Part(root.transform,"IndicadorMira",PrimitiveType.Cube,new Vector3(-.035f,.2f,0),new Vector3(.008f,.012f,.022f),cyan);
            Part(root.transform,"Cano",PrimitiveType.Cylinder,new Vector3(.285f,.08f,0),new Vector3(.105f,.08f,.105f),dark,new Vector3(0,0,90));
            Part(root.transform,"AnelFrontal",PrimitiveType.Cylinder,new Vector3(.365f,.08f,0),new Vector3(.14f,.025f,.14f),white,new Vector3(0,0,90));
            Part(root.transform,"BocalEscuro",PrimitiveType.Cylinder,new Vector3(.394f,.08f,0),new Vector3(.102f,.009f,.102f),dark,new Vector3(0,0,90));
            Part(root.transform,"LenteEmissora",PrimitiveType.Cylinder,new Vector3(.405f,.08f,0),new Vector3(.06f,.003f,.06f),cyan,new Vector3(0,0,90));
            Part(root.transform,"GuardaMatoFrente",PrimitiveType.Cube,new Vector3(.12f,-.035f,0),new Vector3(.025f,.1f,.045f),dark);
            Part(root.transform,"GuardaMatoBase",PrimitiveType.Cube,new Vector3(.07f,-.085f,0),new Vector3(.125f,.022f,.045f),dark);
            Part(root.transform,"Gatilho",PrimitiveType.Cube,new Vector3(.055f,-.02f,0),new Vector3(.02f,.055f,.03f),amber,new Vector3(0,0,-12));
            for(int side=-1;side<=1;side+=2)
            {
                Part(root.transform,"JanelaDeEnergia",PrimitiveType.Cube,new Vector3(.075f,.075f,side*.077f),new Vector3(.18f,.065f,.01f),dark);
                for(int i=0;i<3;i++)
                    Part(root.transform,"CelulaDeEnergia",PrimitiveType.Cube,new Vector3(.015f+i*.055f,.075f,side*.084f),new Vector3(.03f,.043f,.007f),cyan);
                Part(root.transform,"TravaDaBateria",PrimitiveType.Cube,new Vector3(-.075f,.04f,side*.08f),new Vector3(.035f,.035f,.012f),amber);
            }
            var muzzle=new GameObject("PontaDoCano");muzzle.layer=2;
            SceneManager.MoveGameObjectToScene(muzzle,preview);muzzle.transform.SetParent(root.transform,false);muzzle.transform.localPosition=new Vector3(.415f,.08f,0);
            var flash=muzzle.AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(.05f,.8f,1);flash.range=1.3f;flash.intensity=1.2f;flash.shadows=LightShadows.None;flash.enabled=false;
            var emitter=root.AddComponent<EmissorLaser>();emitter.pontaDoCano=muzzle.transform;emitter.materialFeixe=beam;emitter.materialNucleo=core;emitter.luzDoDisparo=flash;
            return PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/ArmaLaser.prefab");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    static AudioClip LaserSound()
    {
        string path=Root+"/Audio/DisparoLaser.wav";
        if (!File.Exists(path))
        {
            const int rate=44100;const int count=5292;
            using (var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                double phase=0;
                for(int i=0;i<count;i++)
                {
                    double t=(double)i/rate;double frequency=260+1400*Math.Exp(-t*28);phase+=2*Math.PI*frequency/rate;
                    double value=(Math.Sin(phase)+.25*Math.Sin(phase*2.01))*Math.Exp(-t*32)*Math.Min(1,t*1200)*.28;
                    writer.Write((short)(value*short.MaxValue));
                }
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    [MenuItem("Derby/Arma laser/Instalar na cena de backup")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before installing the weapon.");
        Directory.CreateDirectory(ReportPath);
        File.Copy(ScenePath,ReportPath+"/CenaAntesDoLaser.unity",true);
        GameObject prefab=BuildPrefab();
        Scene scene=SceneManager.GetSceneByPath(ScenePath);
        bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var follow=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<ArmaNaMao>(true)).Single();
            var weapon=follow.GetComponent<Arma>();
            Transform old=follow.arma;
            EmissorLaser existing=follow.GetComponentInChildren<EmissorLaser>(true);
            GameObject laser=existing!=null?existing.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            if(old!=null && old!=laser.transform)old.gameObject.SetActive(false);
            laser.transform.SetParent(follow.transform,false);
            Vector3 parentScale=follow.transform.lossyScale;
            laser.transform.localScale=new Vector3(.85f/parentScale.x,.85f/parentScale.y,.85f/parentScale.z);
            Quaternion aim=follow.cameraJogador!=null?follow.cameraJogador.rotation:follow.transform.rotation;
            laser.transform.SetPositionAndRotation(follow.mao.position+aim*follow.deslocamento,aim*Quaternion.Euler(0,-90,0)*Quaternion.Euler(follow.rotacaoExtra));
            follow.arma=laser.transform;
            weapon.emissorLaser=laser.GetComponent<EmissorLaser>();
            weapon.miraCanvas=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<RectTransform>(true))
                .FirstOrDefault(x=>x.name=="Mira" && x.GetComponentInParent<Canvas>()!=null);
            weapon.somDoTiro=LaserSound();
            EditorUtility.SetDirty(follow);EditorUtility.SetDirty(weapon);
            PrefabUtility.RecordPrefabInstancePropertyModifications(laser.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(ReportPath+"/status.txt","Laser prefab installed in TPS backup; original gun hidden; muzzle, beam and shot sound assigned.\n");
        }
        finally { if(opened)EditorSceneManager.CloseScene(scene,true); }
    }
}

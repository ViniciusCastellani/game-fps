using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Renders the backup in a separate preview scene without saving the user's open scene.
public static class PreviaCenarioTPS
{
    const string ScenePath = "Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity";
    static string OutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/CenarioTPSPreview"));

    [InitializeOnLoadMethod]
    static void CheckRequestedPreview()
    {
        if (File.Exists(OutputPath + ".request")) EditorApplication.delayCall += RunRequestedPreview;
    }

    static void RunRequestedPreview()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequestedPreview;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(OutputPath + ".request");
        try { Capture(); }
        catch (Exception error)
        {
            Directory.CreateDirectory(OutputPath);
            File.WriteAllText(OutputPath + "/error.txt", error.ToString());
            Debug.LogException(error);
        }
    }

    [MenuItem("Derby/Cenario TPS/Gerar previas da cena de backup %#F9")]
    public static void Capture()
    {
        Directory.CreateDirectory(OutputPath);
        Scene preview = EditorSceneManager.OpenPreviewScene(ScenePath);
        RenderTexture target = null;
        var report = new StringBuilder();
        try
        {
            foreach (GameObject root in preview.GetRootGameObjects())
            {
                foreach (Camera existing in root.GetComponentsInChildren<Camera>(true)) existing.enabled = false;
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
                foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
            GameObject group = preview.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .First(x => x.name == "Melhorias_Visuais_TPS").gameObject;
            report.AppendLine("Scene: " + ScenePath);
            report.AppendLine("Decorative instances: " + group.transform.childCount);
            foreach (string materialName in new[] { "AssentosClaros", "AssentosEscuros", "PublicidadeArena" })
            {
                var paletteMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/DerbySegundoTempo/Cenarios/Estadio/Materiais/" + materialName + ".mat");
                if (paletteMaterial == null || paletteMaterial.shader == null || !paletteMaterial.shader.isSupported)
                    throw new InvalidOperationException("Invalid stadium palette: " + materialName);
            }
            foreach (Renderer renderer in group.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || !material.shader.isSupported)
                        throw new InvalidOperationException("Invalid material: " + renderer.name);
                }
            }
            Physics.SyncTransforms();
            var barriers = group.GetComponentsInChildren<NavMeshObstacle>();
            if (barriers.Length != 10) throw new InvalidOperationException("Expected 10 new barriers.");
            foreach (var barrier in barriers)
            {
                var collider = barrier.GetComponent<BoxCollider>();
                if (collider == null || !collider.enabled || collider.isTrigger || !barrier.carving)
                    throw new InvalidOperationException("Barrier must block physics and carve navigation: " + barrier.name);
                var ray = new Ray(collider.bounds.center + barrier.transform.right * 4, -barrier.transform.right);
                if (!collider.Raycast(ray, out _, 8))
                    throw new InvalidOperationException("Barrier failed collision ray: " + barrier.name);
            }
            report.AppendLine("10 barrier colliders passed ray checks; navigation carving enabled.");
            if (group.transform.Cast<Transform>().Count(x => x.name == "BandeirinhaEscanteio") != 4)
                throw new InvalidOperationException("Expected four corner flags.");
            if (group.GetComponentsInChildren<Transform>().Any(x => x.name.StartsWith("Posto")))
                throw new InvalidOperationException("Extra station signage must be removed.");
            var cameraObject = new GameObject("CameraPreviaTPS");
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = preview;
            camera.fieldOfView = 62;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 350;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(.22f, .29f, .36f);
            camera.cullingMask = ~0;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            target = new RenderTexture(1440, 900, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            Shot(camera, target, "01_sul", new Vector3(10, -19.8f, 3896), new Vector3(8, -21.3f, 3874));
            Shot(camera, target, "02_norte", new Vector3(8, -20.7f, 3912), new Vector3(8, -21.5f, 3944));
            Shot(camera, target, "03_estacoes", new Vector3(5.8f, -20.6f, 3882), new Vector3(6.1f, -21.4f, 3874.5f));
            Shot(camera, target, "04_placa", new Vector3(8, -21.1f, 3936), new Vector3(8, -22.15f, 3943.5f));
            camera.orthographic = true;
            camera.orthographicSize = 43;
            Shot(camera, target, "05_campo", new Vector3(11, 25, 3909), new Vector3(11, -22.862f, 3909));
            camera.orthographic = false;
            Shot(camera, target, "06_bandeirinha", new Vector3(-44, -20.8f, 3936), new Vector3(-49.03f, -21.9f, 3940.56f));
            Shot(camera, target, "07_obstaculos", new Vector3(11, -18.5f, 3888), new Vector3(7, -22.2f, 3910));
            var ball = group.GetComponentInChildren<BolaFutebol>();
            if (ball == null || ball.GetComponent<Rigidbody>().isKinematic || ball.GetComponent<SphereCollider>().isTrigger)
                throw new InvalidOperationException("The scene needs a dynamic football with a solid sphere collider.");
            if (ball.GetComponentInChildren<MeshRenderer>().sharedMaterials.Length != 2)
                throw new InvalidOperationException("Football panels need their black and white materials.");
            report.AppendLine("Football collider and black/white panels validated.");
            Shot(camera, target, "09_bola", new Vector3(8.8f, -21.85f, 3911.7f), ball.transform.position);
            var holder=preview.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<ArmaNaMao>(true)).Single();
            var laser=holder.arma.GetComponent<EmissorLaser>();
            if(laser==null || holder.GetComponent<Arma>().emissorLaser!=laser)
                throw new InvalidOperationException("The backup scene must use the laser weapon and its muzzle.");
            holder.arma.SetPositionAndRotation(holder.mao.position+holder.deslocamento,Quaternion.Euler(0,-90,0));
            Vector3 laserEnd=laser.Origem+Vector3.forward*100;
            float nearest=100;
            Physics.SyncTransforms();
            foreach(var hit in Physics.RaycastAll(laser.Origem,Vector3.forward,100,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(hit.collider.GetComponentInParent<JogadorMovimento>()==null && hit.distance<nearest)
                { nearest=hit.distance;laserEnd=hit.point; }
            laser.Emitir(laserEnd);
            Vector3 gunPosition=holder.arma.position;
            Shot(camera,target,"10_arma_laser",gunPosition+new Vector3(.8f,.35f,-.55f),gunPosition+new Vector3(0,.06f,.17f));
            Shot(camera,target,"11_laser_estadio",holder.transform.position+new Vector3(1.5f,1.45f,-3),holder.transform.position+new Vector3(0,.9f,4));
            report.AppendLine("Laser model, muzzle and visible shot captured.");
            foreach (var root in preview.GetRootGameObjects())
            {
                foreach (var station in root.GetComponentsInChildren<EstacaoDeCura>(true))
                {
                    station.painelDeTexto.SetActive(true);
                    foreach (var canvas in station.painelDeTexto.GetComponentsInChildren<Canvas>(true)) canvas.enabled = true;
                    station.textoFlutuante.text = "Cura de " + station.quantidadeDeCura + " de vida por " + station.custoEmPontos + " pontos\n[E] para usar";
                    station.textoFlutuante.ForceMeshUpdate();
                }
                foreach (var station in root.GetComponentsInChildren<EstacaoDeMunicao>(true))
                {
                    station.painelDeTexto.SetActive(true);
                    foreach (var canvas in station.painelDeTexto.GetComponentsInChildren<Canvas>(true)) canvas.enabled = true;
                    station.textoFlutuante.text = station.quantidadeDeMunicao + " de munição por " + station.custoEmPontos + " pontos\n[E] para usar";
                    station.textoFlutuante.ForceMeshUpdate();
                }
            }
            Shot(camera, target, "08_interacao", new Vector3(6.1f, -20.8f, 3879), new Vector3(6.1f, -21.8f, 3874.5f));
            File.WriteAllText(OutputPath + "/report.txt", report + "Dynamic football valid; materials valid; 11 Unity renders completed.\n");
        }
        finally
        {
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(preview);
        }
        Debug.Log("TPS preview completed: " + OutputPath);
    }

    static void Shot(Camera camera, RenderTexture target, string name, Vector3 position, Vector3 lookAt)
    {
        Vector3 forward = (lookAt - position).normalized;
        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > .99f ? Vector3.forward : Vector3.up;
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, up));
        foreach (var root in camera.scene.GetRootGameObjects())
            foreach (var billboard in root.GetComponentsInChildren<OlharParaCamera>())
                billboard.transform.rotation = Quaternion.LookRotation(billboard.transform.position - position);
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture previous = RenderTexture.active;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(OutputPath + "/" + name + ".png", image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
    }
}

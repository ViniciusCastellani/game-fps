#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ArmaLaserTests
{
    Scene scene;
    GameObject player;
    Component weapon;
    Component emitter;
    Camera camera;
    Transform muzzle;
    Type weaponType;
    Type emitterType;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        scene=SceneManager.CreateScene("TesteIsoladoLaser");
        player=new GameObject("JogadorTeste");SceneManager.MoveGameObjectToScene(player,scene);player.tag="Jogador";
        var cameraObject=new GameObject("CameraTeste");cameraObject.transform.SetParent(player.transform,false);
        cameraObject.transform.position=new Vector3(0,1,0);camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
        weaponType=Type.GetType("Arma, Assembly-CSharp",true);
        emitterType=Type.GetType("EmissorLaser, Assembly-CSharp",true);
        weapon=player.AddComponent(weaponType);((Behaviour)weapon).enabled=false;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DerbySegundoTempo/Armas/Laser/Prefabs/ArmaLaser.prefab");
        Assert.NotNull(prefab);
        var model=UnityEngine.Object.Instantiate(prefab);model.transform.SetParent(player.transform,false);
        emitter=model.GetComponent(emitterType);muzzle=(Transform)emitterType.GetField("pontaDoCano").GetValue(emitter);
        muzzle.position=new Vector3(0,1,.5f);
        Set("cameraJogador",camera);Set("emissorLaser",emitter);Set("alcance",20f);
        Time.timeScale=1;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown() { yield return SceneManager.UnloadSceneAsync(scene); }

    void Set(string name,object value) { weaponType.GetField(name).SetValue(weapon,value); }
    int Ammo => (int)weaponType.GetField("municaoAtual").GetValue(weapon);
    bool Visible => (bool)emitterType.GetProperty("FeixeVisivel").GetValue(emitter);
    Vector3 Endpoint => (Vector3)emitterType.GetProperty("Destino").GetValue(emitter);
    void Shoot() { Physics.SyncTransforms();weaponType.GetMethod("Atirar",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(weapon,null); }
    Component Enemy()
    {
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(target,scene);
        target.name="AlvoTeste";target.tag="Inimigo";target.transform.position=new Vector3(0,1,8);
        return target.AddComponent(Type.GetType("InimigoVida, Assembly-CSharp",true));
    }
    int Life(Component enemy) => (int)enemy.GetType().GetField("vida").GetValue(enemy);

    RectTransform Crosshair(RenderMode mode=RenderMode.ScreenSpaceOverlay,Camera uiCamera=null)
    {
        var canvasObject=new GameObject("CanvasTeste",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject,scene);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=mode;canvas.worldCamera=uiCamera;canvas.planeDistance=2;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1980,1080);
        var image=new GameObject("Mira",typeof(RectTransform),typeof(Image));image.transform.SetParent(canvasObject.transform,false);
        image.GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/DerbySegundoTempo/Arte/Imagens/Interface/crosshair 1.png");
        var rect=image.GetComponent<RectTransform>();
        // Mesmos offsets, escala e pivo da mira da cena TPS.
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=Vector2.zero;
        rect.sizeDelta=new Vector2(50,50);rect.localScale=Vector3.one*1.69f;rect.anchoredPosition=new Vector2(-62,-70);
        Canvas.ForceUpdateCanvases();
        return rect;
    }

    Vector2 CrosshairCenter(RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
        return RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,(corners[0]+corners[2])*.5f);
    }

    void AssertBeamAtCrosshair(RectTransform rect)
    {
        Vector2 actual=camera.WorldToScreenPoint(Endpoint);
        Assert.That(Vector2.Distance(actual,CrosshairCenter(rect)),Is.LessThan(.5f),"O impacto deve coincidir com o centro visual da mira em pixels.");
    }

    [UnityTest]
    public IEnumerator MiraDoCanvasComPivoNoCantoAlinhaFeixeEDano()
    {
        var crosshair=Crosshair();
        yield return null;
        Vector2 center=CrosshairCenter(crosshair);
        var enemy=Enemy();enemy.transform.position=camera.ScreenToWorldPoint(new Vector3(center.x,center.y,8));
        enemy.transform.localScale=Vector3.one*.05f;enemy.GetComponent<Collider>().isTrigger=true;
        Shoot();
        Assert.AreEqual(90,Life(enemy));Assert.AreEqual(29,Ammo);Assert.IsTrue(Visible);
        AssertBeamAtCrosshair(crosshair);
    }

    [UnityTest]
    public IEnumerator MiraAcompanhaEscalaDoCanvasViewportECamera()
    {
        var crosshair=Crosshair();
        yield return null;
        Shoot();AssertBeamAtCrosshair(crosshair);
        crosshair.anchoredPosition=new Vector2(140,-100);
        var scaler=crosshair.GetComponentInParent<CanvasScaler>();
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        camera.rect=new Rect(.1f,.15f,.8f,.7f);camera.fieldOfView=42;
        camera.transform.SetPositionAndRotation(new Vector3(3,2,-4),Quaternion.Euler(12,35,0));
        yield return null;
        Canvas.ForceUpdateCanvases();Shoot();AssertBeamAtCrosshair(crosshair);
        Assert.AreEqual(28,Ammo);
    }

    [UnityTest]
    public IEnumerator MiraScreenSpaceCameraUsaCameraDoCanvas()
    {
        var uiObject=new GameObject("CameraUI");SceneManager.MoveGameObjectToScene(uiObject,scene);
        var uiCamera=uiObject.AddComponent<Camera>();uiCamera.enabled=false;
        uiCamera.transform.SetPositionAndRotation(new Vector3(100,50,-20),Quaternion.Euler(5,30,0));
        var crosshair=Crosshair(RenderMode.ScreenSpaceCamera,uiCamera);
        yield return null;
        Canvas.ForceUpdateCanvases();Shoot();AssertBeamAtCrosshair(crosshair);
        Assert.AreEqual(29,Ammo);
    }

    [UnityTest]
    public IEnumerator TiroCausaDanoEIgnoraJogadorEGatilhos()
    {
        var enemy=Enemy();
        var own=player.AddComponent<CapsuleCollider>();own.center=new Vector3(0,1,2);own.height=1.8f;own.radius=.4f;
        var trigger=new GameObject("GatilhoEstacao");SceneManager.MoveGameObjectToScene(trigger,scene);
        trigger.transform.position=new Vector3(0,1,4);trigger.AddComponent<BoxCollider>().isTrigger=true;
        Shoot();
        Assert.AreEqual(29,Ammo);Assert.AreEqual(90,Life(enemy));Assert.IsTrue(Visible);
        Assert.That(Endpoint.z,Is.EqualTo(7.5f).Within(.01f));
        var lines=emitter.GetComponentsInChildren<LineRenderer>();Assert.AreEqual(2,lines.Length);
        Assert.That(Vector3.Distance(lines[0].GetPosition(0),muzzle.position),Is.LessThan(.001f));
        Assert.That(Vector3.Distance(lines[0].GetPosition(1),Endpoint),Is.LessThan(.001f));
        yield return null;
    }

    [UnityTest]
    public IEnumerator TiroAcertaHitboxDoPrefabRealMesmoComGatilhosGlobaisDesativados()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DerbySegundoTempo/Prefabs/Inimigos/InimigoPerseguidor.prefab");
        Assert.NotNull(prefab);
        var container=new GameObject("InimigoSemIA");SceneManager.MoveGameObjectToScene(container,scene);
        container.SetActive(false);
        var target=UnityEngine.Object.Instantiate(prefab,container.transform);
        target.transform.localPosition=new Vector3(0,0,8);
        // Isola o disparo sem executar navegacao, ataque ou UI do inimigo.
        foreach(var behaviour in target.GetComponentsInChildren<Behaviour>(true))behaviour.enabled=false;
        var enemy=target.GetComponent(Type.GetType("InimigoVida, Assembly-CSharp",true));
        enemy.GetType().GetField("barraVida").SetValue(enemy,null);
        container.SetActive(true);
        var hitbox=target.GetComponent<CapsuleCollider>();Assert.IsTrue(hitbox.isTrigger);
        Physics.SyncTransforms();
        camera.transform.position=new Vector3(0,hitbox.bounds.center.y,0);
        muzzle.position=new Vector3(0,hitbox.bounds.center.y,.5f);
        bool previousQueriesHitTriggers=Physics.queriesHitTriggers;
        try
        {
            Physics.queriesHitTriggers=false;
            Shoot();
            Assert.AreEqual(90,Life(enemy));Assert.AreEqual(29,Ammo);Assert.IsTrue(Visible);
            Assert.That(Endpoint.z,Is.EqualTo(hitbox.bounds.min.z).Within(.01f));
        }
        finally { Physics.queriesHitTriggers=previousQueriesHitTriggers; }
        yield return null;
    }

    [UnityTest]
    public IEnumerator HitboxFilhaSemTagCausaDanoNoInimigoPai()
    {
        var enemy=Enemy();enemy.GetComponent<Collider>().enabled=false;
        var hitbox=new GameObject("HitboxFilhaSemTag");hitbox.transform.SetParent(enemy.transform,false);
        hitbox.AddComponent<BoxCollider>().isTrigger=true;
        Assert.IsTrue(hitbox.CompareTag("Untagged"));
        Shoot();
        Assert.AreEqual(90,Life(enemy));Assert.AreEqual(29,Ammo);Assert.IsTrue(Visible);
        Assert.That(Endpoint.z,Is.EqualTo(7.5f).Within(.01f));
        yield return null;
    }

    [UnityTest]
    public IEnumerator TiroParaNoPrimeiroInimigoComHitboxGatilho()
    {
        var front=Enemy();front.GetComponent<Collider>().isTrigger=true;
        var behind=Enemy();behind.transform.position=new Vector3(0,1,12);
        behind.GetComponent<Collider>().isTrigger=true;
        Shoot();
        Assert.AreEqual(90,Life(front));Assert.AreEqual(100,Life(behind));
        Assert.That(Endpoint.z,Is.EqualTo(7.5f).Within(.01f));
        yield return null;
    }

    [UnityTest]
    public IEnumerator TiroSemAlvoMostraFeixeAteOAlcance()
    {
        Shoot();Assert.IsTrue(Visible);Assert.AreEqual(29,Ammo);
        Assert.That(Vector3.Distance(Endpoint,camera.transform.position+camera.transform.forward*20),Is.LessThan(.001f));
        yield return null;
    }

    [UnityTest]
    public IEnumerator SemMunicaoOuRecarregandoNaoEmiteLaser()
    {
        var enemy=Enemy();Set("municaoAtual",0);Shoot();
        Assert.AreEqual(0,Ammo);Assert.AreEqual(100,Life(enemy));Assert.IsFalse(Visible);
        Set("municaoAtual",30);weaponType.GetField("recarregando",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(weapon,true);
        Shoot();Assert.AreEqual(30,Ammo);Assert.AreEqual(100,Life(enemy));Assert.IsFalse(Visible);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FeixeApagaEReutilizaOsRenderizadores()
    {
        Shoot();Assert.IsTrue(Visible);
        yield return new WaitForSeconds(.16f);
        Assert.IsFalse(Visible);
        foreach(var line in emitter.GetComponentsInChildren<LineRenderer>())Assert.IsFalse(line.enabled);
        Shoot();Assert.IsTrue(Visible);Assert.AreEqual(28,Ammo);Assert.AreEqual(2,emitter.GetComponentsInChildren<LineRenderer>().Length);
        yield return null;
    }

    [UnityTest]
    public IEnumerator CoberturaNaFrenteDoCanoBloqueiaODano()
    {
        var enemy=Enemy();enemy.GetComponent<Collider>().isTrigger=true;muzzle.position=new Vector3(.75f,1,.5f);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(wall,scene);
        wall.name="CoberturaTeste";wall.transform.position=new Vector3(.7f,1,1.5f);wall.transform.localScale=new Vector3(.4f,1,.25f);
        Shoot();Assert.AreEqual(29,Ammo);Assert.AreEqual(100,Life(enemy));Assert.IsTrue(Visible);
        Assert.That(Endpoint.z,Is.InRange(1.3f,1.7f));
        yield return null;
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BolaFutebolTests
{
    Scene scene;
    PhysicsScene physics;
    GameObject prefab;
    const float Step = .02f;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        scene = SceneManager.CreateScene("TesteIsoladoBola", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        physics = scene.GetPhysicsScene();
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DerbySegundoTempo/Cenarios/Estadio/Prefabs/BolaFutebol.prefab");
        Assert.NotNull(prefab);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(floor, scene);
        floor.transform.SetPositionAndRotation(new Vector3(0, -.5f, 0), Quaternion.identity);
        floor.transform.localScale = new Vector3(100, 1, 100);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown() { yield return SceneManager.UnloadSceneAsync(scene); }

    Rigidbody Ball()
    {
        var ball = UnityEngine.Object.Instantiate(prefab, new Vector3(0, .285f, 0), Quaternion.identity);
        SceneManager.MoveGameObjectToScene(ball, scene);
        return ball.GetComponent<Rigidbody>();
    }

    void Simulate(Rigidbody ball, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            ball.SendMessage("FixedUpdate");
            physics.Simulate(Step);
        }
    }

    float Touch(float speed, bool sprint)
    {
        var ball = Ball();
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        SceneManager.MoveGameObjectToScene(player, scene);
        player.tag = "Jogador";
        player.transform.position = new Vector3(0, 1, -1.5f);
        var body = player.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        var type = Type.GetType("JogadorMovimento, Assembly-CSharp", true);
        var movement = (Behaviour)player.AddComponent(type);
        movement.enabled = false;
        type.GetField("velocidadeAndar").SetValue(movement, 5f);
        type.GetField("velocidadeCorrer").SetValue(movement, 6.5f);
        type.GetField("velocidade").SetValue(movement, sprint ? 6.5f : 5f);
        // Alimenta o comando que o controle normal calcula a partir do teclado.
        type.GetField("velocidadeFinal", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, Vector3.forward * speed);
        Physics.SyncTransforms();
        float peak = 0;
        for (int i = 0; i < 80; i++)
        {
            body.linearVelocity = Vector3.forward * speed;
            Simulate(ball, 1);
            peak = Mathf.Max(peak, Vector3.ProjectOnPlane(ball.linearVelocity, Vector3.up).magnitude);
        }
        UnityEngine.Object.DestroyImmediate(player);
        UnityEngine.Object.DestroyImmediate(ball.gameObject);
        return peak;
    }

    [UnityTest]
    public IEnumerator CorrendoEmpurraMaisForteQueAndando()
    {
        float walking = Touch(5, false);
        float running = Touch(6.5f, true);
        TestContext.WriteLine($"Walking peak: {walking:F3} m/s; running peak: {running:F3} m/s");
        Assert.Greater(walking, 1f, "O contato precisa mover a bola.");
        Assert.Greater(running, walking * 1.4f, "Correr precisa dar um toque perceptivelmente mais forte.");
        Assert.Greater(running, 7.5f, "O toque da corrida deve adiantar a bola em relacao ao jogador.");
        Assert.LessOrEqual(running, 12.2f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator JogadorParadoNaoDaImpulso()
    {
        var ball = Ball();
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        SceneManager.MoveGameObjectToScene(player, scene);
        player.tag = "Jogador";
        player.transform.position = new Vector3(0, 1, -.76f);
        var body = player.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeAll;
        var movement = (Behaviour)player.AddComponent(Type.GetType("JogadorMovimento, Assembly-CSharp", true));
        movement.enabled = false;
        Physics.SyncTransforms();
        Simulate(ball, 100);
        Assert.Less(Vector3.ProjectOnPlane(ball.linearVelocity, Vector3.up).magnitude, .05f);
        Assert.Less(Vector2.Distance(new Vector2(ball.position.x, ball.position.z), Vector2.zero), .05f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator BolaRolaEDesaceleraSemAtravessarOChao()
    {
        var ball = Ball();
        ball.linearVelocity = Vector3.right * 3;
        ball.angularVelocity = Vector3.back * (3 / .27f);
        Physics.SyncTransforms();
        Simulate(ball, 400);
        Assert.Greater(ball.position.x, 1);
        Assert.Less(ball.position.x, 12);
        Assert.Less(Vector3.ProjectOnPlane(ball.linearVelocity, Vector3.up).magnitude, .1f);
        Assert.That(ball.position.y, Is.InRange(.24f, .30f));
        yield return null;
    }

    [UnityTest]
    public IEnumerator BolaColideComBarreiraEmAltaVelocidade()
    {
        var ball = Ball();
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(wall, scene);
        wall.transform.position = new Vector3(0, .6f, 2);
        wall.transform.localScale = new Vector3(4, 1.2f, .4f);
        ball.linearVelocity = Vector3.forward * 9;
        Physics.SyncTransforms();
        float maxZ = 0;
        bool bounced = false;
        for (int i = 0; i < 60; i++)
        {
            Simulate(ball, 1);
            maxZ = Mathf.Max(maxZ, ball.position.z);
            bounced |= ball.linearVelocity.z < -.1f;
        }
        TestContext.WriteLine($"Maximum ball center Z: {maxZ:F3}; wall front Z: {wall.GetComponent<Collider>().bounds.min.z:F3}");
        Assert.Less(maxZ, wall.GetComponent<Collider>().bounds.min.z, "O centro da bola nao pode cruzar a face frontal da barreira.");
        Assert.IsTrue(bounced, "A bola deve reagir a colisao.");
        yield return null;
    }
}
#endif

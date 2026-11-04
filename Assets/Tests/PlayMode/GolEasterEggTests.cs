#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GolEasterEggTests
{
    [UnityTest]
    public IEnumerator TriggersPremiamUmaVezReiniciamBolaEPermitemNovoGol()
    {
        var scene = SceneManager.CreateScene("TesteIsoladoGol", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var physics = scene.GetPhysicsScene();
        var scoreType = Type.GetType("SistemaDePontos, Assembly-CSharp", true);
        var goalType = Type.GetType("GolEasterEgg, Assembly-CSharp", true);
        var triggerType = Type.GetType("TriggerGol, Assembly-CSharp", true);
        var ballType = Type.GetType("BolaFutebol, Assembly-CSharp", true);
        Component score = (Component)scoreType.GetField("instancia").GetValue(null);
        int initial = 0;
        try
        {
            GameObject Create(string name)
            {
                var obj = new GameObject(name);
                SceneManager.MoveGameObjectToScene(obj, scene);
                return obj;
            }
            if (score == null) score = Create("PontosTeste").AddComponent(scoreType);
            initial = (int)scoreType.GetField("pontosAtuais").GetValue(score);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DerbySegundoTempo/Cenarios/Estadio/Prefabs/BolaFutebol.prefab");
            var ball = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 1, 0), Quaternion.identity);
            SceneManager.MoveGameObjectToScene(ball, scene);
            var body = ball.GetComponent<Rigidbody>();
            body.useGravity = false;
            var ballScript = ball.GetComponent(ballType);
            var center = Create("Centro").transform;
            center.position = new Vector3(10, 1, 10);
            var goal = Create("GolTeste").AddComponent(goalType);
            goalType.GetField("bola").SetValue(goal, ballScript);
            goalType.GetField("centroDoCampo").SetValue(goal, center);
            goalType.GetField("sistemaDePontos").SetValue(goal, score);
            goalType.GetField("pontosPorGol").SetValue(goal, 137);
            goalType.GetField("tempoParaReiniciar").SetValue(goal, .1f);
            goalType.GetField("materialConfete").SetValue(goal,
                AssetDatabase.LoadAssetAtPath<Material>("Assets/DerbySegundoTempo/Cenarios/Estadio/Materiais/ConfetesGol.mat"));
            var vuvuzela = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/DerbySegundoTempo/Cenarios/Estadio/Audio/VuvuzelaGol.wav");
            Assert.IsNotNull(vuvuzela);
            Assert.Greater(vuvuzela.length, 2f);
            goalType.GetField("somVuvuzela").SetValue(goal, vuvuzela);
            Assert.IsNotNull(goal.GetComponent<AudioSource>());
            Assert.IsFalse(goal.GetComponent<AudioSource>().playOnAwake);
            Assert.IsFalse(goal.GetComponent<AudioSource>().loop);
            Assert.AreEqual(0f, goal.GetComponent<AudioSource>().spatialBlend);
            var register = goalType.GetMethod("RegistrarGol");
            Assert.IsFalse((bool)register.Invoke(goal, new object[] { null, Vector3.zero }));
            foreach (float x in new[] { -2f, 2f })
            {
                var trigger = Create("Trigger");
                trigger.transform.position = new Vector3(x, 1, 0);
                var box = trigger.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(1, 3, 3);
                var script = trigger.AddComponent(triggerType);
                triggerType.GetField("easterEgg").SetValue(script, goal);
                // Um corpo que nao seja a bola nao pode pontuar.
                var other = Create("OutroCorpo");
                other.transform.position = trigger.transform.position;
                other.AddComponent<BoxCollider>();
                other.AddComponent<Rigidbody>().useGravity = false;
                Physics.SyncTransforms();
                physics.Simulate(.02f);
                int before = (int)scoreType.GetField("pontosAtuais").GetValue(score);
                Assert.AreEqual(initial + (x < 0 ? 0 : 137), before);
                UnityEngine.Object.DestroyImmediate(other);
                body.position = new Vector3(0, 1, 0);
                body.linearVelocity = new Vector3(Mathf.Sign(x) * 10, 0, 0);
                body.angularVelocity = Vector3.one;
                Physics.SyncTransforms();
                for (int i = 0; i < 20 && !body.isKinematic; i++) physics.Simulate(.02f);
                Assert.AreEqual(before + 137, scoreType.GetField("pontosAtuais").GetValue(score));
                Assert.IsTrue(body.isKinematic);
                Assert.IsFalse((bool)register.Invoke(goal, new object[] { ballScript, trigger.transform.position }));
                Assert.AreEqual(before + 137, scoreType.GetField("pontosAtuais").GetValue(score));
                Assert.IsNotNull(goal.GetComponentInChildren<ParticleSystem>());
                yield return new WaitForSeconds(.15f);
                Assert.IsFalse(body.isKinematic);
                Assert.IsTrue(body.detectCollisions);
                Assert.Less(Vector3.Distance(body.position, center.position), .001f);
                Assert.Less(body.linearVelocity.magnitude, .001f);
                Assert.Less(body.angularVelocity.magnitude, .001f);
                UnityEngine.Object.DestroyImmediate(trigger);
            }
            // Desativar o sistema durante a comemoracao tambem deve liberar a bola.
            Assert.IsTrue((bool)register.Invoke(goal, new object[] { ballScript, Vector3.zero }));
            ((Behaviour)goal).enabled = false;
            Assert.IsFalse(body.isKinematic);
            Assert.IsTrue(body.detectCollisions);
            Assert.Less(Vector3.Distance(body.position, center.position), .001f);
        }
        finally
        {
            if (score != null)
            {
                scoreType.GetField("pontosAtuais").SetValue(score, initial);
                scoreType.GetMethod("AdicionarPontos").Invoke(score, new object[] { 0 });
            }
        }
        yield return SceneManager.UnloadSceneAsync(scene);
    }
}
#endif

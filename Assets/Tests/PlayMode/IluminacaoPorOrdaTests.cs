#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class IluminacaoPorOrdaTests
{
    Scene cena;
    Scene cenaAnterior;
    float escalaAnterior;

    [UnityTest]
    public IEnumerator HordasMudamPeriodoEReinicioNaSegundaComecaDeTarde()
    {
        cenaAnterior = SceneManager.GetActiveScene();
        escalaAnterior = Time.timeScale;
        Time.timeScale = 1;
        cena = SceneManager.CreateScene("TesteIluminacaoPorOrda");
        SceneManager.SetActiveScene(cena);
        GameObject Criar(string nome)
        {
            var objeto = new GameObject(nome);
            SceneManager.MoveGameObjectToScene(objeto, cena);
            return objeto;
        }
        var tipoLuz = Type.GetType("IluminacaoPorOrda, Assembly-CSharp", true);
        var tipoHordas = Type.GetType("GerenciadorDeOrdas, Assembly-CSharp", true);
        var tipoCronometro = Type.GetType("Cronometro, Assembly-CSharp", true);
        var sol = Criar("SolTeste").AddComponent<Light>();
        sol.type = LightType.Directional;
        var luz = Criar("ControleLuz").AddComponent(tipoLuz);
        tipoLuz.GetField("sol").SetValue(luz, sol);
        tipoLuz.GetField("duracaoTransicao").SetValue(luz, .08f);
        var ceuOriginal = RenderSettings.skybox;
        var shader = Shader.Find("Skybox/Procedural");
        Assert.IsNotNull(shader);
        var ceu = new Material(shader);
        ceu.SetColor("_SkyTint", Color.blue);
        RenderSettings.skybox = ceu;
        var inicio = tipoHordas.GetMethod("IniciarOrda", BindingFlags.NonPublic | BindingFlags.Instance);
        Component Gerenciador()
        {
            var manager = Criar("HordasTeste").AddComponent(tipoHordas);
            ((Behaviour)manager).enabled = false;
            tipoHordas.GetField("iluminacao").SetValue(manager, luz);
            var timer = Criar("CronometroTeste").AddComponent(tipoCronometro);
            ((Behaviour)timer).enabled = false;
            tipoHordas.GetField("cronometro", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, timer);
            var tipoOrda = tipoHordas.GetNestedType("Orda");
            var ordas = Array.CreateInstance(tipoOrda, 2);
            for (int i = 0; i < 2; i++)
            {
                var orda = Activator.CreateInstance(tipoOrda);
                tipoOrda.GetField("prefabsInimigos").SetValue(orda, new GameObject[0]);
                ordas.SetValue(orda, i);
            }
            tipoHordas.GetField("ordas").SetValue(manager, ordas);
            return manager;
        }
        var hordas = Gerenciador();
        inicio.Invoke(hordas, new object[] { 0 });
        Assert.That(sol.intensity, Is.EqualTo(2f).Within(.01f));
        Assert.That(Quaternion.Angle(sol.transform.rotation, Quaternion.Euler(50, -30, 0)), Is.LessThan(.1f));
        Assert.AreEqual(UnityEngine.Rendering.AmbientMode.Trilight, RenderSettings.ambientMode);
        Assert.AreNotSame(ceu, RenderSettings.skybox, "O asset original do ceu deve ser preservado.");
        inicio.Invoke(hordas, new object[] { 1 });
        Assert.Greater(sol.intensity, 1.5f, "A mudanca entre hordas deve ser gradual.");
        yield return new WaitForSeconds(.15f);
        Assert.That(sol.intensity, Is.EqualTo(1.5f).Within(.01f));
        Assert.That(sol.color.g, Is.EqualTo(.66f).Within(.01f));
        Assert.That(Quaternion.Angle(sol.transform.rotation, Quaternion.Euler(22, -65, 0)), Is.LessThan(.1f));
        Assert.That(ceu.GetColor("_SkyTint"), Is.EqualTo(Color.blue));
        // Simula um novo gerenciador reiniciando diretamente na horda 2.
        tipoLuz.GetMethod("AplicarOrda").Invoke(luz, new object[] { 1, true });
        inicio.Invoke(Gerenciador(), new object[] { 1 });
        Assert.That(sol.intensity, Is.EqualTo(1.5f).Within(.01f));
        Assert.That(sol.color.g, Is.EqualTo(.66f).Within(.01f));
        UnityEngine.Object.DestroyImmediate(luz.gameObject);
        Assert.AreSame(ceu, RenderSettings.skybox);
        RenderSettings.skybox = ceuOriginal;
        UnityEngine.Object.DestroyImmediate(ceu);
    }

    [UnityTearDown]
    public IEnumerator Limpar()
    {
        if (cena.IsValid() && cena.isLoaded) yield return SceneManager.UnloadSceneAsync(cena);
        if (cenaAnterior.IsValid() && cenaAnterior.isLoaded) SceneManager.SetActiveScene(cenaAnterior);
        Time.timeScale = escalaAnterior;
    }
}
#endif

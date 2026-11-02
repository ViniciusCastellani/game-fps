using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class IluminacaoPorOrda : MonoBehaviour
{
    [System.Serializable]
    public class Periodo
    {
        public Vector3 rotacaoSol;
        public Color corSol = Color.white;
        [Min(0f)] public float intensidadeSol = 2f;
        public Color ceu;
        public Color horizonte;
        public Color solo;
        public Color tonalidadeSkybox;
        [Range(0f, 5f)] public float exposicaoSkybox = 1.3f;
    }

    public Light sol;
    public GerenciadorDeOrdas gerenciadorDeOrdas;
    [Min(0f)] public float duracaoTransicao = 3f;
    public Periodo dia = new Periodo {
        rotacaoSol = new Vector3(50, -30, 0), corSol = new Color(1f, .97f, .9f), intensidadeSol = 2f,
        ceu = new Color(.42f, .56f, .75f), horizonte = new Color(.44f, .47f, .5f),
        solo = new Color(.22f, .24f, .2f), tonalidadeSkybox = new Color(.5f, .55f, .65f), exposicaoSkybox = 1.3f
    };
    public Periodo tarde = new Periodo {
        rotacaoSol = new Vector3(22, -65, 0), corSol = new Color(1f, .66f, .38f), intensidadeSol = 1.5f,
        ceu = new Color(.4f, .39f, .5f), horizonte = new Color(.63f, .38f, .24f),
        solo = new Color(.24f, .19f, .16f), tonalidadeSkybox = new Color(.7f, .36f, .21f), exposicaoSkybox = 1.05f
    };

    Material skyboxOriginal;
    Material skyboxInstanciado;
    Coroutine transicao;
    int ultimaOrdaAplicada;

    void LateUpdate()
    {
        // Mantem a iluminacao sincronizada inclusive ao reativar o componente.
        if (gerenciadorDeOrdas != null && gerenciadorDeOrdas.NumeroDaOrdaAtual != ultimaOrdaAplicada)
            AplicarOrda(gerenciadorDeOrdas.NumeroDaOrdaAtual, ultimaOrdaAplicada == 0);
    }

    public void AplicarOrda(int numero, bool imediato = false)
    {
        if (sol == null || !isActiveAndEnabled) return;
        if (numero == ultimaOrdaAplicada && !imediato) return;
        ultimaOrdaAplicada = numero;
        PrepararSkybox();
        if (transicao != null) StopCoroutine(transicao);
        transicao = null;
        Periodo alvo = numero <= 1 ? dia : tarde;
        if (imediato || duracaoTransicao <= 0f)
        {
            Aplicar(alvo, alvo, 1f);
            return;
        }
        var origem = new Periodo {
            rotacaoSol = sol.transform.eulerAngles, corSol = sol.color, intensidadeSol = sol.intensity,
            ceu = RenderSettings.ambientSkyColor, horizonte = RenderSettings.ambientEquatorColor,
            solo = RenderSettings.ambientGroundColor,
            tonalidadeSkybox = skyboxInstanciado != null && skyboxInstanciado.HasProperty("_SkyTint")
                ? skyboxInstanciado.GetColor("_SkyTint") : alvo.tonalidadeSkybox,
            exposicaoSkybox = skyboxInstanciado != null && skyboxInstanciado.HasProperty("_Exposure")
                ? skyboxInstanciado.GetFloat("_Exposure") : alvo.exposicaoSkybox
        };
        transicao = StartCoroutine(Transicionar(origem, alvo));
    }

    void PrepararSkybox()
    {
        RenderSettings.sun = sol;
        sol.useColorTemperature = false;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        if (skyboxInstanciado != null || RenderSettings.skybox == null) return;
        skyboxOriginal = RenderSettings.skybox;
        skyboxInstanciado = new Material(skyboxOriginal);
        RenderSettings.skybox = skyboxInstanciado;
    }

    IEnumerator Transicionar(Periodo origem, Periodo alvo)
    {
        float tempo = 0;
        while (tempo < duracaoTransicao)
        {
            tempo += Time.unscaledDeltaTime;
            Aplicar(origem, alvo, Mathf.SmoothStep(0, 1, tempo / duracaoTransicao));
            yield return null;
        }
        Aplicar(alvo, alvo, 1);
        transicao = null;
    }

    void Aplicar(Periodo origem, Periodo alvo, float t)
    {
        sol.transform.rotation = Quaternion.Slerp(Quaternion.Euler(origem.rotacaoSol), Quaternion.Euler(alvo.rotacaoSol), t);
        sol.color = Color.Lerp(origem.corSol, alvo.corSol, t);
        sol.intensity = Mathf.Lerp(origem.intensidadeSol, alvo.intensidadeSol, t);
        RenderSettings.ambientSkyColor = Color.Lerp(origem.ceu, alvo.ceu, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(origem.horizonte, alvo.horizonte, t);
        RenderSettings.ambientGroundColor = Color.Lerp(origem.solo, alvo.solo, t);
        if (skyboxInstanciado == null) return;
        if (skyboxInstanciado.HasProperty("_SkyTint"))
            skyboxInstanciado.SetColor("_SkyTint", Color.Lerp(origem.tonalidadeSkybox, alvo.tonalidadeSkybox, t));
        if (skyboxInstanciado.HasProperty("_GroundColor"))
            skyboxInstanciado.SetColor("_GroundColor", Color.Lerp(origem.solo, alvo.solo, t));
        if (skyboxInstanciado.HasProperty("_Exposure"))
            skyboxInstanciado.SetFloat("_Exposure", Mathf.Lerp(origem.exposicaoSkybox, alvo.exposicaoSkybox, t));
    }

    void OnDisable()
    {
        if (transicao != null) StopCoroutine(transicao);
        transicao = null;
        ultimaOrdaAplicada = 0;
    }

    void OnDestroy()
    {
        if (skyboxInstanciado == null) return;
        if (RenderSettings.skybox == skyboxInstanciado) RenderSettings.skybox = skyboxOriginal;
        if (Application.isPlaying) Destroy(skyboxInstanciado);
        else DestroyImmediate(skyboxInstanciado);
    }
}

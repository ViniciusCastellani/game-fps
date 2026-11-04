using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GolEasterEgg : MonoBehaviour
{
    [Min(1)] public int pontosPorGol = 100;
    [Min(0.1f)] public float tempoParaReiniciar = 1.5f;
    public BolaFutebol bola;
    public Transform centroDoCampo;
    public SistemaDePontos sistemaDePontos;
    public Material materialConfete;

    [Header("Som do gol")]
    public AudioClip somVuvuzela;
    [Range(0f, 1f)] public float volumeVuvuzela = .65f;

    AudioSource audioGol;

    TMP_Text mensagem;
    float esconderMensagemEm;
    bool reinicioPendente;

    void Awake()
    {
        audioGol = GetComponent<AudioSource>();
        audioGol.playOnAwake = false;
        audioGol.loop = false;
        audioGol.spatialBlend = 0f; // Comemoracao audivel em qualquer camera do jogador.
    }

    public bool RegistrarGol(BolaFutebol candidata, Vector3 posicaoDoGol)
    {
        if (!isActiveAndEnabled || candidata == null || candidata != bola ||
            centroDoCampo == null || sistemaDePontos == null || reinicioPendente) return false;
        if (!bola.SuspenderParaGol()) return false;
        reinicioPendente = true;
        sistemaDePontos.AdicionarPontos(pontosPorGol);
        Comemorar(posicaoDoGol);
        StartCoroutine(ReiniciarDepoisDoGol());
        return true;
    }

    IEnumerator ReiniciarDepoisDoGol()
    {
        yield return new WaitForSeconds(tempoParaReiniciar);
        ReiniciarBola();
    }

    void ReiniciarBola()
    {
        if (reinicioPendente && bola != null && centroDoCampo != null)
            bola.Reiniciar(centroDoCampo.position);
        reinicioPendente = false;
    }

    void Update()
    {
        if (mensagem != null && Time.time >= esconderMensagemEm)
            mensagem.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ReiniciarBola();
        if (audioGol != null) audioGol.Stop();
        if (mensagem != null) mensagem.gameObject.SetActive(false);
    }

    void Comemorar(Vector3 posicao)
    {
        if (somVuvuzela != null) audioGol.PlayOneShot(somVuvuzela, volumeVuvuzela);
        if (mensagem == null)
        {
            var tela = new GameObject("ComemoracaoGol", typeof(Canvas), typeof(CanvasScaler));
            tela.transform.SetParent(transform, false);
            var canvas = tela.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var escala = tela.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920, 1080);
            var texto = new GameObject("MensagemGol", typeof(RectTransform), typeof(TextMeshProUGUI));
            texto.transform.SetParent(tela.transform, false);
            mensagem = texto.GetComponent<TMP_Text>();
            mensagem.raycastTarget = false;
            mensagem.alignment = TextAlignmentOptions.Center;
            mensagem.fontSize = 52;
            mensagem.fontStyle = FontStyles.Bold;
            mensagem.color = new Color(1, .85f, .15f);
            var rect = mensagem.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .78f);
            rect.sizeDelta = new Vector2(900, 160);
        }
        mensagem.text = $"GOL!\n<size=32>+{pontosPorGol} PONTOS</size>";
        mensagem.gameObject.SetActive(true);
        esconderMensagemEm = Time.time + 3f;

        var efeito = new GameObject("ConfetesGol");
        efeito.layer = 2;
        efeito.transform.SetParent(transform, false);
        efeito.transform.position = posicao + Vector3.up;
        efeito.transform.rotation = Quaternion.Euler(-90, 0, 0);
        var particulas = efeito.AddComponent<ParticleSystem>();
        particulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var principal = particulas.main;
        principal.playOnAwake = false;
        principal.loop = false;
        principal.duration = .3f;
        principal.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
        principal.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
        principal.startSize = new ParticleSystem.MinMaxCurve(.09f, .2f);
        principal.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        principal.gravityModifier = .65f;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        principal.maxParticles = 180;
        var cores = new Gradient();
        cores.SetKeys(new[] {
            new GradientColorKey(Color.yellow, 0), new GradientColorKey(Color.cyan, .33f),
            new GradientColorKey(Color.magenta, .66f), new GradientColorKey(Color.green, 1)
        }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        principal.startColor = new ParticleSystem.MinMaxGradient(cores) { mode = ParticleSystemGradientMode.RandomColor };
        var emissao = particulas.emission;
        emissao.rateOverTime = 0;
        emissao.SetBursts(new[] { new ParticleSystem.Burst(0, 160) });
        var forma = particulas.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 55;
        forma.radius = 2f;
        var giro = particulas.rotationOverLifetime;
        giro.enabled = true;
        giro.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        var renderer = particulas.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = materialConfete;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particulas.Play();
        Destroy(efeito, 4.5f);
    }
}

using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(250)]
public class Arma : MonoBehaviour
{
    public Camera cameraJogador;
    [Tooltip("Mira do Canvas. Quando vazia, localiza o RectTransform Mira na mesma cena.")]
    public RectTransform miraCanvas;
    public float alcance = 100f;
    public int dano = 10;
    public EmissorLaser emissorLaser;

    public AudioSource audioFonte;
    public AudioClip somDoTiro;
    public AudioClip somDePenteVazio;
    public AudioClip somDeRecarga;

    public int capacidadePente = 30;
    public int municaoAtual = 30;
    public int municaoReserva = 90;

    private bool recarregando = false;
    private bool disparoPendente;

    void Update()
    {
        if (recarregando || Time.timeScale <= 0f)
        {
            return;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            disparoPendente = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Recarregar();
        }
    }

    void LateUpdate()
    {
        if (!disparoPendente) return;
        disparoPendente = false;
        Atirar();
    }

    void OnDisable() { disparoPendente = false; }

    void Atirar()
    {
        if (recarregando || Time.timeScale <= 0f || cameraJogador == null) return;
        if (municaoAtual <= 0)
        {
            TocarSom(somDePenteVazio);
            Debug.Log("Sem munição!");
            return;
        }

        municaoAtual--;

        TocarSom(somDoTiro);
        Ray raio = ObterRaioDaMira();
        RaycastHit hit;
        bool acertou = EncontrarImpacto(raio, alcance, out hit);
        Vector3 destino = acertou ? hit.point : raio.GetPoint(alcance);
        if (emissorLaser != null)
        {
            Vector3 trajeto = destino - emissorLaser.Origem;
            // A mira pode enxergar um alvo enquanto o cano esta atras de uma cobertura.
            if (trajeto.sqrMagnitude > 0.0001f &&
                EncontrarImpacto(new Ray(emissorLaser.Origem, trajeto.normalized), trajeto.magnitude, out var bloqueio))
            {
                hit = bloqueio;
                destino = bloqueio.point;
                acertou = true;
            }
            emissorLaser.Emitir(destino);
        }
        if (acertou)
        {
            Debug.Log("Acertou: " + hit.collider.name);
            CausaDano(hit);
        }
    }

    Ray ObterRaioDaMira()
    {
        if (miraCanvas == null) LocalizarMiraCanvas();

        Vector2 centroTela = cameraJogador.pixelRect.center;
        Canvas canvas = miraCanvas != null ? miraCanvas.GetComponentInParent<Canvas>() : null;
        if (canvas != null)
        {
            Canvas canvasRaiz = canvas.rootCanvas;
            Camera cameraCanvas = canvasRaiz.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : (canvasRaiz.worldCamera != null ? canvasRaiz.worldCamera : cameraJogador);
            // O centro visual pode ser diferente do pivo e do centro da tela.
            Vector3 centroMira = miraCanvas.TransformPoint(miraCanvas.rect.center);
            centroTela = RectTransformUtility.WorldToScreenPoint(cameraCanvas, centroMira);
        }

        Ray raio = cameraJogador.ScreenPointToRay(centroTela);
        // Mantem o alcance medido a partir da camera em perspectiva.
        if (!cameraJogador.orthographic) raio.origin = cameraJogador.transform.position;
        return raio;
    }

    void LocalizarMiraCanvas()
    {
        // Compatibilidade com as cenas existentes sem referencia serializada.
        foreach (var raiz in gameObject.scene.GetRootGameObjects())
        {
            foreach (var rect in raiz.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name != "Mira" || rect.GetComponentInParent<Canvas>() == null) continue;
                miraCanvas = rect;
                return;
            }
        }
    }

    bool EncontrarImpacto(Ray raio, float distancia, out RaycastHit impacto)
    {
        impacto = default;
        float maisPerto = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(raio, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= maisPerto) continue;
            // Os inimigos usam hitboxes gatilho; estacoes e coletaveis nao bloqueiam o tiro.
            if (hit.collider.isTrigger && hit.collider.GetComponentInParent<InimigoVida>() == null) continue;
            maisPerto = hit.distance;
            impacto = hit;
        }
        return maisPerto < float.PositiveInfinity;
    }

    void TocarSom(AudioClip som)
    {
        if (audioFonte != null && som != null) audioFonte.PlayOneShot(som);
    }
    void CausaDano(RaycastHit hit)
    {
        InimigoVida vida = hit.collider.GetComponentInParent<InimigoVida>();
        if (vida != null)
        {
            vida.ReceberDano(dano);
        }
    }

    void Recarregar()
    {
        if (municaoAtual >= capacidadePente)
        {
            return;
        }

        if (municaoReserva <= 0)
        {
            return;
        }

        StartCoroutine(RecarregarComCooldown());
    }

    IEnumerator RecarregarComCooldown()
    {
        recarregando = true;

        TocarSom(somDeRecarga);

        if (somDeRecarga != null)
        {
            yield return new WaitForSeconds(somDeRecarga.length);
        }

        int quantidadeNecessaria = capacidadePente - municaoAtual;

        int quantidadeRecarregada = Mathf.Min(
            quantidadeNecessaria,
            municaoReserva
        );

        municaoAtual += quantidadeRecarregada;
        municaoReserva -= quantidadeRecarregada;

        Debug.Log(
            "Munição: "
            + municaoAtual
            + "/"
            + municaoReserva
        );

        recarregando = false;
    }

    public void AdicionarMunicao(int quantidade)
    {
        municaoReserva += quantidade;

        Debug.Log("Munição reserva: " + municaoReserva);
    }

    public void AumentarCapacidadeDoPente(int quantidade)
    {
        capacidadePente += quantidade;
    }
}

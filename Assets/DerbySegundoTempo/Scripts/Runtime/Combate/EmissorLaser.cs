using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(300)]
[DisallowMultipleComponent]
public class EmissorLaser : MonoBehaviour
{
    public Transform pontaDoCano;
    public Material materialFeixe;
    public Material materialNucleo;
    [Min(0.01f)] public float duracao = 0.09f;
    [Min(0.001f)] public float largura = 0.035f;
    public Light luzDoDisparo;

    private LineRenderer feixe;
    private LineRenderer nucleo;
    private Vector3 destino;
    private float apagarEm;
    private bool visivel;

    public Vector3 Origem => pontaDoCano != null ? pontaDoCano.position : transform.position;
    public bool FeixeVisivel => visivel;
    public Vector3 Destino => destino;

    void Preparar()
    {
        if (feixe != null) return;
        feixe = CriarLinha("FeixeLaser", materialFeixe, largura);
        nucleo = CriarLinha("NucleoLaser", materialNucleo, largura * 0.25f);
    }

    LineRenderer CriarLinha(string nome, Material material, float espessura)
    {
        var objeto = new GameObject(nome);
        objeto.layer = 2; // O efeito nao intercepta os proprios disparos.
        objeto.transform.SetParent(transform, false);
        var linha = objeto.AddComponent<LineRenderer>();
        linha.sharedMaterial = material;
        linha.useWorldSpace = true;
        linha.positionCount = 2;
        linha.startWidth = espessura;
        linha.endWidth = espessura * 0.7f;
        linha.numCapVertices = 3;
        linha.alignment = LineAlignment.View;
        linha.shadowCastingMode = ShadowCastingMode.Off;
        linha.receiveShadows = false;
        linha.enabled = false;
        return linha;
    }

    public void Emitir(Vector3 pontoFinal)
    {
        Preparar();
        destino = pontoFinal;
        apagarEm = Time.time + duracao;
        visivel = true;
        if (luzDoDisparo != null) luzDoDisparo.enabled = true;
        AtualizarLinhas();
    }

    void LateUpdate()
    {
        if (!visivel) return;
        if (Time.time >= apagarEm) { Apagar(); return; }
        // Executa depois da arma seguir a mao e a camera no mesmo frame.
        AtualizarLinhas();
    }

    void AtualizarLinhas()
    {
        feixe.enabled = nucleo.enabled = true;
        feixe.SetPosition(0, Origem);
        feixe.SetPosition(1, destino);
        nucleo.SetPosition(0, Origem);
        nucleo.SetPosition(1, destino);
    }

    void Apagar()
    {
        visivel = false;
        if (feixe != null) feixe.enabled = false;
        if (nucleo != null) nucleo.enabled = false;
        if (luzDoDisparo != null) luzDoDisparo.enabled = false;
    }

    void OnDisable() { Apagar(); }
}

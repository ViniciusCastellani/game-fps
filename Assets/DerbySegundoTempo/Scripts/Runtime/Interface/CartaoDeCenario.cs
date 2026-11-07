using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CartaoDeCenario : MonoBehaviour
{
    public static readonly Color CorEscolhido = new Color(1f, 0.776f, 0.161f);
    public static readonly Color CorNormal = new Color(0.231f, 0.286f, 0.376f);
    public static readonly Color CorImagemApagada = new Color(0.6f, 0.6f, 0.6f, 1f);

    [SerializeField] private string nomeDaCena;
    [SerializeField] private MenuDeModos menu;
    [SerializeField] private Image moldura;
    [SerializeField] private Image imagem;
    [SerializeField] private GameObject seloSelecionado;

    public string NomeDaCena
    {
        get { return nomeDaCena; }
    }

    public bool Escolhido { get; private set; }

    public void Escolher()
    {
        menu.EscolherCartao(this);
    }

    public void DefinirEscolhido(bool escolhido)
    {
        Escolhido = escolhido;
        moldura.color = escolhido ? CorEscolhido : CorNormal;
        imagem.color = escolhido ? Color.white : CorImagemApagada;
        seloSelecionado.SetActive(escolhido);
    }
}

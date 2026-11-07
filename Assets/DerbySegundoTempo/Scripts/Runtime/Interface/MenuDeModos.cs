using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuDeModos : MonoBehaviour
{
    public enum Tela
    {
        Principal,
        Modos,
        Cenarios
    }

    [SerializeField] private GerenciadorDeCenas gerenciadorDeCenas;

    [Header("Principal")]
    [SerializeField] private Button botaoJogar;
    [SerializeField] private Button botaoSair;

    [Header("Modos")]
    [SerializeField] private GameObject painelModos;
    [SerializeField] private Button botaoModoNormal;
    [SerializeField] private Button botaoModoInfinito;
    [SerializeField] private Button botaoVoltarModos;

    [Header("Cenários")]
    [SerializeField] private GameObject painelCenarios;
    [SerializeField] private CartaoDeCenario[] cartoes;
    [SerializeField] private Button botaoIniciarSobrevivencia;
    [SerializeField] private Button botaoVoltarCenarios;

    public Tela TelaAtual { get; private set; }
    public CartaoDeCenario CartaoEscolhido { get; private set; }

    void Start()
    {
        CartaoDeCenario inicial = null;

        foreach (CartaoDeCenario cartao in cartoes)
        {
            if (cartao != null && cartao.NomeDaCena == SessaoDeJogo.UltimoCenarioInfinito)
            {
                inicial = cartao;
                break;
            }
        }

        if (inicial == null && cartoes.Length > 0)
        {
            inicial = cartoes[0];
        }

        EscolherCartao(inicial);

        if (SessaoDeJogo.ConsumirPedidoDeEscolhaDeCenario())
        {
            Mostrar(Tela.Cenarios, CartaoEscolhido != null ? CartaoEscolhido.gameObject : null);
        }
        else
        {
            Mostrar(Tela.Principal, botaoJogar.gameObject);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Voltar();
        }

        EventSystem sistema = EventSystem.current;
        if (sistema == null)
        {
            return;
        }

        GameObject selecionado = sistema.currentSelectedGameObject;
        if (selecionado == null || !selecionado.activeInHierarchy || !PertenceATelaAtual(selecionado))
        {
            sistema.SetSelectedGameObject(FocoPadraoDaTelaAtual());
        }
    }

    public void AbrirEscolhaDeModo()
    {
        Mostrar(Tela.Modos, botaoModoNormal.gameObject);
    }

    public void AbrirEscolhaDeCenario()
    {
        Mostrar(Tela.Cenarios, CartaoEscolhido != null ? CartaoEscolhido.gameObject : null);
    }

    public void Voltar()
    {
        if (TelaAtual == Tela.Cenarios)
        {
            Mostrar(Tela.Modos, botaoModoInfinito.gameObject);
        }
        else if (TelaAtual == Tela.Modos)
        {
            Mostrar(Tela.Principal, botaoJogar.gameObject);
        }
    }

    public void IniciarModoNormal()
    {
        gerenciadorDeCenas.IniciarCampanha();
    }

    public void EscolherCartao(CartaoDeCenario escolhido)
    {
        if (escolhido == null)
        {
            return;
        }

        CartaoEscolhido = escolhido;

        foreach (CartaoDeCenario cartao in cartoes)
        {
            if (cartao != null)
            {
                cartao.DefinirEscolhido(cartao == escolhido);
            }
        }
    }

    public void IniciarSobrevivencia()
    {
        if (CartaoEscolhido == null)
        {
            return;
        }

        gerenciadorDeCenas.IniciarModoInfinito(CartaoEscolhido.NomeDaCena);
    }

    void Mostrar(Tela tela, GameObject foco)
    {
        TelaAtual = tela;
        painelModos.SetActive(tela == Tela.Modos);
        painelCenarios.SetActive(tela == Tela.Cenarios);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(foco);
        }
    }

    GameObject FocoPadraoDaTelaAtual()
    {
        switch (TelaAtual)
        {
            case Tela.Modos:
                return botaoModoNormal.gameObject;
            case Tela.Cenarios:
                return CartaoEscolhido != null ? CartaoEscolhido.gameObject : botaoIniciarSobrevivencia.gameObject;
            default:
                return botaoJogar.gameObject;
        }
    }

    bool PertenceATelaAtual(GameObject selecionado)
    {
        switch (TelaAtual)
        {
            case Tela.Modos:
                return selecionado.transform.IsChildOf(painelModos.transform);
            case Tela.Cenarios:
                return selecionado.transform.IsChildOf(painelCenarios.transform);
            default:
                return selecionado == botaoJogar.gameObject || selecionado == botaoSair.gameObject;
        }
    }
}

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
    [SerializeField] private Tela telaInicial;

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
    [SerializeField] private CartaoDeCenario[] cartoes = new CartaoDeCenario[0];
    [SerializeField] private Button botaoIniciarSobrevivencia;
    [SerializeField] private Button botaoVoltarCenarios;

    public Tela TelaAtual { get; private set; }
    public CartaoDeCenario CartaoEscolhido { get; private set; }

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        TelaAtual = telaInicial;
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

        Mostrar(telaInicial, FocoPadraoDaTelaAtual());
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
        gerenciadorDeCenas.AbrirSelecaoDeModo();
    }

    public void AbrirEscolhaDeCenario()
    {
        gerenciadorDeCenas.VoltarParaEscolhaDeCenario();
    }

    public void Voltar()
    {
        if (TelaAtual == Tela.Cenarios)
        {
            gerenciadorDeCenas.AbrirSelecaoDeModo();
        }
        else if (TelaAtual == Tela.Modos)
        {
            gerenciadorDeCenas.VoltarParaTelaInicial();
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
        if (painelModos != null) painelModos.SetActive(tela == Tela.Modos);
        if (painelCenarios != null) painelCenarios.SetActive(tela == Tela.Cenarios);

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

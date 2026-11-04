#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class EstacoesRuaTests
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    Component jogador;
    Component arma;
    Component pontos;

    Component[] Encontrar(string tipo) => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(x => x.GetComponentsInChildren(Type.GetType(tipo + ", Assembly-CSharp", true), true))
        .Where(x => x.gameObject.activeInHierarchy).ToArray();
    object Campo(Component alvo, string nome) => alvo.GetType().GetField(nome, Flags).GetValue(alvo);
    void Definir(Component alvo, string nome, object valor) => alvo.GetType().GetField(nome, Flags).SetValue(alvo, valor);
    void Chamar(Component alvo, string nome) => alvo.GetType().GetMethod(nome, Flags).Invoke(alvo, null);

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        Time.timeScale = 1;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/DerbySegundoTempo/Cenas/Fases/Rua_TPS.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var jogo = Encontrar("GerenciadorDeJogo").Single();
        Chamar(jogo, "FecharTutorial");
        Chamar(jogo, "Pausar");
        Chamar(jogo, "Continuar");
        foreach (var loja in Encontrar("LojaDeUpgrades"))
            Assert.IsFalse(((Behaviour)loja).enabled, "A loja global nao deve disputar as teclas 1 e 2 com as oficinas.");
        foreach (var inimigo in Encontrar("InimigoVida")) inimigo.gameObject.SetActive(false);
        ((Behaviour)Encontrar("Cronometro").Single()).enabled = false;
        jogador = Encontrar("JogadorMovimento").Single();
        arma = Encontrar("Arma").Single();
        pontos = Encontrar("SistemaDePontos").Single();
        Definir(pontos, "pontosAtuais", 1000);
    }

    IEnumerator Aproximar(Component estacao)
    {
        var corpo = jogador.GetComponent<Rigidbody>();
        corpo.position = estacao.transform.TransformPoint(new Vector3(0, .65f, -1.6f));
        corpo.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsTrue(((GameObject)Campo(estacao, "painelDeTexto")).activeSelf, "O gatilho deve abrir o painel: " + estacao.name);
    }

    IEnumerator Afastar(Component estacao)
    {
        var corpo = jogador.GetComponent<Rigidbody>();
        corpo.position = new Vector3(0, 1, -18);
        corpo.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsFalse(((GameObject)Campo(estacao, "painelDeTexto")).activeSelf, "O painel deve fechar ao sair.");
    }

    [UnityTest]
    public IEnumerator OficinasCompramUpgradesComPontos()
    {
        var oficinas = Encontrar("EstacaoDeUpgrade");
        Assert.AreEqual(2, oficinas.Length);
        foreach (var oficina in oficinas)
        {
            Assert.IsTrue(oficina.GetComponents<Collider>().Any(x => x.enabled && x.isTrigger));
            Assert.AreEqual(KeyCode.Alpha1, Campo(oficina, "teclaDano"));
            Assert.AreEqual(KeyCode.Alpha2, Campo(oficina, "teclaCapacidade"));
            yield return Aproximar(oficina);
            Assert.AreSame(arma, Campo(oficina, "armaDoJogador"));
            int saldo = (int)Campo(pontos, "pontosAtuais");
            int dano = (int)Campo(arma, "dano");
            int pente = (int)Campo(arma, "capacidadePente");
            Chamar(oficina, "ComprarUpgradeDeDano");
            Assert.AreEqual(dano + (int)Campo(oficina, "aumentoDeDano"), Campo(arma, "dano"));
            Assert.AreEqual(saldo - (int)Campo(oficina, "custoDoDano"), Campo(pontos, "pontosAtuais"));
            saldo = (int)Campo(pontos, "pontosAtuais");
            Chamar(oficina, "ComprarUpgradeDeCapacidade");
            Assert.AreEqual(pente + (int)Campo(oficina, "aumentoDeCapacidade"), Campo(arma, "capacidadePente"));
            Assert.AreEqual(saldo - (int)Campo(oficina, "custoDaCapacidade"), Campo(pontos, "pontosAtuais"));
            Definir(pontos, "pontosAtuais", 0);
            dano = (int)Campo(arma, "dano");
            pente = (int)Campo(arma, "capacidadePente");
            Chamar(oficina, "ComprarUpgradeDeDano");
            Chamar(oficina, "ComprarUpgradeDeCapacidade");
            Assert.AreEqual(dano, Campo(arma, "dano"));
            Assert.AreEqual(pente, Campo(arma, "capacidadePente"));
            yield return Afastar(oficina);
            Assert.IsNull(Campo(oficina, "armaDoJogador"));
            Definir(pontos, "pontosAtuais", 1000);
        }
    }

    [UnityTest]
    public IEnumerator FarmaciaERecargaUsamOsComponentesOriginais()
    {
        var farmacia = Encontrar("EstacaoDeCura").Single();
        var vida = Encontrar("JogadorVida").Single();
        Definir(vida, "vidaAtual", 20);
        yield return Aproximar(farmacia);
        int saldo = (int)Campo(pontos, "pontosAtuais");
        Chamar(farmacia, "Usar");
        Assert.AreEqual(Math.Min((int)Campo(vida, "vidaMaxima"), 20 + (int)Campo(farmacia, "quantidadeDeCura")), Campo(vida, "vidaAtual"));
        Assert.AreEqual(saldo - (int)Campo(farmacia, "custoEmPontos"), Campo(pontos, "pontosAtuais"));
        yield return Afastar(farmacia);
        var recarga = Encontrar("EstacaoDeMunicao").Single();
        yield return Aproximar(recarga);
        int reserva = (int)Campo(arma, "municaoReserva");
        saldo = (int)Campo(pontos, "pontosAtuais");
        Chamar(recarga, "Usar");
        Assert.AreEqual(reserva + (int)Campo(recarga, "quantidadeDeMunicao"), Campo(arma, "municaoReserva"));
        Assert.AreEqual(saldo - (int)Campo(recarga, "custoEmPontos"), Campo(pontos, "pontosAtuais"));
        Definir(pontos, "pontosAtuais", 0);
        reserva = (int)Campo(arma, "municaoReserva");
        Chamar(recarga, "Usar");
        Assert.AreEqual(reserva, Campo(arma, "municaoReserva"));
        yield return Afastar(recarga);
    }

    [UnityTearDown]
    public IEnumerator Limpar()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield return null;
    }
}
#endif

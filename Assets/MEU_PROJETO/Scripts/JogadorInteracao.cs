using UnityEngine;

public class JogadorInteracao : MonoBehaviour
{
    public float alcance = 1f; // distância minima para interagir
    public KeyCode teclaInteracao = KeyCode.E; // tecla de interação
    public Transform posicaoDoOlhoDoJogador; // camera
    public GameObject textoDeInteracao; // texto pressione [E] para interagir

    public IInteragivel objetoInteragivel;

    void Update()
    {
        DetectarObjetoInteragivel();
        ExibirTextoDeInteracao();
        TentarInteragir();
    }

    void DetectarObjetoInteragivel()
    {
        // reseta o objeto interagivel
        objetoInteragivel = null;

        // cria uma variável chamada raio do tipo Ray,
        // a partir da posição do jogador, na direção para frente
        Ray raio = new Ray(
            posicaoDoOlhoDoJogador.transform.position,
            transform.forward
        );

        // cria uma variável do tipo RaycastHit chamada atingiu
        RaycastHit atingiu;

        // dispara um raio do ponto de origem da câmera do jogador,
        // até atingir um collider, com a distância definida na variável alcance
        if (Physics.Raycast(raio, out atingiu, alcance))
        {
            // tenta encontrar um objeto interagivel, ou seja,
            // um objeto que implemente a interface IInteragivel
            // e guarda na variável objetoInteragivel
            objetoInteragivel = atingiu.collider.GetComponent<IInteragivel>();
        }
    }

    void ExibirTextoDeInteracao()
    {
        // se o alvo não for nulo, ou seja, atingiu algo interagivel
        if (objetoInteragivel != null)
        {
            // ativa o texto de interação na tela
            textoDeInteracao.SetActive(true);
        }
        // se o alvo for nulo, ou seja, não atingiu algo interagivel
        else if (objetoInteragivel == null)
        {
            // desativa o texto de interação na tela
            textoDeInteracao.SetActive(false);
        }
    }

    void TentarInteragir()
    {
        // se pressionou o botão de interação
        if (Input.GetKeyDown(teclaInteracao))
        {
            // se o alvo não for nulo, ou seja, atingiu algo interagivel
            if (objetoInteragivel != null)
            {
                // chama a função Interagir do alvo
                objetoInteragivel.Interagir();
            }
        }
    }

    // debug opcional: desenhar o raio na cena (aparece no Scene View)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            posicaoDoOlhoDoJogador.transform.position,
            posicaoDoOlhoDoJogador.transform.position + transform.forward * alcance
        );
    }
}
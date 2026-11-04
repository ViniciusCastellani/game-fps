using UnityEngine;

public class JogadorInteracao : MonoBehaviour
{
    public float alcance = 1f;
    public KeyCode teclaInteracao = KeyCode.E;
    public Transform posicaoDoOlhoDoJogador;
    public GameObject textoDeInteracao;

    public IInteragivel objetoInteragivel;

    void Update()
    {
        DetectarObjetoInteragivel();
        ExibirTextoDeInteracao();
        TentarInteragir();
    }

    void DetectarObjetoInteragivel()
    {
        objetoInteragivel = null;

        Ray raio = new Ray(
            posicaoDoOlhoDoJogador.transform.position,
            transform.forward
        );

        RaycastHit atingiu;

        if (Physics.Raycast(raio, out atingiu, alcance))
        {
            objetoInteragivel = atingiu.collider.GetComponent<IInteragivel>();
        }
    }

    void ExibirTextoDeInteracao()
    {
        if (objetoInteragivel != null)
        {
            textoDeInteracao.SetActive(true);
        }
        else if (objetoInteragivel == null)
        {
            textoDeInteracao.SetActive(false);
        }
    }

    void TentarInteragir()
    {
        if (Input.GetKeyDown(teclaInteracao))
        {
            if (objetoInteragivel != null)
            {
                objetoInteragivel.Interagir();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            posicaoDoOlhoDoJogador.transform.position,
            posicaoDoOlhoDoJogador.transform.position + transform.forward * alcance
        );
    }
}
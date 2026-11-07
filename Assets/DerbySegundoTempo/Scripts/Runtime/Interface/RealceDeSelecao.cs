using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class RealceDeSelecao : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] private GameObject indicadorDeFoco;
    [SerializeField] private float escalaEmFoco = 1.04f;

    public bool EmFoco { get; private set; }

    private Selectable selecionavel;
    private Vector3 escalaOriginal;

    void Awake()
    {
        selecionavel = GetComponent<Selectable>();
        escalaOriginal = transform.localScale;
        Aplicar(false);
    }

    public void OnPointerEnter(PointerEventData dados)
    {
        if (selecionavel != null && selecionavel.IsInteractable() && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void OnSelect(BaseEventData dados)
    {
        Aplicar(true);
    }

    public void OnDeselect(BaseEventData dados)
    {
        Aplicar(false);
    }

    void OnDisable()
    {
        Aplicar(false);
    }

    void Aplicar(bool focado)
    {
        EmFoco = focado;

        if (indicadorDeFoco != null)
        {
            indicadorDeFoco.SetActive(focado);
        }

        transform.localScale = focado ? escalaOriginal * escalaEmFoco : escalaOriginal;
    }
}

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class TriggerGol : MonoBehaviour
{
    public GolEasterEgg easterEgg;

    void OnTriggerEnter(Collider outro)
    {
        var corpo = outro.attachedRigidbody;
        if (easterEgg != null && corpo != null && corpo.TryGetComponent<BolaFutebol>(out var bola))
            easterEgg.RegistrarGol(bola, transform.position);
    }

    void OnDrawGizmosSelected()
    {
        var caixa = GetComponent<BoxCollider>();
        Gizmos.color = new Color(0, 1, .6f, .3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(caixa.center, caixa.size);
    }
}

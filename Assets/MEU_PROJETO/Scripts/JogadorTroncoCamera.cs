using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class JogadorTroncoCamera : MonoBehaviour
{
    public Animator animator;
    public Transform cameraJogador;

    [Tooltip("Quanto do ângulo vertical da câmera o tronco acompanha (1 = mão sempre no mesmo lugar da tela).")]
    [Range(0f, 1f)] public float fracaoDoOlhar = 0.8f;

    [Tooltip("Inclinação máxima do tronco, em graus (para cima ou para baixo).")]
    public float anguloMaximo = 60f;

    [Tooltip("Mantém o quadril do personagem sobre o eixo do Jogador (corrige corpo deslocado para o lado).")]
    public bool centralizarCorpo = true;

    [Tooltip("Esconde a cabeça do personagem (em primeira pessoa ela invade a visão ao inclinar o tronco).")]
    public bool esconderCabeca = true;

    private readonly List<Transform> ossos = new List<Transform>();
    private Transform quadril;
    private bool jaLogou;
    private Transform cabeca;
    private Vector3 escalaCabeca = Vector3.one;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraJogador == null && Camera.main != null) cameraJogador = Camera.main.transform;

        if (animator == null || !animator.isHuman || cameraJogador == null)
        {
            Debug.LogWarning("[JogadorTroncoCamera] Animator humanoide ou câmera não encontrados; componente desativado.");
            enabled = false;
            return;
        }

        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        AdicionarOsso(HumanBodyBones.Spine);
        AdicionarOsso(HumanBodyBones.Chest);
        AdicionarOsso(HumanBodyBones.UpperChest);

        quadril = animator.GetBoneTransform(HumanBodyBones.Hips);
        cabeca = animator.GetBoneTransform(HumanBodyBones.Head);
        if (cabeca != null) escalaCabeca = cabeca.localScale;
    }

    void AdicionarOsso(HumanBodyBones osso)
    {
        Transform t = animator.GetBoneTransform(osso);
        if (t != null) ossos.Add(t);
    }

    void LateUpdate()
    {
        if (ossos.Count == 0) return;

        if (centralizarCorpo && quadril != null) CentralizarCorpo();

        float pitch = Mathf.DeltaAngle(0f, cameraJogador.localEulerAngles.x);
        float angulo = Mathf.Clamp(pitch * fracaoDoOlhar, -anguloMaximo, anguloMaximo);

        Vector3 eixo = animator.transform.right;
        float porOsso = angulo / ossos.Count;
        foreach (Transform osso in ossos)
            osso.rotation = Quaternion.AngleAxis(porOsso, eixo) * osso.rotation;

        if (cabeca != null)
            cabeca.localScale = esconderCabeca ? Vector3.zero : escalaCabeca;
    }

    void CentralizarCorpo()
    {
        Transform raizModelo = animator.transform;

        if (!jaLogou)
        {
            jaLogou = true;
            Debug.Log("[JogadorTroncoCamera] Deslocamento do quadril em relação ao eixo do Jogador (metros, mundo): X=" +
                      (quadril.position.x - transform.position.x).ToString("F3") + "  Z=" +
                      (quadril.position.z - transform.position.z).ToString("F3"));
        }

        Vector3 h = quadril.position - raizModelo.position;
        raizModelo.position = new Vector3(transform.position.x - h.x, raizModelo.position.y, transform.position.z - h.z);
    }
}

using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
[DefaultExecutionOrder(150)]
public class AlternarCamera : MonoBehaviour
{
    public CinemachineCamera cameraPrimeiraPessoa;
    public CinemachineCamera cameraTerceiraPessoa;
    private bool primeiraPessoa = true;
    public bool PrimeiraPessoa => primeiraPessoa;
    private CinemachineBrain brain;
    private readonly List<MalhaPrimeiraPessoa> malhas = new List<MalhaPrimeiraPessoa>();

    private class MalhaPrimeiraPessoa
    {
        public SkinnedMeshRenderer renderer;
        public Mesh original;
        public Mesh semCabeca;
    }
    void Start()
    {
        if (Camera.main != null) brain = Camera.main.GetComponent<CinemachineBrain>();
        PrepararCorpoSemCabeca();
        AtualizarCamera();
    }
    void OnEnable()
    {
        AtualizarVisibilidade(false);
    }
    void OnDisable()
    {
        AtualizarVisibilidade(false);
    }
    void OnDestroy()
    {
        AtualizarVisibilidade(false);
        foreach (var malha in malhas)
            Destroy(malha.semCabeca);
    }

    void PrepararCorpoSemCabeca()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman) return;
        Transform cabeca = animator.GetBoneTransform(HumanBodyBones.Head);
        if (cabeca == null) return;

        foreach (var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Mesh original = renderer.sharedMesh;
            if (original == null) continue;
            if (!original.isReadable)
            {
                Debug.LogWarning("Ative Read/Write no modelo para ocultar a cabeça em primeira pessoa.", renderer);
                continue;
            }

            Transform[] bones = renderer.bones;
            bool[] ossoDaCabeca = new bool[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                ossoDaCabeca[i] = bones[i] != null
                    && (bones[i] == cabeca || bones[i].IsChildOf(cabeca));

            BoneWeight[] pesos = original.boneWeights;
            bool[] manter = new bool[original.vertexCount];
            // Preserve também vértices sem pesos; somente a geometria da cabeça é removida.
            for (int i = 0; i < manter.Length; i++) manter[i] = true;
            for (int i = 0; i < pesos.Length; i++)
            {
                BoneWeight p = pesos[i];
                float peso = PesoDaCabeca(ossoDaCabeca, p.boneIndex0, p.weight0)
                    + PesoDaCabeca(ossoDaCabeca, p.boneIndex1, p.weight1)
                    + PesoDaCabeca(ossoDaCabeca, p.boneIndex2, p.weight2)
                    + PesoDaCabeca(ossoDaCabeca, p.boneIndex3, p.weight3);
                manter[i] = peso < 0.5f;
            }

            Mesh corpoSemCabeca = Instantiate(original);
            corpoSemCabeca.name = original.name + "_PrimeiraPessoa";
            for (int sub = 0; sub < original.subMeshCount; sub++)
            {
                int[] triangles = original.GetTriangles(sub);
                var filtrados = new List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (!manter[triangles[i]] || !manter[triangles[i + 1]] || !manter[triangles[i + 2]]) continue;
                    filtrados.Add(triangles[i]);
                    filtrados.Add(triangles[i + 1]);
                    filtrados.Add(triangles[i + 2]);
                }
                corpoSemCabeca.SetTriangles(filtrados, sub, false);
            }
            malhas.Add(new MalhaPrimeiraPessoa { renderer = renderer, original = original, semCabeca = corpoSemCabeca });
        }
    }

    static float PesoDaCabeca(bool[] cabeca, int indice, float peso)
    {
        return indice >= 0 && indice < cabeca.Length && cabeca[indice] ? peso : 0f;
    }

    void AtualizarVisibilidade(bool primeira)
    {
        foreach (var malha in malhas)
            if (malha.renderer != null)
                malha.renderer.sharedMesh = primeira ? malha.semCabeca : malha.original;
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            primeiraPessoa = !primeiraPessoa;
            AtualizarCamera();
        }
    }
    void AtualizarCamera()
    {
        if (primeiraPessoa)
        {
            cameraPrimeiraPessoa.Priority = 20;
            cameraTerceiraPessoa.Priority = 10;
        }
        else
        {
            cameraPrimeiraPessoa.Priority = 10;
            cameraTerceiraPessoa.Priority = 20;
        }
    }
    void LateUpdate()
    {
        // O Brain atualiza em 100: só esconda a cabeça após chegar à câmera FP.
        bool chegouNaPrimeiraPessoa = primeiraPessoa && brain != null
            && !brain.IsBlending && ReferenceEquals(brain.ActiveVirtualCamera, cameraPrimeiraPessoa);
        AtualizarVisibilidade(chegouNaPrimeiraPessoa);
    }
}

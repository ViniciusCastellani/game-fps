"""Prefab de bola com malha procedural de pentagonos e hexagonos, sem texturas externas."""
from pathlib import Path
import itertools
import math
import re
import struct
import uuid
import numpy as np

ROOT = Path('Assets/DerbySegundoTempo/Cenarios/Estadio')
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
BASE = '  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
RADIUS = .27


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'derby-tps-visual/' + str(path)).hex


def meta(path, importer, main_id=None):
    content = f'fileFormatVersion: 2\nguid: {guid(path)}\n{importer}:\n  externalObjects: {{}}\n'
    if main_id is not None:
        content += f'  mainObjectFileID: {main_id}\n'
    Path(str(path) + '.meta').write_text(content)


def material(name, color):
    path = ROOT / 'Materiais' / f'{name}.mat'
    color = '{' + ', '.join(f'{k}: {v}' for k, v in zip('rgba', (*color, 1))) + '}'
    path.write_text(HEADER + f'''--- !u!21 &2100000
Material:
{BASE}  serializedVersion: 8
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _Surface: 0
    - _Cull: 2
    - _ZWrite: 1
    - _Metallic: 0
    - _Smoothness: 0.3
    m_Colors:
    - _BaseColor: {color}
    - _Color: {color}
''')
    meta(path, 'NativeFormatImporter', 2100000)
    return guid(path)


def soccer_mesh():
    phi = (1 + math.sqrt(5)) / 2
    ico = np.array([(0, a, b * phi) for a in [-1, 1] for b in [-1, 1]] +
                   [(a, b * phi, 0) for a in [-1, 1] for b in [-1, 1]] +
                   [(b * phi, 0, a) for a in [-1, 1] for b in [-1, 1]], dtype=float)
    adjacent = np.isclose(np.linalg.norm(ico[:, None] - ico[None, :], axis=2), 2)
    directed = {(a, b): (2 * ico[a] + ico[b]) / 3 for a in range(12) for b in range(12) if adjacent[a, b]}
    faces = [(0, [directed[a, b] for b in range(12) if adjacent[a, b]]) for a in range(12)]
    for a, b, c in itertools.combinations(range(12), 3):
        if adjacent[a, b] and adjacent[b, c] and adjacent[c, a]:
            faces.append((1, [directed[e] for e in [(a, b), (b, a), (b, c), (c, b), (c, a), (a, c)]]))
    assert sum(len(face) == 5 for _, face in faces) == 12
    assert sum(len(face) == 6 for _, face in faces) == 20
    triangles = [[], []]

    def add_triangle(a, b, c, group):
        if np.dot(np.cross(b-a, c-a), a+b+c) < 0:
            b, c = c, b
        # Subdivide e projeta sobre a esfera para manter a silhueta arredondada.
        ab, bc, ca = (a+b)/2, (b+c)/2, (c+a)/2
        for tri in [(a, ab, ca), (ab, b, bc), (ca, bc, c), (ab, bc, ca)]:
            triangles[group].append([point / np.linalg.norm(point) for point in tri])

    for group, polygon in faces:
        polygon = np.array(polygon)
        normal = polygon.mean(axis=0)
        normal /= np.linalg.norm(normal)
        axis = polygon[0] - polygon.mean(axis=0)
        axis /= np.linalg.norm(axis)
        other = np.cross(normal, axis)
        order = np.argsort(np.arctan2(polygon @ other, polygon @ axis))
        outer = polygon[order]
        outer /= np.linalg.norm(outer, axis=1)[:, None]
        inner = outer * .97 + normal * .03
        for k in range(len(outer)):
            n = (k+1) % len(outer)
            add_triangle(normal, inner[k], inner[n], group)
            add_triangle(outer[k], outer[n], inner[n], 0)
            add_triangle(outer[k], inner[n], inner[k], 0)

    vertices = []
    indices = []
    counts = []
    for group in triangles:
        start = len(indices)
        for triangle in group:
            for normal in triangle:
                position = normal * RADIUS
                vertices.append((*position, *normal, 0, 0))
                indices.append(len(vertices)-1)
        counts.append(len(indices)-start)
    assert len(vertices) < 65536
    return vertices, indices, counts


def criar_assets_bola():
    for folder in ['Materiais', 'Malhas', 'Prefabs']:
        (ROOT / folder).mkdir(parents=True, exist_ok=True)
    black = material('BolaPreta', (.015, .018, .022))
    white = material('BolaBranca', (.91, .9, .86))
    vertices, indices, counts = soccer_mesh()
    mesh_path = ROOT / 'Malhas' / 'BolaFutebol.asset'
    template = Path('Assets/Terceiros/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr star mesh.asset').read_text()
    submeshes = '  m_SubMeshes:\n'
    offset = 0
    for count in counts:
        submeshes += f'''  - serializedVersion: 2
    firstByte: {offset*2}
    indexCount: {count}
    topology: 0
    baseVertex: 0
    firstVertex: {offset}
    vertexCount: {count}
    localAABB:
      m_Center: {{x: 0, y: 0, z: 0}}
      m_Extent: {{x: {RADIUS}, y: {RADIUS}, z: {RADIUS}}}
'''
        offset += count
    template = template.replace('m_Name: cfxr star mesh', 'm_Name: BolaFutebol')
    template = re.sub(r'  m_SubMeshes:.*?(?=  m_Shapes:)', lambda _: submeshes, template, flags=re.S)
    template = re.sub(r'm_VertexCount: \d+', f'm_VertexCount: {len(vertices)}', template)
    template = re.sub(r'm_IndexBuffer: .*', 'm_IndexBuffer: ' + struct.pack('<' + str(len(indices)) + 'H', *indices).hex(), template)
    data = b''.join(struct.pack('<8f', *v) for v in vertices)
    template = re.sub(r'm_DataSize: \d+', f'm_DataSize: {len(data)}', template)
    template = re.sub(r'_typelessdata: .*', '_typelessdata: ' + data.hex(), template)
    template = template.replace('m_Extent: {x: 0.48046875, y: 0.46679688, z: 0}', f'm_Extent: {{x: {RADIUS}, y: {RADIUS}, z: {RADIUS}}}')
    mesh_path.write_text(template)
    meta(mesh_path, 'NativeFormatImporter', 4300000)
    physics_path = ROOT / 'Materiais' / 'BolaFisica.physicMaterial'
    physics_path.write_text(HEADER + f'''--- !u!134 &13400000
PhysicsMaterial:
{BASE}  m_Name: BolaFisica
  serializedVersion: 2
  m_DynamicFriction: 0.4
  m_StaticFriction: 0.4
  m_Bounciness: 0.35
  m_FrictionCombine: 3
  m_BounceCombine: 0
''')
    meta(physics_path, 'NativeFormatImporter', 13400000)
    script = Path('Assets/DerbySegundoTempo/Scripts/Runtime/Cenario/BolaFutebol.cs')
    path = ROOT / 'Prefabs' / 'BolaFutebol.prefab'
    path.write_text(HEADER + f'''--- !u!1 &1000
GameObject:
{BASE}  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1001}}
  - component: {{fileID: 1002}}
  - component: {{fileID: 1003}}
  - component: {{fileID: 1004}}
  m_Layer: 0
  m_Name: BolaFutebol
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &1001
Transform:
{BASE}  m_GameObject: {{fileID: 1000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children:
  - {{fileID: 1011}}
  m_Father: {{fileID: 0}}
--- !u!54 &1002
Rigidbody:
{BASE}  m_GameObject: {{fileID: 1000}}
  serializedVersion: 5
  m_Mass: 0.43
  m_LinearDamping: 0.08
  m_AngularDamping: 0.05
  m_CenterOfMass: {{x: 0, y: 0, z: 0}}
  m_InertiaTensor: {{x: 1, y: 1, z: 1}}
  m_InertiaRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ImplicitCom: 1
  m_ImplicitTensor: 1
  m_UseGravity: 1
  m_IsKinematic: 0
  m_Interpolate: 1
  m_Constraints: 0
  m_CollisionDetection: 2
--- !u!135 &1003
SphereCollider:
{BASE}  m_GameObject: {{fileID: 1000}}
  m_Material: {{fileID: 13400000, guid: {guid(physics_path)}, type: 2}}
  m_IsTrigger: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Radius: {RADIUS}
  m_Center: {{x: 0, y: 0, z: 0}}
--- !u!114 &1004
MonoBehaviour:
{BASE}  m_GameObject: {{fileID: 1000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(script)}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::BolaFutebol
  multiplicadorAndando: 0.85
  multiplicadorCorrendo: 1.3
  velocidadeMaxima: 12
  intervaloEntreToques: 0.12
  desaceleracaoNoSolo: 0.8
  velocidadeMinimaDoJogador: 0.15
--- !u!1 &1010
GameObject:
{BASE}  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1011}}
  - component: {{fileID: 1012}}
  - component: {{fileID: 1013}}
  m_Layer: 0
  m_Name: VisualBola
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &1011
Transform:
{BASE}  m_GameObject: {{fileID: 1010}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children: []
  m_Father: {{fileID: 1001}}
--- !u!33 &1012
MeshFilter:
{BASE}  m_GameObject: {{fileID: 1010}}
  m_Mesh: {{fileID: 4300000, guid: {guid(mesh_path)}, type: 2}}
--- !u!23 &1013
MeshRenderer:
{BASE}  m_GameObject: {{fileID: 1010}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_RenderingLayerMask: 1
  m_Materials:
  - {{fileID: 2100000, guid: {black}, type: 2}}
  - {{fileID: 2100000, guid: {white}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
''')
    meta(path, 'PrefabImporter')
    return guid(path)


if __name__ == '__main__':
    print(criar_assets_bola())

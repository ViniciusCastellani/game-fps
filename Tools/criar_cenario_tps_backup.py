"""Gera assets decorativos e uma copia independente da cena TPS. Requer Pillow e NumPy.
Execute na raiz do projeto. Nao modifica a cena nem o prefab originais.
"""
from pathlib import Path
import struct
import re, math, uuid, json, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=Path('Assets/DerbySegundoTempo/Cenarios/Estadio')
HEADER='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
def guid(p):return uuid.uuid5(uuid.NAMESPACE_URL,'derby-tps-visual/'+str(p)).hex
def meta(p,kind='DefaultImporter'):
 p=Path(p);Path(str(p)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(p)}\n{kind}:\n  externalObjects: {{}}\n{('  mainObjectFileID: 2100000' + chr(10)) if kind == 'NativeFormatImporter' else ''}  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
for folder in [ROOT,ROOT/'Materiais',ROOT/'Texturas',ROOT/'Prefabs',ROOT/'Malhas']:
 folder.mkdir(parents=True,exist_ok=True)
 Path(str(folder)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(folder)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n')
# Reutiliza o shader URP instalado no projeto.
shader='933532a4fcc9baf4fa0491de14d08ed7'
def material(name,color,texture=None,emission=False,shader_override=None):
 shader_id = shader_override or ('650dd9526735d5b46b79224bc6e94025' if texture else shader)
 p=ROOT/'Materiais'/f'{name}.mat'; c='{'+', '.join(f'{k}: {v}' for k,v in zip('rgba',color))+'}'
 tex=f'{{fileID: 2800000, guid: {guid(texture)}, type: 3}}' if texture else '{fileID: 0}'
 p.write_text(HEADER+f'''--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: {shader_id}, type: 3}}
  m_ValidKeywords: {'[_EMISSION]' if emission else '[]'}
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {tex}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _Surface: 0
    - _Cull: 2
    - _ZWrite: 1
    - _Metallic: 0
    - _Smoothness: 0.25
    m_Colors:
    - _BaseColor: {c}
    - _Color: {c}
    - _EmissionColor: {c if emission else '{r: 0, g: 0, b: 0, a: 1}'}
''');meta(p,'NativeFormatImporter');return guid(p)
M={n:material(n,c,emission=e) for n,c,e in [
 ('Grafite',(.035,.045,.055,1),False),('Marfim',(.82,.82,.76,1),False),('Amarelo',(.95,.59,.055,1),False),('Verde',(.08,.65,.35,1),False),('Metal',(.18,.21,.24,1),False)]}
font='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
def sign(name,title,subtitle,accent=(214,145,45)):
 p=ROOT/'Texturas'/f'{name}.png';im=Image.new('RGB',(1024,256),(18,23,28));d=ImageDraw.Draw(im)
 d.rectangle((0,0,22,256),fill=accent);d.rectangle((35,222,990,230),fill=accent)
 f=ImageFont.truetype(font,70)
 while d.textlength(title,font=f)>930:f=ImageFont.truetype(font,f.size-1)
 d.text((54,38),title,font=f,fill=(245,243,230));sf=ImageFont.truetype(font,28)
 while d.textlength(subtitle,font=sf)>910:sf=ImageFont.truetype(font,sf.size-1)
 d.text((57,146),subtitle,font=sf,fill=accent)
 im.save(p)
 # Explicit texture import avoids inheriting a Sprite default.
 Path(str(p)+'.meta').write_text(f'''fileFormatVersion: 2
guid: {guid(p)}
TextureImporter:
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 1
    sRGBTexture: 1
  isReadable: 0
  textureType: 0
  textureShape: 1
  maxTextureSize: 1024
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  alphaIsTransparency: 0
  spriteMode: 0
  textureCompression: 1
''')
 M[name]=material(name,(1,1,1,1),p)
for args in [('Derby','DERBY','SEGUNDO TEMPO  /  ARENA 01'),('Torcida','ATE O APITO FINAL','NOSSA CASA. NOSSA ARENA.')]:sign(*args)
# Mesh plano com UV explicitas: toda arte usa a mesma frente -Z e topo +Y.
# Reutiliza o formato de um Mesh nativo presente no proprio projeto.
mesh_path=ROOT/'Malhas'/'PainelUV.asset'
template=Path('Assets/Terceiros/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr star mesh.asset').read_text()
vertices=[(-.5,-.5,0,0,0,-1,0,0),(.5,-.5,0,0,0,-1,1,0),(.5,.5,0,0,0,-1,1,1),(-.5,.5,0,0,0,-1,0,1)]
vertex_data=b''.join(struct.pack('<8f',*v) for v in vertices)
mesh=template.replace('m_Name: cfxr star mesh','m_Name: PainelUV')
mesh=re.sub(r'indexCount: 111','indexCount: 6',mesh)
mesh=re.sub(r'vertexCount: 39','vertexCount: 4',mesh)
mesh=re.sub(r'm_VertexCount: 39','m_VertexCount: 4',mesh)
mesh=re.sub(r'm_IndexBuffer: .*','m_IndexBuffer: '+struct.pack('<6H',0,2,1,0,3,2).hex(),mesh)
mesh=re.sub(r'm_DataSize: \d+','m_DataSize: 128',mesh)
mesh=re.sub(r'_typelessdata: .*','_typelessdata: '+vertex_data.hex(),mesh)
mesh=mesh.replace('m_Extent: {x: 0.48046875, y: 0.46679688, z: 0}','m_Extent: {x: 0.5, y: 0.5, z: 0}')
mesh_path.write_text(mesh)
Path(str(mesh_path)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(mesh_path)}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 4300000\n')
# Paleta aplicada apenas por overrides na copia da cena.
# A textura original e extraida sem alteracao; o shader remapeia so a cor saturada dos assentos.
from derby_scene_geometry import j as gltf, binary as gltf_binary
seat_source=gltf['materials'][3]['pbrMetallicRoughness']['baseColorTexture']['index']
seat_image=gltf['images'][gltf['textures'][seat_source]['source']]
view=gltf['bufferViews'][seat_image['bufferView']]
seat_texture=ROOT/'Texturas'/'AssentosOriginal.png'
seat_texture.write_bytes(gltf_binary[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']])
Path(str(seat_texture)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(seat_texture)}\nTextureImporter:\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 1\n    sRGBTexture: 1\n  textureType: 0\n  maxTextureSize: 1024\n')
seat_shader=ROOT/'Materiais'/'ArenaSeats.shader'
seat_shader.write_text('''Shader "Derby/ArenaSeats"
{
    Properties
    {
        _BaseMap("Original seat atlas", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Cutoff", Float) = 0.5
        _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _Cull;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 original=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb;
                half saturation=max(max(original.r,original.g),original.b)-min(min(original.r,original.g),original.b);
                half3 albedo=lerp(original,half3(.78,.77,.72),smoothstep(.08,.25,saturation));
                half3 normal=normalize(input.normalWS);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 illumination=SampleSH(normal)+sun.color*saturate(dot(normal,sun.direction))*sun.shadowAttenuation;
                return half4(albedo*illumination,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
''')
meta(seat_shader,'ShaderImporter')
M['AssentosClaros']=material('AssentosClaros',(1,1,1,1),seat_texture,shader_override=guid(seat_shader))
M['AssentosEscuros']=material('AssentosEscuros',(.115,.13,.14,1))
M['PublicidadeArena']=material('PublicidadeArena',(.055,.065,.072,1))
# Prefabs estaticos: componentes nativos, sem scripts de execucao ou colisores.
BASE='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
def vec(v,keys='xyz'):return '{'+', '.join(f'{k}: {float(x):.8g}' for k,x in zip(keys,v))+'}'
class Prefab:
 def __init__(self,name):self.name=name;self.nodes=[];self.add(name,None)
 def add(self,name,parent=0,pos=(0,0,0),scale=(1,1,1),mat=None,yaw=0,quad=False):
  i=len(self.nodes);self.nodes.append(dict(name=name,parent=parent,pos=pos,scale=scale,mat=mat,yaw=yaw,quad=quad));return i
 def cube(self,name,pos,scale,mat,parent=0,yaw=0):return self.add(name,parent,pos,scale,mat,yaw)
 def save(self):
  s=HEADER
  for i,n in enumerate(self.nodes):
   go=1000+i*10;t=go+1;mat=n['mat'];components=[t]+([go+2,go+3] if mat else [])
   s+=f'--- !u!1 &{go}\nGameObject:\n'+BASE+'  serializedVersion: 6\n  m_Component:\n'+''.join(f'  - component: {{fileID: {c}}}\n' for c in components)+f"  m_Layer: 2\n  m_Name: {n['name']}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n"
   a=math.radians(n['yaw'])/2;children=[1001+j*10 for j,k in enumerate(self.nodes) if k['parent']==i]
   s+=f'--- !u!4 &{t}\nTransform:\n'+BASE+f"  m_GameObject: {{fileID: {go}}}\n  serializedVersion: 2\n  m_LocalRotation: {vec((0,math.sin(a),0,math.cos(a)),'xyzw')}\n  m_LocalPosition: {vec(n['pos'])}\n  m_LocalScale: {vec(n['scale'])}\n  m_ConstrainProportionsScale: 0\n"+('  m_Children:\n'+''.join(f'  - {{fileID: {c}}}\n' for c in children) if children else '  m_Children: []\n')+f"  m_Father: {{fileID: {0 if n['parent'] is None else 1001+n['parent']*10}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: {n['yaw']}, z: 0}}\n"
   if mat:
    s+=f'--- !u!33 &{go+2}\nMeshFilter:\n'+BASE+f'  m_GameObject: {{fileID: {go}}}\n  m_Mesh: {('{fileID: 4300000, guid: '+guid(mesh_path)+', type: 2}') if n['quad'] else '{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}'}\n'
    s+=f'--- !u!23 &{go+3}\nMeshRenderer:\n'+BASE+f'''  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RenderingLayerMask: 1
  m_Materials:
  - {{fileID: 2100000, guid: {M[mat]}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
'''
  p=ROOT/'Prefabs'/f'{self.name}.prefab';p.write_text(s);meta(p,'PrefabImporter');return guid(p)
P={}
PREFAB_ROOTS={}
from criar_bola_futebol import criar_assets_bola, RADIUS as BALL_RADIUS
P['BolaFutebol']=criar_assets_bola()
def face(p,art,pos,size):
 return p.add('ArteFrenteUV',pos=pos,scale=(*size,1),mat=art,quad=True)
# Placas baixas com pes, travessa e arte somente na face correta.
for name,art in [('PlacaDerby','Derby'),('FaixaTorcida','Torcida')]:
 p=Prefab(name);w=4.8
 for x in [-1.85,1.85]:
  p.cube('SapataApoiada',(x,.035,.12),(.16,.07,.85),'Metal')
  p.cube('Suporte',(x,.38,.12),(.085,.76,.085),'Metal')
 p.cube('Quadro',(0,.78,.06),(w+.12,1.32,.12),'Grafite')
 face(p,art,(0,.78,-.008),(w,1.2))
 p.cube('TravessaInferior',(0,.15,.15),(3.85,.06,.08),'Metal')
 for x in [-2.43,2.43]:p.cube('PerfilLateral',(x,.78,-.02),(.05,1.35,.055),'Metal')
 P[name]=p.save()
# As estacoes usam apenas os itens e os textos de proximidade originais.
# Nenhuma placa, coluna ou cobertura extra e colocada em frente ao banco.
# Delimitador baixo de manutencao: base pesada, refletivos e altura humana.
p=Prefab('BalizadorArena')
p.cube('Base',(0,.06,0),(.46,.12,.46),'Grafite')
p.cube('Corpo',(0,.5,0),(.12,.88,.12),'Amarelo')
for y in [.3,.55,.8]:p.cube('AnelRefletivo',(0,y,0),(.135,.075,.135),'Marfim')
P[p.name]=p.save()
# Bandeirinha pequena de escanteio, presa ao mastro ate o solo.
p=Prefab('BandeirinhaEscanteio')
p.cube('BaseNoGramado',(0,.015,0),(.13,.03,.13),'Grafite')
p.cube('Mastro',(0,.9,0),(.035,1.8,.035),'Marfim')
p.cube('TopoMastro',(0,1.79,0),(.055,.045,.055),'Metal')
p.cube('Bandeirinha',(0.255,1.565,0),(.5,.35,.018),'Amarelo')
p.cube('FaixaDeIdentidade',(0.405,1.565,-.001),(.075,.35,.022),'Grafite')
P[p.name]=p.save()

# Reutiliza as barreiras do projeto com colisor e carving nativo de NavMesh.
# Os prefabs e a malha de navegacao originais continuam intactos.
for name,index in [('CoberturaCentralBaixa',1),('CoberturaCentralAlta',2)]:
 source_barrier=Path(f'Assets/Terceiros/Abandoned World/Metal and Concrete Barrier/Prefabs/Concrete_Barrier_{index}.prefab')
 data=source_barrier.read_text()
 go_id=int(re.search(r'--- !u!1 &(-?\d+)',data)[1])
 root_id=int(re.search(r'--- !u!4 &(-?\d+)',data)[1])
 collider=data[data.index('BoxCollider:'):]
 def collider_vector(key):
  return np.array([float(part.split(':')[1]) for part in re.search(key+r': \{([^}]+)',collider)[1].split(',')])
 size=collider_vector('m_Size');center=collider_vector('m_Center')
 data=data.replace('  m_Layer: 0','  m_Layer: 6',1)
 data=data.replace(f'  m_Name: Concrete_Barrier_{index}',f'  m_Name: {name}',1)
 data=data.replace('  m_Layer: 6','  - component: {fileID: 998000001}\n  m_Layer: 6',1)
 data+=f'''--- !u!208 &998000001
NavMeshObstacle:
{BASE}  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  serializedVersion: 3
  m_Shape: 1
  m_Extents: {vec(size/2)}
  m_MoveThreshold: 0.1
  m_Carve: 1
  m_CarveOnlyStationary: 1
  m_Center: {vec(center)}
  m_TimeToStationary: 0.1
'''
 target=ROOT/'Prefabs'/f'{name}.prefab';target.write_text(data);meta(target,'PrefabImporter')
 P[name]=guid(target);PREFAB_ROOTS[name]=root_id
# Calcula matrizes completas do prefab importado, evitando assumir escala 1.
s=Path('Assets/DerbySegundoTempo/Prefabs/Cenario/Estadio.prefab').read_text();gos={};ts={}
for d in re.split(r'(?=^--- !u!)',s,flags=re.M):
 h=re.match(r'--- !u!(\d+) &(-?\d+)',d)
 if not h:continue
 typ,i=map(int,h.groups())
 if typ==1:gos[i]=re.search(r'  m_Name: (.*)',d)[1]
 if typ==4:
  def v(k):return [float(x.split(':')[1]) for x in re.search(k+r': \{([^}]+)',d)[1].split(',')]
  q=np.array(v('m_LocalRotation'));q/=np.linalg.norm(q);x,y,z,w=q
  r=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
  m=np.eye(4);m[:3,:3]=r@np.diag(v('m_LocalScale'));m[:3,3]=v('m_LocalPosition')
  ts[i]=(int(re.search(r'm_GameObject: \{fileID: (-?\d+)',d)[1]),int(re.search(r'm_Father: \{fileID: (-?\d+)',d)[1]),m)
def world(i):
 g,f,m=ts[i];return world(f)@m if f else m
items={gos[g]:world(i) for i,(g,f,m) in ts.items()}
source=Path('Assets/DerbySegundoTempo/Cenas/Arquivo/Estadio_TPS.unity');scene=source.read_text();addition='';instances=[];placements=[]
def place(name,pos,yaw=0,scale=(1,1,1)):
 global addition
 i=810000000+len(instances)*10;instances.append(i);pg=P[name];a=math.radians(yaw)/2;root_id=PREFAB_ROOTS.get(name,1001)
 mods=''
 for prop,values,keys in [('m_LocalPosition',pos,'xyz'),('m_LocalRotation',(0,math.sin(a),0,math.cos(a)),'xyzw'),('m_LocalScale',scale,'xyz')]:
  for k,v in zip(keys,values):mods+=f'    - target: {{fileID: {root_id}, guid: {pg}, type: 3}}\n      propertyPath: {prop}.{k}\n      value: {float(v):.9g}\n      objectReference: {{fileID: 0}}\n'
 addition+=f'''--- !u!1001 &{i}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 809999991}}
    m_Modifications:
{mods}    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {pg}, type: 3}}
--- !u!4 &{i+1} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: {root_id}, guid: {pg}, type: 3}}
  m_PrefabInstance: {{fileID: {i}}}
  m_PrefabAsset: {{fileID: 0}}
'''
 placements.append({'prefab':name,'position':[float(v) for v in pos],'yaw':yaw,'scale':list(scale)})
# Altura obtida da superficie horizontal do gramado no GLB original.
# Consulta triangulos de verdade em cada ponto, em vez de presumir um plano.
from derby_scene_geometry import mesh_data
floor_vertices,floor_faces,_=mesh_data(16)
floor_tri=floor_vertices[floor_faces]
normals=np.cross(floor_tri[:,1]-floor_tri[:,0],floor_tri[:,2]-floor_tri[:,0])
horizontal=np.abs(normals[:,1])>np.linalg.norm(normals,axis=1)*.99
def ground(x,z):
 a=floor_tri[:,0][:,[0,2]];e=floor_tri[:,1][:,[0,2]]-a;f=floor_tri[:,2][:,[0,2]]-a;d=np.array([x,z])-a
 den=e[:,0]*f[:,1]-e[:,1]*f[:,0];den=np.where(np.abs(den)<1e-8,np.nan,den)
 u=(d[:,0]*f[:,1]-d[:,1]*f[:,0])/den;v=(e[:,0]*d[:,1]-e[:,1]*d[:,0])/den
 hit=horizontal&(u>=-1e-5)&(v>=-1e-5)&(u+v<=1.00001)
 heights=floor_tri[:,0,1]+u*(floor_tri[:,1,1]-floor_tri[:,0,1])+v*(floor_tri[:,2,1]-floor_tri[:,0,1])
 heights=heights[hit];heights=heights[np.abs(heights+22.862)<.1]
 if not len(heights):raise ValueError(f'No pitch surface at {x}, {z}')
 return float(heights.max())
def grounded(name,x,z,yaw=0,scale=(1,1,1)):place(name,(x,ground(x,z),z),yaw,scale)
# Apenas placas baixas nas duas linhas de fundo; frente sempre para o campo.
for z,yaw in [(3873.8,180),(3944.0,0)]:
 for x in ([-27,30,55] if z<3900 else [-27,8,43]):grounded('PlacaDerby' if x in [8,30] else 'FaixaTorcida',x,z,yaw)
# Sinalizacao discreta nas extremidades; nenhum mastro ou bandeira no ar.
for x in [-33,49]:
 for z in [3874.1,3943.6]:grounded('BalizadorArena',x,z)
# Os quatro cantos seguem as linhas do gramado, e nao as paredes do estadio.
from derby_scene_geometry import pitch_corners
for x,y,z in pitch_corners():
 grounded('BandeirinhaEscanteio',x,z,0 if x>11 else 180)
# Coberturas em grupos abertos: rotas livres entre as formacoes e longe dos postos.
for name,x,z,yaw in [
 ('CoberturaCentralBaixa',-10,3896,0),
 ('CoberturaCentralAlta',-10,3898.7,0),
 ('CoberturaCentralBaixa',-7.8,3894.8,90),
 ('CoberturaCentralAlta',0,3908,90),
 ('CoberturaCentralBaixa',2.7,3908,90),
 ('CoberturaCentralBaixa',3.9,3910.2,0),
 ('CoberturaCentralBaixa',23,3914,0),
 ('CoberturaCentralAlta',23,3916.7,0),
 ('CoberturaCentralAlta',43,3921,90),
 ('CoberturaCentralBaixa',45.7,3921,90),
]:grounded(name,x,z,yaw,(1.45,1.45,1.45))
# A poucos passos da posicao inicial do jogador, fora das novas coberturas.
place('BolaFutebol',(7.95,ground(7.95,3910.2)+BALL_RADIUS+.015,3910.2))
root=f'''--- !u!1 &809999990
GameObject:
{BASE}  serializedVersion: 6
  m_Component:
  - component: {{fileID: 809999991}}
  m_Layer: 2
  m_Name: Melhorias_Visuais_TPS
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &809999991
Transform:
{BASE}  m_GameObject: {{fileID: 809999990}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children:
'''+''.join(f'  - {{fileID: {i+1}}}\n' for i in instances)+'  m_Father: {fileID: 0}\n'
# Materiais da instancia original permanecem intactos; somente esta cena recebe os overrides.
original_materials={'2157741859597789458':'AssentosClaros','3737767757542052234':'AssentosEscuros','2375587592274999025':'PublicidadeArena'}
overrides=''
for block in re.split(r'(?=^--- !u!)',s,flags=re.M):
 head=re.match(r'--- !u!23 &(-?\d+)',block)
 if not head:continue
 for original,mat_name in original_materials.items():
  if f'fileID: {original}, guid: 2db67a377cb151749990707a8ff72f88' not in block:continue
  overrides+=f'    - target: {{fileID: {head[1]}, guid: 629a21ef4ec4bbe4ca49edd31d944c43, type: 3}}\n      propertyPath: m_Materials.Array.data[0]\n      value: \n      objectReference: {{fileID: 2100000, guid: {M[mat_name]}, type: 2}}\n'
instance_start=scene.index('--- !u!1001 &3731437572866092453')
instance_end=scene.index('    m_RemovedComponents:',instance_start)
scene=scene[:instance_end]+overrides+scene[instance_end:]
# Preserva a luz e a atmosfera originais: a remocao de nevoa extra mantem a leitura do combate.
marker='--- !u!1660057539 &9223372036854775807'
scene=scene.replace(marker,root+addition+marker);scene+='  - {fileID: 809999991}\n'
out=Path('Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity');out.write_text(scene);meta(out)
# Um bake feito na copia deve continuar independente dos dados usados pela cena original.
own_nav=Path('Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual/NavMesh-NavMesh Surface.asset')
if own_nav.exists():
 own_guid=re.search(r'^guid: (\w+)',Path(str(own_nav)+'.meta').read_text(),re.M)[1]
 scene=scene.replace('m_NavMeshData: {fileID: 23800000, guid: 2a5b713bf8ea2aa4a96ebec1c50566a8, type: 2}',f'm_NavMeshData: {{fileID: 23800000, guid: {own_guid}, type: 2}}')
 out.write_text(scene)
(ROOT/'posicionamento.json').write_text(json.dumps(placements,indent=2));meta(ROOT/'posicionamento.json')
print(f'Criados {len(P)} prefabs, {len(placements)} instancias e {out}')

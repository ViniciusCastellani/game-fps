from pathlib import Path
import json,struct,re
import numpy as np
b=Path('Assets/DerbySegundoTempo/Arte/Modelos/soccer_stadium.glb').read_bytes();l=struct.unpack_from('<I',b,12)[0];j=json.loads(b[20:20+l]);off=20+l;blen=struct.unpack_from('<I',b,off)[0];binary=b[off+8:off+8+blen]
def accessor(i):
 a=j['accessors'][i];v=j['bufferViews'][a['bufferView']];dtype={5126:'<f4',5125:'<u4',5123:'<u2',5121:'u1'}[a['componentType']];n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']];o=v.get('byteOffset',0)+a.get('byteOffset',0);stride=v.get('byteStride',n*np.dtype(dtype).itemsize)
 return np.ndarray((a['count'],n),dtype=dtype,buffer=binary,offset=o,strides=(stride,np.dtype(dtype).itemsize)).copy()
s=Path('Assets/DerbySegundoTempo/Prefabs/Cenario/Estadio.prefab').read_text();go={};ts={}
for d in re.split(r'(?=^--- !u!)',s,flags=re.M):
 h=re.match(r'--- !u!(\d+) &(-?\d+)',d)
 if not h:continue
 t,i=map(int,h.groups())
 if t==1:go[i]=re.search(r'  m_Name: (.*)',d)[1]
 if t==4:
  def v(k):return np.array([float(x.split(':')[1]) for x in re.search(k+r': \{([^}]+)',d)[1].split(',')])
  q=v('m_LocalRotation');q/=np.linalg.norm(q);x,y,z,w=q;r=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
  m=np.eye(4);m[:3,:3]=r@np.diag(v('m_LocalScale'));m[:3,3]=v('m_LocalPosition');ts[i]=(int(re.search(r'm_GameObject: \{fileID: (-?\d+)',d)[1]),int(re.search(r'm_Father: \{fileID: (-?\d+)',d)[1]),m)
def world(i):
 g,f,m=ts[i];return world(f)@m if f else m
trans={go[g]:world(i) for i,(g,f,m) in ts.items() if go[g]!='defaultMaterial'}
parents={}
for i,n in enumerate(j['nodes']):
 for c in n.get('children',[]):parents[c]=n['name']
def mesh_data(mi):
 ni=next(i for i,n in enumerate(j['nodes']) if n.get('mesh')==mi);p=j['meshes'][mi]['primitives'][0];v=accessor(p['attributes']['POSITION']);m=trans[parents[ni]];v=(m[:3,:3]@v.T).T+m[:3,3];f=accessor(p['indices']).reshape(-1,3)
 return v,f,p

def pitch_corners():
 """Intersecoes das linhas brancas do atlas original, projetadas no gramado."""
 vertices,faces,primitive=mesh_data(16)
 triangles=vertices[faces]
 texcoords=accessor(primitive['attributes']['TEXCOORD_0'])[faces]
 floor=np.max(np.abs(triangles[:,:,1]+22.86199),axis=1)<.001
 a=texcoords[:,0];e=texcoords[:,1]-a;f=texcoords[:,2]-a
 den=e[:,0]*f[:,1]-e[:,1]*f[:,0]
 den=np.where(np.abs(den)<1e-8,np.nan,den)
 corners=[]
 for pixel_x,pixel_y in [(502,99),(939,99),(502,923),(939,923)]:
  delta=np.array([pixel_x/1024,pixel_y/1024])-a
  u=(delta[:,0]*f[:,1]-delta[:,1]*f[:,0])/den
  v=(e[:,0]*delta[:,1]-e[:,1]*delta[:,0])/den
  hit=floor&(u>=0)&(v>=0)&(u+v<=1)
  points=triangles[:,0]+u[:,None]*(triangles[:,1]-triangles[:,0])+v[:,None]*(triangles[:,2]-triangles[:,0])
  if not np.any(hit):raise ValueError('Canto fora da malha do gramado')
  corners.append(points[hit][0])
 return np.array(corners)
if __name__=='__main__':
 v,f,p=mesh_data(16);t=v[f];n=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);sel=np.abs(n[:,1])>np.linalg.norm(n,axis=1)*.99
 print('Field bounds',v.min(0),v.max(0));ys=np.round(t[sel,:,1].mean(1),3);vals,counts=np.unique(ys,return_counts=True);print('Horizontal surface levels',sorted(zip(counts,vals),reverse=True)[:30])
 for x,z in [(8,3874),(4,3874),(-20,3900),(30,3943),(45,3880),(0,3900)]:
  a=t[:,0][:,[0,2]];e=t[:,1][:,[0,2]]-a;f2=t[:,2][:,[0,2]]-a;d=np.array([x,z])-a;den=e[:,0]*f2[:,1]-e[:,1]*f2[:,0];safe=np.where(abs(den)<1e-8,np.nan,den);u=(d[:,0]*f2[:,1]-d[:,1]*f2[:,0])/safe;vv=(e[:,0]*d[:,1]-e[:,1]*d[:,0])/safe;hit=sel&(u>=0)&(vv>=0)&(u+vv<=1);heights=t[:,0,1]+u*(t[:,1,1]-t[:,0,1])+vv*(t[:,2,1]-t[:,0,1]);print('FLOOR',x,z,heights[hit])

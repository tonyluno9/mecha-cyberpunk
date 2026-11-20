
"""cc_audit.py - revision automatica de vehiculos exportados (carros\*.fbx)"""
import bpy, bmesh, os, math, numpy as np
from mathutils import Vector
import cc_tools as C
CAR=os.path.join(C.ROOT,'carros')
def imp(path):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    return [o for o in bpy.data.objects if o not in before]
def wverts(o):
    return np.array([tuple(o.matrix_world@v.co) for v in o.data.vertices])
def mesh_stats(o):
    bm=bmesh.new(); bm.from_mesh(o.data)
    bnd=sum(1 for e in bm.edges if e.is_boundary)
    nm=sum(1 for e in bm.edges if not e.is_manifold and not e.is_boundary)
    # caras invertidas: normales que apuntan hacia el centro (aprox, solo carroceria)
    bm.free(); return bnd,nm
def audit(code):
    path=os.path.join(CAR,code+'.fbx')
    new=imp(path); bpy.context.view_layer.update()
    root=[o for o in new if o.parent is None][0]
    ms={o.name.split('.')[0]:o for o in new if o.type=='MESH'}
    r={'code':code}
    body=ms.get('Carroceria') or ms.get('Cuerpo')
    allP=np.concatenate([wverts(o) for o in ms.values()])
    mn,mx=allP.min(0),allP.max(0)
    r['L']=round(float(mx[1]-mn[1]),2); r['W']=round(float(mx[0]-mn[0]),2); r['H']=round(float(mx[2]-mn[2]),2)
    r['pivote']=[round(float((mn[0]+mx[0])/2),2),round(float((mn[1]+mx[1])/2),2),round(float(mn[2]),3)]
    wheels={k:o for k,o in ms.items() if k.startswith('Rueda')}
    r['ruedas']=len(wheels)
    wz=[]; wc=[]
    for k,o in wheels.items():
        P=wverts(o); c=(P.min(0)+P.max(0))/2; piv=o.matrix_world.translation
        wc.append(round(float((Vector(c)-piv).length),3)); wz.append(round(float(P[:,2].min()),3))
    r['rueda_pivote_off']=max(wc) if wc else None; r['rueda_minz']=(min(wz),max(wz)) if wz else None
    r['cofre']='Cofre' in ms; r['puertas']=sum(1 for k in ms if k.startswith('Puerta'))
    r['cristal']=sum(1 for k in ms if k.startswith('Cristal')); r['interior']=any(k.startswith('Interior') for k in ms); r['motor']='Motor' in ms
    if 'Cofre' in ms:
        h=ms['Cofre']; P=wverts(h); piv=h.matrix_world.translation
        r['cofre_bisagra']=round(float(abs(piv.y-P[:,1].max())),3)   # pivote en borde trasero
        r['frente_menosY']=bool(P[:,1].mean()<0)
    else:
        P=wverts(body); r['frente_menosY']=None
    bnd,nm=mesh_stats(body)
    r['bordes_abiertos']=bnd; r['no_manifold']=nm
    return r,new
def clear(new):
    for o in new:
        try: bpy.data.objects.remove(o)
        except Exception: pass

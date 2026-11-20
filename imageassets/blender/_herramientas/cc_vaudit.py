
"""cc_vaudit.py - 19-nov: escala real de carros viejos, rin de policia, revision y 4 vistas por vehiculo"""
import bpy, bmesh, os, math, numpy as np
from mathutils import Vector, Matrix
import cc_tools as C
CAR=os.path.join(C.ROOT,'carros')
SCALE={'V01A':4.5,'V01B':4.5,'V02':4.5,'V03':4.5,'V04':4.5,'V05':4.5,'V06':4.5,'V09':4.5,'V10':4.5,'V11':4.5,'V07':2.05,'V08':2.0,'C19':1.8}
def clean_scene():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    for m in list(bpy.data.meshes):
        if m.users==0: bpy.data.meshes.remove(m)
def imp(code):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(CAR,code+'.fbx'))
    new=[o for o in bpy.data.objects if o not in before]
    root=[o for o in new if o.parent is None and o.type=='EMPTY'][0]
    root.name=code
    return root,new
def depth(o):
    d=0
    while o.parent: o=o.parent; d+=1
    return d
def scale_all(root,S):
    bpy.context.view_layer.update()
    objs=sorted(root.children_recursive,key=depth)
    mws={o.name:o.matrix_world.copy() for o in objs}
    done=set()
    for o in objs:
        if o.type=='MESH' and o.data.name not in done:
            o.data.transform(Matrix.Scale(S,4)); done.add(o.data.name)
    for d in sorted({depth(o) for o in objs}):
        for o in [x for x in objs if depth(x)==d]:
            t,r,s=mws[o.name].decompose(); o.matrix_world=Matrix.LocRotScale(t*S,r,s)
        bpy.context.view_layer.update()
def export(root,code):
    vl=bpy.context.view_layer
    for o in vl.objects: o.select_set(False)
    objs=[root]+list(root.children_recursive)
    names=[]
    for o in objs:
        if '.' in o.name and o!=root: names.append(o.name); o.name=o.name.split('.')[0]
    for o in objs: o.hide_set(False); o.select_set(True)
    vl.objects.active=root
    bpy.ops.export_scene.fbx(filepath=os.path.join(CAR,code+'.fbx'),use_selection=True,object_types={'EMPTY','MESH'},global_scale=1.0,apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=False,add_leaf_bones=False,bake_anim=False,
        path_mode='COPY',embed_textures=True,use_mesh_modifiers=True)
def ch(root,name):
    for o in root.children_recursive:
        if o.name.split('.')[0]==name: return o
def wv(o): return np.array([tuple(o.matrix_world@v.co) for v in o.data.vertices])
# ---------- rin de policia ----------
def mat(name,col,met=0.0,rough=0.6):
    m=bpy.data.materials.get(name)
    if m: return m
    m=bpy.data.materials.new(name); m.use_nodes=True
    b=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if b is None:
        b=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled'); out=m.node_tree.nodes.new('ShaderNodeOutputMaterial'); m.node_tree.links.new(b.outputs[0],out.inputs[0])
    b.inputs['Base Color'].default_value=(*col,1); b.inputs['Metallic'].default_value=met; b.inputs['Roughness'].default_value=rough
    m.diffuse_color=(*col,1); return m
def police_wheel(R,Wd,side):
    """malla de llanta con rin de acero negro (eje = X local), radio R, ancho Wd. side=+1 la cara exterior hacia +X"""
    bm=bmesh.new(); seg=32
    def ring(r,x): return [bm.verts.new((x, r*math.cos(2*math.pi*i/seg), r*math.sin(2*math.pi*i/seg))) for i in range(seg)]
    def bridge(a,b,mi):
        for i in range(seg):
            j=(i+1)%seg; f=bm.faces.new((a[i],a[j],b[j],b[i])); f.material_index=mi
    h=Wd/2; s=side
    # llanta: perfil (r,x) de afuera hacia adentro
    prof=[(0.66*R,s*h*0.95),(0.80*R,s*h),(0.95*R,s*h*0.92),(R,s*h*0.6),(R,-s*h*0.6),(0.95*R,-s*h*0.92),(0.80*R,-s*h),(0.66*R,-s*h*0.95)]
    rings=[ring(r,x) for r,x in prof]
    for a,b in zip(rings[:-1],rings[1:]): bridge(a,b,0)
    # rin: cara exterior hundida + centro + taza
    rim=[(0.66*R,s*h*0.95),(0.62*R,s*h*0.70),(0.36*R,s*h*0.62),(0.22*R,s*h*0.80),(0.12*R,s*h*0.86)]
    rr=[ring(r,x) for r,x in rim]; rr.insert(0,rings[0])
    for k,(a,b) in enumerate(zip(rr[:-1],rr[1:])): bridge(a,b,1 if k<3 else 2)
    c=bm.verts.new((s*h*0.88,0,0))
    last=rr[-1]
    for i in range(seg):
        f=bm.faces.new((last[i],last[(i+1)%seg],c)) if s>0 else bm.faces.new((last[(i+1)%seg],last[i],c)); f.material_index=2
    # cara interior (plano oscuro)
    inner=rings[-1]; ci=bm.verts.new((-s*h*0.6,0,0))
    for i in range(seg):
        f=bm.faces.new((inner[(i+1)%seg],inner[i],ci)) if s>0 else bm.faces.new((inner[i],inner[(i+1)%seg],ci)); f.material_index=1
    # 5 birlos
    for k in range(5):
        a=2*math.pi*k/5; y=0.29*R*math.cos(a); z=0.29*R*math.sin(a)
        r0=bmesh.ops.create_cone(bm,cap_ends=True,segments=6,radius1=0.035*R,radius2=0.035*R,depth=0.05*R)
        for v in r0['verts']:
            v.co=Vector((v.co.z+s*h*0.68, v.co.x+y, v.co.y+z))
        for f in {f for v in r0['verts'] for f in v.link_faces}: f.material_index=2
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces[:])
    me=bpy.data.meshes.new('RinPolicia'); bm.to_mesh(me); bm.free()
    me.materials.append(mat('Llanta_Hule',(0.035,0.035,0.035),0.0,0.85))
    me.materials.append(mat('Rin_AceroNegro',(0.05,0.05,0.055),0.6,0.45))
    me.materials.append(mat('Rin_Cromo',(0.75,0.75,0.78),1.0,0.15))
    return me
def police_wheels(root):
    out=[]
    for o in root.children_recursive:
        n=o.name.split('.')[0]
        if not n.startswith('Rueda'): continue
        P=np.array([tuple(v.co) for v in o.data.vertices]); ext=P.max(0)-P.min(0)
        ax=int(np.argmin(ext)); Wd=float(ext[ax]); R=float(max(ext[(ax+1)%3],ext[(ax+2)%3]))/2
        side=1 if o.matrix_world.translation.x>0 else -1
        me=police_wheel(R,Wd,side)
        if ax!=0:   # llevar eje X del rin al eje local real
            me.transform(Matrix.Rotation(math.radians(90),4,'Z' if ax==1 else 'Y'))
        old=o.data; o.data=me; me.name=n+'_RinPolicia'
        out.append((n,round(R,3),round(Wd,3),ax))
    return out
# ---------- revision ----------
def flipped_pct(o):
    """% de aristas donde dos caras vecinas tienen el orden de vertices inconsistente (= una de las dos esta invertida)"""
    bm=bmesh.new(); bm.from_mesh(o.data); bad=0; tot=0
    for e in bm.edges:
        lf=e.link_loops
        if len(lf)!=2: continue
        tot+=1
        if lf[0].vert==lf[1].vert: bad+=1
    bm.free(); return round(100*bad/max(tot,1),2)
def check(root,code):
    ms={o.name.split('.')[0]:o for o in root.children_recursive if o.type=='MESH'}
    allP=np.concatenate([wv(o) for o in ms.values()]); mn,mx=allP.min(0),allP.max(0)
    body=ms.get('Carroceria') or ms.get('Cuerpo')
    r={'L':round(float(mx[1]-mn[1]),2),'W':round(float(mx[0]-mn[0]),2),'H':round(float(mx[2]-mn[2]),2),
       'piv':round(float(max(abs((mn[0]+mx[0])/2),abs((mn[1]+mx[1])/2),abs(mn[2]))),3)}
    wl=[o for k,o in ms.items() if k.startswith('Rueda')]
    r['ruedas']=len(wl)
    r['rueda_piv']=round(max(float((Vector(((wv(o).min(0)+wv(o).max(0))/2))-o.matrix_world.translation).length) for o in wl),3) if wl else None
    r['rueda_z']=round(max(abs(float(wv(o)[:,2].min())) for o in wl),3) if wl else None
    r['cofre']='Cofre' in ms; r['puertas']=sum(1 for k in ms if k in ('Puerta_I','Puerta_D'))
    r['cristal']=sum(1 for k in ms if k.startswith('Cristal')); r['cristal_puerta']=sum(1 for k in ms if k.startswith('Cristal_Puerta'))
    r['interior']=any(k.startswith('Interior') for k in ms); r['motor']='Motor' in ms
    if 'Cofre' in ms: r['frente']='-Y Blender (+Z Unity)' if wv(ms['Cofre'])[:,1].mean()<0 else 'AL REVES'
    else: r['frente']='-Y' if wv(body)[:,1].min()<-0.4*r['L'] else '?'
    r['invertidas_%']=flipped_pct(body)
    bm=bmesh.new(); bm.from_mesh(body.data); r['bordes_abiertos']=sum(1 for e in bm.edges if e.is_boundary); bm.free()
    return r
def views(root,code,out):
    ms={o.name.split('.')[0]:o for o in root.children_recursive}
    allP=np.concatenate([wv(o) for o in root.children_recursive if o.type=='MESH']); mn,mx=allP.min(0),allP.max(0)
    L=float(mx[1]-mn[1]); H=float(mx[2]-mn[2]); c=Vector(((mn[0]+mx[0])/2,(mn[1]+mx[1])/2,H*0.45))
    d=max(L,H*1.6)*1.55
    sc=bpy.context.scene; sc.display.shading.show_backface_culling=True
    paths=[]
    V=[(Vector((0.75,-0.75,0.35)),False),(Vector((-0.7,0.75,0.35)),False),(Vector((1,0,0.05)),False),(Vector((-0.65,-0.75,0.5)),True)]
    for k,(dv,opn) in enumerate(V):
        rot={}
        if opn:
            for n,ax,a in (('Cofre','X',-50),('Puerta_I','Z',-65),('Puerta_D','Z',65)):
                o=ms.get(n)
                if o: rot[n]=o.rotation_euler.copy(); o.rotation_euler.rotate_axis(ax,math.radians(a))
            bpy.context.view_layer.update()
        p=os.path.join(C.REV,'_tmp_va_%s_%d.png'%(code,k))
        C.snap(p,tuple(c+dv.normalized()*d),tuple(c),35,res=(360,250)); paths.append(p)
        for n,e in rot.items(): ms[n].rotation_euler=e
    return paths

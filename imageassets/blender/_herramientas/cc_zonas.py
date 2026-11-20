
import bpy, bmesh, math, random, os
from mathutils import Vector, Matrix
import cc_tools as C
ZON=os.path.join(C.ROOT,'zonas')

PAL={'Concreto':(0.62,0.61,0.58),'ConcretoClaro':(0.86,0.86,0.84),'Asfalto':(0.16,0.16,0.17),'PinturaBlanca':(0.92,0.92,0.9),
     'PinturaAmarilla':(0.9,0.72,0.12),'Madera':(0.42,0.29,0.18),'MaderaOscura':(0.27,0.19,0.12),'Roca':(0.38,0.37,0.35),
     'Metal':(0.55,0.57,0.6),'Hierro':(0.12,0.12,0.13),'Contenedor':(0.62,0.22,0.14),'Fondo':(0.01,0.01,0.012),
     'Malla':(0.6,0.62,0.64),'Luz':(0.75,0.95,1.0)}
EMIT={'Luz':6.0}

TEXDIR=os.path.join(ZON,'texturas')
# material -> (texture file, metros por repeticion)
TEX={'Concreto':('T_ConcretoManchado.png',2.0),'ConcretoClaro':('T_ConcretoManchado.png',2.0),
     'ConcretoCerro':('T05_ConcretoCerro.png',2.0),'Asfalto':('T03_Asfalto.png',4.0),'Arena':('T04_Arena.png',2.0),
     'Adoquin':('T06_Adoquin.png',2.0),
     'Madera':('T_MaderaVieja.png',1.0),'Pilote':('T_MaderaVieja_Oscura.png',1.0),'MaderaOscura':('T_MaderaVieja_Oscura.png',1.0),'Roca':('T_Roca.png',1.5),
     'Metal':('T_MetalOxidado.png',1.0),'Hierro':('T_HierroOxidado.png',1.0)}
PAL.update({'Pilote':(0.3,0.25,0.2),'ConcretoCerro':(0.7,0.68,0.64),'Arena':(0.8,0.72,0.55),'Adoquin':(0.6,0.58,0.55)})
TINT={}   # sin nodos Mix: el FBX solo reconoce Imagen -> Base Color directo

def mat(name):
    m=bpy.data.materials.get(name)
    if m and (name not in TEX or m.get('tex')==TEX[name][0]): return m
    if m: bpy.data.materials.remove(m)
    m=bpy.data.materials.new(name); m.use_nodes=True
    c=PAL.get(name,(0.5,0.5,0.5)); m.diffuse_color=(*c,1)
    nt=m.node_tree
    b=next((n for n in nt.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if b is None:
        b=nt.nodes.new('ShaderNodeBsdfPrincipled')
        out=next((n for n in nt.nodes if n.type=='OUTPUT_MATERIAL'),None) or nt.nodes.new('ShaderNodeOutputMaterial')
        nt.links.new(b.outputs[0],out.inputs[0])
    b.inputs['Base Color'].default_value=(*c,1)
    b.inputs['Roughness'].default_value=0.85
    if name in ('Metal','Hierro'): b.inputs['Metallic'].default_value=0.6; b.inputs['Roughness'].default_value=0.5
    if name in EMIT:
        b.inputs['Emission Color'].default_value=(*c,1); b.inputs['Emission Strength'].default_value=EMIT[name]
    if name in TEX:
        fn=TEX[name][0]
        img=bpy.data.images.load(os.path.join(TEXDIR,fn),check_existing=True)
        t=nt.nodes.new('ShaderNodeTexImage'); t.image=img; t.location=(-500,300)
        if name in TINT:
            mx=nt.nodes.new('ShaderNodeMix'); mx.data_type='RGBA'; mx.blend_type='MULTIPLY'; mx.inputs[0].default_value=1.0
            nt.links.new(t.outputs['Color'],mx.inputs[6]); mx.inputs[7].default_value=TINT[name]
            nt.links.new(mx.outputs[2],b.inputs['Base Color'])
        else:
            nt.links.new(t.outputs['Color'],b.inputs['Base Color'])
        m['tex']=fn; m.diffuse_color=(1,1,1,1)
        if name in ('Metal','Hierro'): b.inputs['Metallic'].default_value=0.35; b.inputs['Roughness'].default_value=0.7
    return m

class Builder:
    def __init__(self,name):
        self.name=name; self.bm=bmesh.new(); self.mats=[]
    def mi(self,m):
        if m not in self.mats: self.mats.append(m)
        return self.mats.index(m)
    def face(self,pts,m):
        vs=[self.bm.verts.new(Vector(p)) for p in pts]
        f=self.bm.faces.new(vs); f.material_index=self.mi(m); return f
    def box(self,x0,x1,y0,y1,z0,z1,m,skip=()):
        P=lambda x,y,z:(x,y,z)
        fs={'-z':[(x0,y0,z0),(x0,y1,z0),(x1,y1,z0),(x1,y0,z0)],'+z':[(x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)],
            '-y':[(x0,y0,z0),(x1,y0,z0),(x1,y0,z1),(x0,y0,z1)],'+y':[(x1,y1,z0),(x0,y1,z0),(x0,y1,z1),(x1,y1,z1)],
            '-x':[(x0,y1,z0),(x0,y0,z0),(x0,y0,z1),(x0,y1,z1)],'+x':[(x1,y0,z0),(x1,y1,z0),(x1,y1,z1),(x1,y0,z1)]}
        for k,v in fs.items():
            if k not in skip: self.face(v,m)
    def poly_prism(self,pts2,z0,z1,m,caps=True,mtop=None):
        n=len(pts2)
        bot=[self.bm.verts.new((x,y,z0)) for x,y in pts2]; top=[self.bm.verts.new((x,y,z1)) for x,y in pts2]
        for i in range(n):
            j=(i+1)%n; f=self.bm.faces.new((bot[i],bot[j],top[j],top[i])); f.material_index=self.mi(m)
        if caps:
            f=self.bm.faces.new(top); f.material_index=self.mi(mtop or m)
            f=self.bm.faces.new(bot[::-1]); f.material_index=self.mi(m)
    def cyl(self,cx,cy,r,z0,z1,m,sides=8,caps=True):
        pts=[(cx+r*math.cos(2*math.pi*i/sides),cy+r*math.sin(2*math.pi*i/sides)) for i in range(sides)]
        self.poly_prism(pts,z0,z1,m,caps)
    def lathe(self,cx,cy,prof,m,sides=8,cap_top=True,cap_bot=True):
        rings=[]
        for r,z in prof:
            rings.append([self.bm.verts.new((cx+r*math.cos(2*math.pi*i/sides),cy+r*math.sin(2*math.pi*i/sides),z)) for i in range(sides)])
        for a,b in zip(rings[:-1],rings[1:]):
            for i in range(sides):
                j=(i+1)%sides; f=self.bm.faces.new((a[i],a[j],b[j],b[i])); f.material_index=self.mi(m)
        if cap_top: f=self.bm.faces.new(rings[-1]); f.material_index=self.mi(m)
        if cap_bot: f=self.bm.faces.new(rings[0][::-1]); f.material_index=self.mi(m)
    def tube(self,pts,r,m,sides=4,caps=True):
        """tube along polyline pts (list of Vector)"""
        pts=[Vector(p) for p in pts]; rings=[]
        for k,p in enumerate(pts):
            t=(pts[min(k+1,len(pts)-1)]-pts[max(k-1,0)]).normalized()
            a=Vector((0,0,1)) if abs(t.z)<0.9 else Vector((1,0,0))
            u=t.cross(a).normalized(); v=t.cross(u)
            rings.append([self.bm.verts.new(p+r*(math.cos(2*math.pi*i/sides)*u+math.sin(2*math.pi*i/sides)*v)) for i in range(sides)])
        for a,b in zip(rings[:-1],rings[1:]):
            for i in range(sides):
                j=(i+1)%sides; f=self.bm.faces.new((a[i],a[j],b[j],b[i])); f.material_index=self.mi(m)
        if caps:
            f=self.bm.faces.new(rings[0][::-1]); f.material_index=self.mi(m)
            f=self.bm.faces.new(rings[-1]); f.material_index=self.mi(m)
    def sweep(self,prof,frames,mats,closed=True,caps=True):
        """prof: list of (u,z) ; frames: list of (origin Vector, side Vector u-dir) ; mats: material per profile edge"""
        rings=[]
        for o,s in frames:
            rings.append([self.bm.verts.new(o+s*u+Vector((0,0,z))) for u,z in prof])
        n=len(prof); E=n if closed else n-1
        for a,b in zip(rings[:-1],rings[1:]):
            for i in range(E):
                j=(i+1)%n; f=self.bm.faces.new((a[i],b[i],b[j],a[j])); f.material_index=self.mi(mats[i])
        if caps and closed:
            f=self.bm.faces.new(rings[0]); f.material_index=self.mi(mats[0])
            f=self.bm.faces.new(rings[-1][::-1]); f.material_index=self.mi(mats[0])
        return rings
    def finish(self,coll=None,loc=(0,0,0)):
        bm=self.bm
        bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=1e-5)
        ngons=[f for f in bm.faces if len(f.verts)>4]
        if ngons: bmesh.ops.triangulate(bm,faces=ngons,quad_method='BEAUTY',ngon_method='EAR_CLIP')
        bm.normal_update()
        uv=bm.loops.layers.uv.new('UVMap')
        for f in bm.faces:
            n=f.normal; ax=max(range(3),key=lambda i:abs(n[i]))
            mn=self.mats[f.material_index] if f.material_index<len(self.mats) else ''
            k=1.0/TEX[mn][1] if mn in TEX else 1.0
            rot=(mn=='Pilote' and ax!=2)
            for l in f.loops:
                co=l.vert.co
                if rot:
                    hz=co.y*(1 if n.x>0 else -1) if ax==0 else co.x*(-1 if n.y>0 else 1)
                    l[uv].uv=(k*co.z,k*hz); continue
                if ax==0: l[uv].uv=(k*co.y*(1 if n.x>0 else -1),k*co.z)
                elif ax==1: l[uv].uv=(k*co.x*(-1 if n.y>0 else 1),k*co.z)
                else: l[uv].uv=(k*co.x,k*co.y*(1 if n.z>0 else -1))
        me=bpy.data.meshes.new(self.name); bm.to_mesh(me); bm.free()
        for m in self.mats: me.materials.append(mat(m))
        for p in me.polygons: p.use_smooth=False
        o=bpy.data.objects.new(self.name,me)
        (coll or bpy.context.scene.collection).objects.link(o); o.location=loc
        return o

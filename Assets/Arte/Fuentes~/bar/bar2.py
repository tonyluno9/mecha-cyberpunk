import bpy, math, os
from mathutils import Vector
OUT = r'C:\Users\alane\AppData\Local\Temp\claude\C--Users-alane-Documents-Codex-mecha-cyberpunk\59b77b14-16b1-4121-bbec-edc41bd5899d\scratchpad\arte-bar'
MATS = {
 "Neon_Cian": ((0.0,0.9,1.0,1), True, (0.0,0.9,1.0,1)),
 "Neon_Magenta": ((1.0,0.0,0.8,1), True, (1.0,0.0,0.8,1)),
 "Neon_Naranja": ((1.0,0.35,0.0,1), True, (1.0,0.35,0.0,1)),
 "Metal_Oscuro": ((0.06,0.07,0.09,1), False, None),
 "Madera_Oscura": ((0.12,0.06,0.04,1), False, None),
}
def mat(name):
    m = bpy.data.materials.get(name)
    if m: return m
    col, emis, ec = MATS[name]
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type=="BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = col
    b.inputs["Roughness"].default_value = 0.35 if not emis else 0.5
    if not emis: b.inputs["Metallic"].default_value = 0.6 if "Metal" in name else 0
    if emis:
        b.inputs["Emission Color"].default_value = ec
        b.inputs["Emission Strength"].default_value = 4.0
    m.diffuse_color = col
    return m
def reset():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o,do_unlink=True)
    for m in list(bpy.data.meshes): bpy.data.meshes.remove(m)
parts=[]
def box(name,m,loc,size):
    bpy.ops.mesh.primitive_cube_add(location=loc); o=bpy.context.object
    o.scale=(size[0]/2,size[1]/2,size[2]/2); fin(o,name,m)
def cyl(name,m,loc,r,h,v=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=v,radius=r,depth=h,location=loc); o=bpy.context.object; fin(o,name,m)
def fin(o,name,m):
    o.name=name; bpy.ops.object.transform_apply(scale=True)
    o.data.materials.append(mat(m)); parts.append(o)
def finish(name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join(); o=bpy.context.object; o.name=name
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.quads_convert_to_tris(); bpy.ops.object.mode_set(mode='OBJECT')
    bb=[o.matrix_world@Vector(c) for c in o.bound_box]
    mnz=min(v.z for v in bb); cx=sum(v.x for v in bb)/8; cy=sum(v.y for v in bb)/8
    for v in o.data.vertices: v.co-=Vector((cx,cy,mnz))
    o.location=(0,0,0)
    path=os.path.join(OUT,name)
    bpy.ops.wm.save_as_mainfile(filepath=path+".blend")
    bpy.ops.export_scene.gltf(filepath=path+".glb",export_format='GLB',use_selection=True,export_yup=True)
    tris=len(o.data.polygons)
    parts.clear()
    return tris
def barra():
    reset()
    box("barra","Madera_Oscura",(0,0,0.55),(3.0,0.7,1.1))
    box("tope","Metal_Oscuro",(0,0,1.13),(3.1,0.85,0.06))
    box("neon_frente","Neon_Cian",(0,-0.36,0.12),(2.9,0.03,0.05))
    box("neon_borde","Neon_Magenta",(0,-0.43,1.12),(3.0,0.03,0.04))
    box("reposapies","Metal_Oscuro",(0,-0.5,0.2),(2.9,0.05,0.05))
    box("neon_naranja","Neon_Naranja",(0,-0.36,0.62),(2.5,0.03,0.04))
    box("fondo","Metal_Oscuro",(0,0.42,1.5),(3.0,0.05,0.8))
    box("estante","Madera_Oscura",(0,0.3,1.5),(2.8,0.22,0.05))
    box("estante2","Madera_Oscura",(0,0.3,1.85),(2.8,0.22,0.05))
    box("neon_estante","Neon_Naranja",(0,0.18,1.45),(2.6,0.02,0.03))
    for sx in(-1.3,0,1.3):
        box(f"soporte{sx}","Metal_Oscuro",(sx,0.3,1.5),(0.06,0.06,0.78))
        box(f"escuadra{sx}","Metal_Oscuro",(sx,0.2,1.43),(0.04,0.14,0.04))
    for i,x in enumerate((-1.1,-0.7,-0.3,0.3,0.7,1.1)):
        cyl(f"botella{i}",["Neon_Cian","Neon_Naranja","Neon_Magenta"][i%3],(x,0.3,1.65),0.045,0.25,6)
    for i,x in enumerate((-0.9,0,0.9)):
        cyl(f"pie{i}","Metal_Oscuro",(x,-0.95,0.34),0.04,0.68,8)
        cyl(f"base{i}","Metal_Oscuro",(x,-0.95,0.02),0.2,0.04,10)
        cyl(f"asiento{i}","Madera_Oscura",(x,-0.95,0.72),0.2,0.08,10)
        cyl(f"aro{i}","Neon_Magenta",(x,-0.95,0.66),0.21,0.025,10)
    return finish("prop_barra")
def mesa():
    reset()
    cyl("tapa","Madera_Oscura",(0,0,0.75),0.5,0.05,16)
    cyl("aro","Neon_Cian",(0,0,0.73),0.51,0.02,16)
    cyl("pata","Metal_Oscuro",(0,0,0.37),0.05,0.74,8)
    cyl("base","Metal_Oscuro",(0,0,0.02),0.3,0.04,12)
    cyl("lampara","Neon_Naranja",(0,0,0.8),0.06,0.06,8)
    for i,a in enumerate((90,270)):
        dx,dy=math.cos(math.radians(a)),math.sin(math.radians(a))
        px,py=dx*0.8,dy*0.8
        for sx in(-1,1):
            for sy in(-1,1):
                box(f"p{i}{sx}{sy}","Metal_Oscuro",(px+sx*0.18,py+sy*0.18,0.22),(0.04,0.04,0.44))
        box(f"asiento{i}","Madera_Oscura",(px,py,0.46),(0.44,0.44,0.05))
        box(f"resp{i}","Madera_Oscura",(px+dx*0.2,py+dy*0.2,0.75),(0.44,0.05,0.5))
        box(f"neonresp{i}","Neon_Magenta",(px+dx*0.2,py+dy*0.2,1.02),(0.44,0.06,0.03))
    return finish("prop_mesa")
def dj():
    reset()
    box("cuerpo","Metal_Oscuro",(0,0,0.55),(2.0,0.8,1.1))
    box("tope","Madera_Oscura",(0,0,1.12),(2.1,0.9,0.05))
    box("neon_frente","Neon_Magenta",(0,-0.41,0.55),(1.7,0.02,0.06))
    box("neon_base","Neon_Cian",(0,-0.41,0.1),(1.9,0.02,0.04))
    box("mezcladora","Metal_Oscuro",(0,0,1.2),(0.5,0.4,0.1))
    box("neon_mezcla","Neon_Naranja",(0,-0.2,1.2),(0.4,0.02,0.03))
    for sx in(-1,1):
        cyl(f"plato{sx}","Metal_Oscuro",(sx*0.6,0,1.18),0.27,0.05,16)
        cyl(f"disco{sx}","Neon_Cian",(sx*0.6,0,1.21),0.22,0.01,16)
        box(f"bocina{sx}","Metal_Oscuro",(sx*1.25,0.1,0.6),(0.5,0.5,1.2))
        box(f"cono{sx}","Neon_Magenta",(sx*1.25,-0.16,0.5),(0.3,0.03,0.3))
        box(f"tweeter{sx}","Neon_Naranja",(sx*1.25,-0.16,0.95),(0.14,0.03,0.14))
        box(f"poste{sx}","Metal_Oscuro",(sx*0.35,0.3,1.4),(0.05,0.05,0.5))
    box("pantalla_marco","Metal_Oscuro",(0,0.3,1.8),(0.9,0.04,0.5))
    box("pantalla","Neon_Cian",(0,0.27,1.8),(0.8,0.02,0.4))
    return finish("prop_dj")
def letrero():
    reset()
    box("panel","Metal_Oscuro",(0,0,1.0),(2.0,0.08,0.8))
    box("marco_a","Neon_Cian",(0,-0.05,1.37),(1.9,0.03,0.04))
    box("marco_b","Neon_Cian",(0,-0.05,0.63),(1.9,0.03,0.04))
    box("marco_c","Neon_Magenta",(-0.93,-0.05,1.0),(0.04,0.03,0.74))
    box("marco_d","Neon_Magenta",(0.93,-0.05,1.0),(0.04,0.03,0.74))
    for i,x in enumerate((-0.55,0,0.55)):
        box(f"barra{i}",["Neon_Cian","Neon_Naranja","Neon_Magenta"][i],(x,-0.05,1.0),(0.3,0.03,0.3 if i==1 else 0.12))
    for sx in(-0.7,0.7): box(f"poste{sx}","Metal_Oscuro",(sx,0.06,0.5),(0.06,0.06,1.0))
    return finish("prop_letrero")
def run(which):
    return {"barra":barra,"mesa":mesa,"dj":dj,"letrero":letrero}[which]()

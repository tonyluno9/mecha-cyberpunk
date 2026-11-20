
import math, random
from mathutils import Vector
from cc_zonas import Builder

# ---------- MALECON ----------
def ML01_BarandalMalecon():
    b=Builder('ML01_BarandalMalecon'); M='ConcretoClaro'
    L=2.0
    b.box(-L,L,-0.2,0.2,0,0.16,M)                 # zocalo
    b.box(-L,L,-0.18,0.18,0.92,1.08,M)            # pasamanos
    b.box(-L+0.0,-L+0.18,-0.2,0.2,0.16,0.92,M,skip=('-z','+z'))   # medio pilar en cada extremo
    b.box(L-0.18,L,-0.2,0.2,0.16,0.92,M,skip=('-z','+z'))
    prof=[(0.075,0.16),(0.095,0.22),(0.06,0.3),(0.06,0.36),(0.105,0.56),(0.06,0.78),(0.06,0.83),(0.09,0.88),(0.075,0.92)]
    n=9
    for i in range(n):
        x=-L+0.18+(2*L-0.36)*(i+0.5)/n
        b.lathe(x,0,prof,M,sides=8,cap_top=False,cap_bot=False)
    return b

def ML02_EscaleraPlaya():
    b=Builder('ML02_EscaleraPlaya'); M='Concreto'
    H=2.0; n=10; run=0.36
    W=1.5   # medio ancho de la escalera (3 m)
    y_top=2.0
    for i in range(n):
        z1=H-i*H/n; z0=0.0
        ya=y_top-(i+1)*run; yb=y_top-i*run
        b.box(-W,W,ya,yb,0.0,z1,M,skip=('-z',))
    # descanso de llegada (playa)
    b.box(-W,W,-2.0,y_top-n*run,0,0.05,M,skip=('-z',))
    # muros laterales con remate
    for s in (-1,1):
        x0,x1=(W,2.0) if s>0 else (-2.0,-W)
        prof=[(-2.0,0),(2.0,0),(2.0,H+0.9),(y_top-0.4,H+0.9),(-2.0+0.6,0.9),(-2.0,0.9)]
        pts=[]
        # pared como prisma: perfil en (y,z), extruir en x
        vs0=[b.bm.verts.new((x0,y,z)) for y,z in prof]; vs1=[b.bm.verts.new((x1,y,z)) for y,z in prof]
        k=len(prof)
        for i in range(k):
            j=(i+1)%k; f=b.bm.faces.new((vs0[i],vs0[j],vs1[j],vs1[i])); f.material_index=b.mi(M)
        f=b.bm.faces.new(vs1); f.material_index=b.mi(M)
        f=b.bm.faces.new(vs0[::-1]); f.material_index=b.mi(M)
    return b

def _pier_deck(b,x0,x1,y0,y1,along='y'):
    """planks crossing the run direction"""
    zt=1.2; th=0.05; pw=0.2; gap=0.025
    if along=='y':
        y=y0
        while y<y1-0.01:
            b.box(x0,x1,y,min(y+pw,y1),zt-th,zt,'Madera'); y+=pw+gap
    else:
        x=x0
        while x<x1-0.01:
            b.box(x,min(x+pw,x1),y0,y1,zt-th,zt,'Madera'); x+=pw+gap

def _pile(b,x,y):
    b.cyl(x,y,0.15,-2.0,1.45,'MaderaOscura',sides=8)

def ML03_MuelleRecto():
    b=Builder('ML03_MuelleRecto')
    _pier_deck(b,-1.5,1.5,-2.0,2.0,'y')
    for x in (-1.35,0.0,1.35): b.box(x-0.08,x+0.08,-2.0,2.0,0.9,1.15,'MaderaOscura',skip=('-y','+y'))   # largueros
    for y in (-1.0,1.0):
        b.box(-1.5,1.5,y-0.1,y+0.1,0.75,0.9,'MaderaOscura')       # cabezal
        _pile(b,-1.5,y); _pile(b,1.5,y)
    for s in (-1,1): b.box(s*1.5-0.06*(s>0)-0.0,s*1.5+0.06*(s<0),-2.0,2.0,1.15,1.3,'MaderaOscura',skip=('-y','+y'))  # guardacanto
    return b

def ML04_MuelleEsquina():
    b=Builder('ML04_MuelleEsquina')
    _pier_deck(b,-1.5,1.5,-2.0,1.5,'y')
    _pier_deck(b,1.5,2.0,-1.5,1.5,'x')
    for x in (-1.35,0.0): b.box(x-0.08,x+0.08,-2.0,1.5,0.9,1.15,'MaderaOscura')
    b.box(-1.5,2.0,-0.08,0.08,0.9,1.15,'MaderaOscura')
    b.box(-1.5,2.0,1.27,1.43,0.9,1.15,'MaderaOscura')
    b.box(1.27,1.43,-2.0,-1.5,0.9,1.15,'MaderaOscura')
    for p in ((-1.5,-1.0),(1.5,-1.0),(-1.5,1.5),(1.0,1.5),(1.0,-1.5),(1.5,1.5)):
        _pile(b,*p)
    # guardacantos exteriores (lado -X y lado +Y)
    b.box(-1.56,-1.5,-2.0,1.5,1.15,1.3,'MaderaOscura')
    b.box(-1.56,2.0,1.5,1.56,1.15,1.3,'MaderaOscura')
    b.box(1.5,1.56,-2.0,-1.5,1.15,1.3,'MaderaOscura')
    return b

def _rock(b,c,s,seed):
    import bmesh
    rnd=random.Random(seed)
    ret=bmesh.ops.create_icosphere(b.bm,subdivisions=1,radius=1.0)
    vs=ret['verts']
    for v in vs:
        k=1+rnd.uniform(-0.22,0.22)
        v.co=Vector((v.co.x*s[0]*k,v.co.y*s[1]*k,v.co.z*s[2]*k))+Vector(c)
    fs=set(f for v in vs for f in v.link_faces)
    for f in fs: f.material_index=b.mi('Roca')

def ML05_Escollera():
    b=Builder('ML05_Escollera')
    rnd=random.Random(11); k=0
    # 3 hileras: lado mar (-Y) bajas, lado tierra (+Y) altas; rocas encimadas
    for y,zc,sz in ((-1.45,0.15,0.75),(-0.45,0.55,0.95),(0.6,1.05,1.05),(1.5,1.55,0.85)):
        x=-2.0+rnd.uniform(0,0.3)
        while x<2.0:
            s=(rnd.uniform(0.55,0.8)*sz,rnd.uniform(0.5,0.75)*sz,rnd.uniform(0.4,0.6)*sz)
            _rock(b,(x+s[0]*0.5,y+rnd.uniform(-0.2,0.2),zc+rnd.uniform(-0.1,0.1)),s,k); k+=1
            x+=s[0]*1.15
    return b

# ---------- CERRO ----------
def CE01_EscaleraCerro():
    b=Builder('CE01_EscaleraCerro'); M='Concreto'
    n=12; run=4.0/n; rise=2.0/n; W=1.0; th=0.3
    for i in range(n):
        y0=-2.0+i*run; y1=y0+run; zt=(i+1)*rise
        b.box(-W,W,y0,y1,zt-rise-0.0,zt,M,skip=('-z','-x','+x'))
    # losa inclinada abajo
    b.face([(-W,-2.0,-th),(-W,2.0,2.0-th),(W,2.0,2.0-th),(W,-2.0,-th)],M)
    b.face([(-W,-2.0,-th),(W,-2.0,-th),(W,-2.0,0.0),(-W,-2.0,0.0)],M)
    b.face([(W,2.0,2.0-th),(-W,2.0,2.0-th),(-W,2.0,2.0),(W,2.0,2.0)],M)
    # costados con bordillo (prisma de perfil en y,z)
    for x0,x1 in ((-W-0.15,-W),(W,W+0.15)):
        prof=[(-2.0,-th),(2.0,2.0-th),(2.0,2.0+0.15+rise),(-2.0,0.15+rise)]
        vs0=[b.bm.verts.new((x0,y,z)) for y,z in prof]; vs1=[b.bm.verts.new((x1,y,z)) for y,z in prof]
        for i in range(4):
            j=(i+1)%4; f=b.bm.faces.new((vs0[i],vs0[j],vs1[j],vs1[i])); f.material_index=b.mi(M)
        f=b.bm.faces.new(vs1); f.material_index=b.mi(M); f=b.bm.faces.new(vs0[::-1]); f.material_index=b.mi(M)
    # lados de los escalones (cerrar hacia abajo hasta la losa)
    for s in (-1,1):
        x=s*W
        for i in range(n):
            y0=-2.0+i*run; y1=y0+run; zt=(i+1)*rise
            pts=[(x,y0,y0/2+1.0-th),(x,y1,y1/2+1.0-th),(x,y1,zt),(x,y0,zt)]
            b.face(pts if s>0 else pts[::-1],M)
    return b

def CE02_BarandalTubo(slope=0.0,name='CE02_BarandalTubo'):
    b=Builder(name); M='Metal'
    z=lambda x: slope*(x+2.0)
    for x in (-2.0,0.0,2.0):
        b.cyl(x,0,0.03,z(x),z(x)+1.05,M,sides=6)
    for h in (0.5,1.0):
        b.tube([(-2.0,0,z(-2.0)+h),(2.0,0,z(2.0)+h)],0.025,M,sides=6)
    return b

def CE03_BarandalTuboEscalera():
    return CE02_BarandalTubo(slope=0.5,name='CE03_BarandalTuboEscalera')

# ---------- AUTOPISTA ----------
DECK=[(-4.0,0.0),(4.0,0.0),(6.0,0.9),(6.0,2.1),(5.8,2.1),(5.72,1.45),(5.55,1.25),(5.5,1.2),
      (-5.5,1.2),(-5.55,1.25),(-5.72,1.45),(-5.8,2.1),(-6.0,2.1),(-6.0,0.9)]
DECK_M=['Concreto']*7+['Asfalto']+['Concreto']*6

def _frames_line(L,n,z0=0.0,z1=0.0):
    return [(Vector((0,-L/2+L*i/n,z0+(z1-z0)*i/n)),Vector((1,0,0))) for i in range(n+1)]

def _lines(b,frames,dash=False):
    # lineas de borde y central sobre el asfalto
    import math
    for u0,u1,m in ((-5.2,-5.05,'PinturaBlanca'),(5.05,5.2,'PinturaBlanca')):
        b.sweep([(u1,1.205),(u0,1.205)],frames,[m],closed=False,caps=False)
    # central discontinua: 3 m pintado / 3 m sin pintar
    pts=[f[0] for f in frames]; acc=[0.0]
    for a,c in zip(pts[:-1],pts[1:]): acc.append(acc[-1]+(c-a).length)
    for i in range(len(frames)-1):
        mid=(acc[i]+acc[i+1])/2
        if dash and (mid%6.0)>3.0: continue
        b.sweep([(0.075,1.205),(-0.075,1.205)],frames[i:i+2],['PinturaBlanca'],closed=False,caps=False)

def AU01_AutopistaRecta():
    b=Builder('AU01_AutopistaRecta')
    fr=_frames_line(8.0,1)
    b.sweep(DECK,fr,DECK_M)
    _lines(b,_frames_line(8.0,8),dash=True)
    return b

def AU02_AutopistaCurva45():
    b=Builder('AU02_AutopistaCurva45'); R=24.0; n=16
    fr=[]
    for i in range(n+1):
        a=math.radians(45)*i/n
        o=Vector((R-R*math.cos(a),R*math.sin(a),0)); t=Vector((math.sin(a),math.cos(a),0)); s=Vector((t.y,-t.x,0))
        fr.append((o,s))
    b.sweep(DECK,fr,DECK_M)
    _lines(b,fr,dash=True)
    return b

def AU03_AutopistaRampa():
    b=Builder('AU03_AutopistaRampa'); L=48.0; n=24; H=7.2
    SLAB=[(-5.5,-0.3),(5.5,-0.3),(6.0,-0.3),(6.0,0.9),(5.8,0.9),(5.72,0.25),(5.55,0.05),(5.5,0.0),
          (-5.5,0.0),(-5.55,0.05),(-5.72,0.25),(-5.8,0.9),(-6.0,0.9),(-6.0,-0.3)]
    SM=['Concreto']*7+['Asfalto']+['Concreto']*6
    fr=[(Vector((0,L*i/n,H*i/n)),Vector((1,0,0))) for i in range(n+1)]
    b.sweep(SLAB,fr,SM)
    fr2=[(Vector((0,L*i/n,H*i/n-1.2)),Vector((1,0,0))) for i in range(n+1)]
    _lines(b,fr2,dash=True)
    # muros de contencion hasta el piso
    for i in range(n):
        y0=L*i/n; y1=L*(i+1)/n; z0=H*i/n-0.3; z1=H*(i+1)/n-0.3
        if z1<=0: continue
        R=[(6.0,y0,0),(6.0,y1,0),(6.0,y1,z1),(6.0,y0,z0)] if z0>0 else [(6.0,y0,0),(6.0,y1,0),(6.0,y1,z1)]
        b.face(R,'Concreto')
        b.face([(-p[0],p[1],p[2]) for p in R][::-1],'Concreto')
    # cara final (estribo) bajo el extremo alto
    b.face([(-6.0,L,0),(-6.0,L,H-0.3),(6.0,L,H-0.3),(6.0,L,0)],'Concreto')
    return b

def AU04_ColumnaAutopista():
    b=Builder('AU04_ColumnaAutopista'); M='Concreto'
    b.cyl(0,0,0.9,0.0,5.0,M,sides=12,caps=False)
    b.face([(0.9*math.cos(2*math.pi*i/12),0.9*math.sin(2*math.pi*i/12),0.0) for i in range(12)][::-1],M)
    # cabezal trapezoidal (10 m de ancho)
    prof=[(-1.0,5.0),(1.0,5.0),(4.9,5.6),(4.9,6.0),(-4.9,6.0),(-4.9,5.6)]
    vs0=[b.bm.verts.new((u,-0.9,z)) for u,z in prof]; vs1=[b.bm.verts.new((u,0.9,z)) for u,z in prof]
    for i in range(6):
        j=(i+1)%6; f=b.bm.faces.new((vs0[i],vs0[j],vs1[j],vs1[i])); f.material_index=b.mi(M)
    f=b.bm.faces.new(vs1); f.material_index=b.mi(M); f=b.bm.faces.new(vs0[::-1]); f.material_index=b.mi(M)
    b.box(-1.2,1.2,-1.2,1.2,0.0,0.3,M)   # zapata visible
    return b

def AU05_Guardarrail():
    b=Builder('AU05_Guardarrail')
    W=[(0.0,0.45),(0.06,0.5),(0.0,0.58),(0.06,0.66),(0.0,0.71),(0.02,0.71),(0.08,0.66),(0.02,0.58),(0.08,0.5),(0.02,0.45)]
    fr=[(Vector((x,0,0)),Vector((0,-1,0))) for x in (-2.0,2.0)]
    # perfil W extruido en X (u = -y)
    b.sweep(W,[(Vector((-2.0,0,0)),Vector((0,-1,0))),(Vector((2.0,0,0)),Vector((0,-1,0)))],['Metal']*len(W))
    import bmesh
    bmesh.ops.recalc_face_normals(b.bm,faces=b.bm.faces[:])
    for x in (-1.0,1.0):
        b.box(x-0.05,x+0.05,0.04,0.2,0.0,0.75,'Metal')
    return b

def AU06_EntradaTunel():
    b=Builder('AU06_EntradaTunel')
    # contorno del hueco: lados rectos hasta 4 m + semicirculo r 5
    hole=[(5.0,0.0),(5.0,2.0),(5.0,4.0)]
    for i in range(1,12):
        a=math.pi*i/12; hole.append((5.0*math.cos(a),4.0+5.0*math.sin(a)))
    hole+= [(-5.0,4.0),(-5.0,2.0),(-5.0,0.0)]
    cx,cz=0.0,4.0; X=8.0; Z=11.0
    def outer(p):
        dx,dz=p[0]-cx,p[1]-cz
        if abs(dz)<1e-6 and p[1]<=cz: return (X if dx>0 else -X,p[1])
        ts=[]
        if dx!=0: ts.append((X if dx>0 else -X)/dx if True else 0)
        if dz>0: ts.append((Z-cz)/dz)
        if dz<0: ts.append((0-cz)/dz)
        t=min(t for t in ts if t>0)
        return (cx+dx*t,cz+dz*t)
    out=[outer(p) for p in hole]
    out[0]=(X,0.0); out[-1]=(-X,0.0)
    T=1.0
    def edge(p):
        return 'R' if abs(p[0]-X)<1e-6 else ('L' if abs(p[0]+X)<1e-6 else 'T')
    for yv,flip in ((0.0,False),(T,True)):
        for i in range(len(hole)-1):
            q=[(hole[i][0],yv,hole[i][1]),(out[i][0],yv,out[i][1]),(out[i+1][0],yv,out[i+1][1]),(hole[i+1][0],yv,hole[i+1][1])]
            b.face(q[::-1] if flip else q,'Concreto')
            e0,e1=edge(out[i]),edge(out[i+1])
            if e0!=e1:
                cxn=X if 'R' in (e0,e1) else -X
                t=[(out[i][0],yv,out[i][1]),(cxn,yv,Z),(out[i+1][0],yv,out[i+1][1])]
                b.face(t[::-1] if flip else t,'Concreto')
    # esquinas superiores (relleno entre proyecciones)
    for s in (1,-1):
        q=[(s*X,0.0,Z),(s*X,T,Z),(s*X,T,0),(s*X,0.0,0)]
        b.face(q if s>0 else q[::-1],'Concreto')
    b.face([(-X,0,Z),(X,0,Z),(X,T,Z),(-X,T,Z)],'Concreto')
    # tubo interior (normales hacia adentro) 14 m
    D=14.0
    prof=[(p[0],p[1]) for p in hole]
    rings=[[b.bm.verts.new((u,y,z)) for u,z in prof] for y in (0.0,D)]
    for i in range(len(prof)-1):
        f=b.bm.faces.new((rings[0][i+1],rings[1][i+1],rings[1][i],rings[0][i])); f.material_index=b.mi('Concreto')
    b.face([(-5.0,D,0),(5.0,D,0),(5.0,D,9.0),(-5.0,D,9.0)],'Fondo')
    b.face([(-5.0,0,0.01),(5.0,0,0.01),(5.0,D,0.01),(-5.0,D,0.01)],'Asfalto')
    b.box(-0.15,0.15,0.5,D-0.5,8.85,8.95,'Luz',skip=('+z',))
    # remate superior
    b.box(-X-0.2,X+0.2,-0.2,T+0.2,Z,Z+0.5,'Concreto')
    return b

# ---------- PUERTO ----------
def PT01_Contenedor40():
    b=Builder('PT01_Contenedor40'); M='Contenedor'
    L=12.192; W=2.438; H=2.591; hl=L/2; hw=W/2
    b.box(-hl,hl,-hw+0.03,hw-0.03,0.0,H,M)
    # costillas de las paredes largas
    pitch=0.28; x=-hl+0.25
    while x<hl-0.25:
        for s in (-1,1):
            y0,y1=(hw-0.03,hw+0.02) if s>0 else (-hw-0.02,-hw+0.03)
            b.box(x,x+0.14,y0,y1,0.16,H-0.12,M,skip=('-z','+z'))
        x+=pitch
    # rieles y postes de esquina
    for s in (-1,1):
        b.box(-hl,hl,s*hw-0.05*(s>0)-0.0 if s>0 else -hw,hw if s>0 else -hw+0.05,0.0,0.16,M)
        b.box(-hl,hl,hw-0.05 if s>0 else -hw,hw if s>0 else -hw+0.05,H-0.12,H,M)
    for sx in (-1,1):
        for sy in (-1,1):
            x0=hl-0.17 if sx>0 else -hl; y0=hw-0.17 if sy>0 else -hw
            b.box(x0,x0+0.17,y0,y0+0.17,0.0,H,M)
    # puertas (extremo +X): barras de cierre
    for yb in (-0.85,-0.35,0.35,0.85):
        b.cyl(hl+0.04,yb,0.025,0.15,H-0.15,M,sides=6)
    b.box(hl,hl+0.02,-hw+0.17,hw-0.17,0.2,H-0.15,M,skip=('-x',))
    # techo con ondas leves
    x=-hl+0.4
    while x<hl-0.4:
        b.box(x,x+0.5,-hw+0.17,hw-0.17,H,H+0.02,M,skip=('-z',)); x+=1.0
    return b

def PT02_BitaAmarre():
    b=Builder('PT02_BitaAmarre')
    b.box(-0.3,0.3,-0.3,0.3,0.0,0.06,'Hierro')
    b.lathe(0,0,[(0.16,0.06),(0.15,0.12),(0.13,0.4),(0.15,0.45),(0.24,0.5),(0.24,0.56),(0.2,0.6),(0.0,0.62)],'Hierro',sides=12,cap_top=False,cap_bot=False)
    return b

# ---------- DESHUESADERO ----------
def DS01_RejaPuas():
    b=Builder('DS01_RejaPuas')
    H=2.4
    for x in (-2.0,0.0,2.0):
        b.cyl(x,0,0.035,0.0,H,'Metal',sides=6)
        b.tube([(x,0,H-0.02),(x,-0.35,H+0.32)],0.02,'Metal',sides=4)   # brazo inclinado hacia afuera (-Y)
    b.tube([(-2.0,0,H-0.05),(2.0,0,H-0.05)],0.022,'Metal',sides=6)
    b.tube([(-2.0,0,0.08),(2.0,0,0.08)],0.015,'Metal',sides=4)
    # malla: dos caras (frente y vuelta)
    b.face([(-2.0,0.0,0.05),(2.0,0.0,0.05),(2.0,0.0,H-0.05),(-2.0,0.0,H-0.05)],'Malla')
    b.face([(-2.0,0.002,0.05),(-2.0,0.002,H-0.05),(2.0,0.002,H-0.05),(2.0,0.002,0.05)],'Malla')
    # alambre de puas: 3 hilos sobre los brazos + concertina
    for k in (1,2,3):
        t=k/3.0
        b.tube([(-2.0,-0.35*t,H+0.32*t),(2.0,-0.35*t,H+0.32*t)],0.006,'Hierro',sides=3)
    pts=[]
    turns=16; N=turns*8
    for i in range(N+1):
        a=2*math.pi*turns*i/N; x=-2.0+4.0*i/N
        pts.append((x,-0.18+0.2*math.cos(a),H+0.22+0.2*math.sin(a)))
    b.tube(pts,0.007,'Hierro',sides=3)
    return b

# ---------- LAS LOMAS ----------
def LM01_MuroLuz():
    b=Builder('LM01_MuroLuz')
    H=3.0
    b.box(-2.0,2.0,-0.15,0.15,0.0,H,'ConcretoClaro')
    b.box(-2.0,2.0,-0.22,0.22,H,H+0.12,'ConcretoClaro')
    b.box(-2.0,2.0,-0.2,-0.15,0.0,0.12,'ConcretoClaro')   # zoclo
    b.box(-2.0,2.0,-0.165,-0.15,0.2,0.24,'Luz',skip=('-x','+x','+y'))      # tira de luz al pie (lado calle -Y)
    b.box(-2.0,2.0,-0.235,-0.22,H-0.04,H,'Luz',skip=('-x','+x','+y'))     # tira bajo el remate
    return b

ALL=[ML01_BarandalMalecon,ML02_EscaleraPlaya,ML03_MuelleRecto,ML04_MuelleEsquina,ML05_Escollera,
     CE01_EscaleraCerro,CE02_BarandalTubo,CE03_BarandalTuboEscalera,
     AU01_AutopistaRecta,AU02_AutopistaCurva45,AU03_AutopistaRampa,AU04_ColumnaAutopista,AU05_Guardarrail,AU06_EntradaTunel,
     PT01_Contenedor40,PT02_BitaAmarre,DS01_RejaPuas,LM01_MuroLuz]

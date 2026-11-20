import os

import bpy, bmesh, math
import numpy as np
from mathutils import Vector, Matrix
import cc_cars as K

_IMG={}
def img_array(img):
    k=img.name
    if k not in _IMG:
        w,h=img.size; a=np.empty(w*h*4,np.float32); img.pixels.foreach_get(a)
        _IMG[k]=a.reshape(h,w,4)
    return _IMG[k]

def tex_images(o):
    m=o.data.materials[0]; res={}
    for n in m.node_tree.nodes:
        if n.type=='TEX_IMAGE' and n.image:
            for s in n.outputs:
                for l in s.links: res[l.to_socket.name]=n.image
    return res

def face_samples(o):
    """per face: uv-center sampled basecolor rgb, roughness, metallic; plus center, normal"""
    me=o.data; imgs=tex_images(o)
    uvl=me.uv_layers.active.data
    nF=len(me.polygons)
    uv=np.zeros((nF,2)); cen=np.zeros((nF,3)); nor=np.zeros((nF,3)); area=np.zeros(nF)
    for p in me.polygons:
        u=np.mean([tuple(uvl[i].uv) for i in p.loop_indices],axis=0); uv[p.index]=u
        cen[p.index]=tuple(p.center); nor[p.index]=tuple(p.normal); area[p.index]=p.area
    def samp(img):
        a=img_array(img); h,w=a.shape[:2]
        x=np.clip((uv[:,0]%1.0)*w,0,w-1).astype(int); y=np.clip((uv[:,1]%1.0)*h,0,h-1).astype(int)
        return a[y,x,:3]
    col=samp(imgs['Base Color']) if 'Base Color' in imgs else np.zeros((nF,3))
    rou=samp(imgs['Roughness'])[:,0] if 'Roughness' in imgs else np.ones(nF)
    met=samp(imgs['Metallic'])[:,0] if 'Metallic' in imgs else np.zeros(nF)
    return dict(uv=uv,cen=cen,nor=nor,area=area,col=col,rou=rou,met=met)

def hsv(col):
    mx=col.max(1); mn=col.min(1); v=mx; s=np.where(mx>1e-6,(mx-mn)/np.maximum(mx,1e-6),0)
    r,g,b=col[:,0],col[:,1],col[:,2]; d=np.maximum(mx-mn,1e-6)
    h=np.where(mx==r,((g-b)/d)%6,np.where(mx==g,(b-r)/d+2,(r-g)/d+4))*60
    return h,s,v


def dbg_show(o, idx, path, views=((1.0,-1.3,0.7),(-1.1,0.9,0.6)), size=1.0):
    import cc_tools as C, os
    d=o.copy(); d.data=o.data.copy(); bpy.context.scene.collection.objects.link(d)
    m=bpy.data.materials.get('DBG') or bpy.data.materials.new('DBG'); m.diffuse_color=(1,0,0,1)
    d.data.materials.append(m); mi=len(d.data.materials)-1
    for i in idx: d.data.polygons[i].material_index=mi
    vo=[x for x in bpy.context.view_layer.objects if x is not None]
    hid={x.name:x.hide_get() for x in vo}
    for x in vo: x.hide_set(x is not d)
    P=np.array([tuple(d.matrix_world@v.co) for v in d.data.vertices]); c=(P.min(0)+P.max(0))/2
    outs=[]
    for k,vw in enumerate(views):
        p=path.replace('.png','_%d.png'%k)
        C.snap(p,tuple(Vector(c)+Vector(vw)*size),tuple(c),40,None,(500,400)); outs.append(p)
    for x in vo:
        if x.name in hid and x is not d: x.hide_set(hid[x.name])
    me=d.data; bpy.data.objects.remove(d); bpy.data.meshes.remove(me)
    return outs


def fit_circle(x,y):
    A=np.c_[2*x,2*y,np.ones(len(x))]; b=x**2+y**2
    c=np.linalg.lstsq(A,b,rcond=None)[0]; return c[0],c[1],float(np.sqrt(max(c[2]+c[0]**2+c[1]**2,1e-9)))

def find_wheels(o, gap=0.25, win=None, side_frac=0.5, moto=False):
    P=np.array([tuple(v.co) for v in o.data.vertices])
    xm=P[:,0].max(); H=P[:,2].max()
    sides=[0] if moto else [1,-1]
    res=[]
    for sg in sides:
        Q=P if moto else P[sg*P[:,0]>side_frac*xm]
        if moto: Q=P[np.abs(P[:,0])<0.12*max(xm,0.3)+0.05]
        g=Q[Q[:,2]<0.02+0.004*H]
        ys=np.sort(g[:,1]); clusters=[]
        if len(ys)==0: continue
        cur=[ys[0]]
        for y in ys[1:]:
            if y-cur[-1]>gap: clusters.append(cur); cur=[y]
            else: cur.append(y)
        clusters.append(cur)
        for cl in clusters:
            if len(cl)<3: continue
            y0=float(np.median(cl)); w=win or 0.45*H if not moto else 0.45
            w=win or (0.5 if not moto else 0.4)
            yy=[];zz=[]
            for a in np.arange(y0-w,y0+w,0.01):
                R=Q[(Q[:,1]>=a)&(Q[:,1]<a+0.01)]
                if len(R): yy.append(a+0.005); zz.append(R[:,2].min())
            yy=np.array(yy); zz=np.array(zz); keep=zz<0.5*w
            cy,cz,r=y0,0.3,0.3
            for it in range(6):
                if keep.sum()<4: break
                cy,cz,r=fit_circle(yy[keep],zz[keep])
                d=np.abs(np.hypot(yy-cy,zz-cz)-r); keep=(d<0.02+0.03*r)&(zz<cz)
            # tire x range from bottom tread
            dd=np.hypot(Q[:,1]-cy,Q[:,2]-cz)
            T=Q[(dd>0.85*r)&(dd<1.03*r)&(Q[:,2]<cz-0.4*r)]
            xr=(float(T[:,0].min()),float(T[:,0].max())) if len(T) else (None,None)
            res.append(dict(side=sg,cy=round(float(cy),3),cz=round(float(cz),3),r=round(float(r),3),n=int(keep.sum()),x0=round(xr[0],3) if xr[0] is not None else None,x1=round(xr[1],3) if xr[1] is not None else None))
    return res


def find_wheels2(o, gap=0.25, side_frac=0.5, moto=False, band=None):
    P=np.array([tuple(v.co) for v in o.data.vertices])
    xm=P[:,0].max(); H=P[:,2].max()
    res=[]
    for sg in ([0] if moto else [1,-1]):
        if moto: Q=P[np.abs(P[:,0])<(band or 0.1)]
        else: Q=P[sg*P[:,0]>side_frac*xm]
        g=Q[Q[:,2]<0.03]
        ys=np.sort(g[:,1])
        if len(ys)==0: continue
        clusters=[]; cur=[ys[0]]
        for y in ys[1:]:
            if y-cur[-1]>gap: clusters.append(cur); cur=[y]
            else: cur.append(y)
        clusters.append(cur)
        for cl in clusters:
            if len(cl)<2: continue
            cy=float((min(cl)+max(cl))/2)
            est=[]
            for a in np.arange(cy-0.9,cy+0.9,0.01):
                R=Q[(Q[:,1]>=a)&(Q[:,1]<a+0.01)]
                if not len(R): continue
                dy=a+0.005-cy; z=R[:,2].min()
                if 0.02<z<0.6 and abs(dy)>0.05: est.append((dy*dy+z*z)/(2*z))
            if not est: continue
            est=np.array(est); r0=float(np.median(est))
            est=est[np.abs(est-r0)<0.15*r0]; r=float(np.median(est)) if len(est) else r0
            dd=np.hypot(Q[:,1]-cy,Q[:,2]-r)
            T=Q[(dd>0.85*r)&(dd<1.03*r)&(Q[:,2]<0.6*r)]
            xr=(round(float(T[:,0].min()),3),round(float(T[:,0].max()),3)) if len(T) else (None,None)
            res.append(dict(side=sg,cy=round(cy,3),cz=round(r,3),r=round(r,3),n=len(est),x0=xr[0],x1=xr[1]))
    return res


def wheel_from_dark(o, w, S=None, vmax=0.22, zcap=None):
    if S is None: S=face_samples(o)
    h,s,v=hsv(S['col']); c=S['cen']
    sg=w['side']; x0,x1=w['x0'],w['x1']
    if sg==0: m=(np.abs(c[:,0])<max(abs(x0),abs(x1))+0.02)
    else: m=(c[:,0]>=min(x0,x1)-0.02)&(c[:,0]<=max(x0,x1)+0.02)
    m&=(np.abs(c[:,1]-w['cy'])<1.0)&(c[:,2]<(zcap or 1.6))&(v<vmax)
    Q=c[m]
    if len(Q)<20: return None
    y2,y98=np.percentile(Q[:,1],[1,99]); z98=np.percentile(Q[:,2],99)
    return dict(cy=round(float((y2+y98)/2),3),ry=round(float((y98-y2)/2),3),rz=round(float(z98/2),3),n=int(m.sum()))


def refine_wheel(o, side, cy0, r0, x0=None, x1=None, dcy=0.2, dr=0.15):
    P=np.array([tuple(v.co) for v in o.data.vertices])
    xm=P[:,0].max()
    if side==0: Q=P[np.abs(P[:,0])<0.12]
    elif x0 is not None: Q=P[(P[:,0]>=min(x0,x1)-0.01)&(P[:,0]<=max(x0,x1)+0.01)]
    else: Q=P[side*P[:,0]>0.5*xm]
    best=None
    for cy in np.arange(cy0-dcy,cy0+dcy+1e-6,0.01):
        M=Q[np.abs(Q[:,1]-cy)<1.1*(r0+dr)]
        bins={}
        for p in M:
            k=int(round((p[1]-cy)/0.01))
            if k not in bins or p[2]<bins[k]: bins[k]=p[2]
        ks=np.array(list(bins.keys()))*0.01; zs=np.array(list(bins.values()))
        for r in np.arange(r0-dr,r0+dr+1e-6,0.005):
            m=np.abs(ks)<0.8*r
            if m.sum()<10: continue
            pred=r-np.sqrt(np.maximum(r*r-ks[m]**2,0))
            err=zs[m]-pred
            # points below the circle (skirt etc.) are allowed (clip), points above count fully
            e=np.where(err>0,err,np.minimum(-err,0.01))
            cost=float(np.mean(e))
            # coverage: tire edges at |dy|~0.75r should exist
            if best is None or cost<best[0]: best=(cost,cy,r)
    return dict(cost=round(best[0],4),cy=round(float(best[1]),3),r=round(float(best[2]),3))


WHEELS={
 'V12':[(-1.225,0.395),(1.34,0.39)],'V13':[(-1.32,0.41),(1.305,0.41)],'V14':[(-1.15,0.37),(1.15,0.37)],
 'V15':[(-1.625,0.35),(1.525,0.35)],'V16':[(-2.0,0.63),(2.025,0.63)],'V17':[(-2.38,0.81),(2.62,0.81)],
 'V18':[(-6.11,0.87),(-1.05,0.87),(4.9,0.87),(6.63,0.87)],'V19':[(-1.81,0.58),(1.775,0.58)],
 'V20':[(-1.295,0.39),(1.205,0.39)],'V21':[(-1.27,0.43),(1.425,0.43)],'V22':[(-1.145,0.35),(1.37,0.37)],
 'V23':[(-1.285,0.38),(1.37,0.37)],'V24':[(-1.3625,0.39),(1.4,0.385)],'MT01':[(-0.70,0.31),(0.70,0.315)],'MT02':[(-0.895,0.29),(0.90,0.295)]}

def tire_x(o, cy, r, side):
    P=np.array([tuple(v.co) for v in o.data.vertices])
    dd=np.hypot(P[:,1]-cy,P[:,2]-r)
    T=P[(dd>0.85*r)&(dd<1.03*r)&(P[:,2]<0.5*r)]
    if side!=0: T=T[side*T[:,0]>0.05]
    if not len(T): return None
    return float(T[:,0].min()),float(T[:,0].max())

def gap_test(o, cy, r, x0, x1):
    P=np.array([tuple(v.co) for v in o.data.vertices])
    m=(P[:,0]>=x0)&(P[:,0]<=x1)&(P[:,2]>r+0.2*r)
    d=np.hypot(P[m,1]-cy,P[m,2]-r)
    tire=int(((d>0.8*r)&(d<=1.0*r)).sum()); ann=int(((d>1.02*r)&(d<1.15*r)).sum())
    return tire,ann


# ===================== BUILDER 18-nov =====================
import cc_zonas as Zb
Zb.PAL.update({'Motor':(0.06,0.06,0.065),'Motor_Metal':(0.32,0.33,0.35),'Motor_Bahia':(0.03,0.03,0.035),
               'Interior':(0.045,0.045,0.05),'Asiento':(0.09,0.085,0.08),'Tablero':(0.05,0.05,0.055),'Cristal':(0.55,0.68,0.78)})

def cristal_mat():
    m=bpy.data.materials.get('Cristal')
    if m: return m
    m=Zb.mat('Cristal')
    nt=m.node_tree; b=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
    b.inputs['Alpha'].default_value=0.28; b.inputs['Roughness'].default_value=0.04
    b.inputs['Metallic'].default_value=0.0
    try: b.inputs['Specular IOR Level'].default_value=0.8
    except Exception: pass
    try: m.blend_method='BLEND'
    except Exception: pass
    m.diffuse_color=(0.55,0.68,0.78,0.3)
    return m

def face_adjacency(me, idx):
    sel=set(idx); emap={}
    for p in me.polygons:
        if p.index not in sel: continue
        for ek in p.edge_keys: emap.setdefault(ek,[]).append(p.index)
    adj={i:set() for i in sel}
    for ek,fs in emap.items():
        for a in fs:
            for b in fs:
                if a!=b: adj[a].add(b)
    return adj

def clean_selection(o, idx, min_area=0.03, fill=True):
    me=o.data
    idx=set(int(i) for i in idx)
    if fill:
        # add unselected faces whose all edge-neighbours are selected
        emap={}
        for p in me.polygons:
            for ek in p.edge_keys: emap.setdefault(ek,[]).append(p.index)
        for it in range(5):
            add=[]
            for p in me.polygons:
                if p.index in idx: continue
                nb=[f for ek in p.edge_keys for f in emap[ek] if f!=p.index]
                if nb and sum(f in idx for f in nb)>=0.6*len(nb): add.append(p.index)
            if not add: break
            idx|=set(add)
    adj=face_adjacency(me,idx); seen=set(); keep=[]
    for i in idx:
        if i in seen: continue
        comp=[i]; seen.add(i); st=[i]
        while st:
            a=st.pop()
            for b in adj[a]:
                if b not in seen: seen.add(b); comp.append(b); st.append(b)
        if sum(me.polygons[f].area for f in comp)>=min_area: keep+=comp
    return keep

def classify_glass(o, fence, rule):
    S=face_samples(o); h,s,v=hsv(S['col']); c=S['cen']; n=S['nor']
    if o.parent is not None and o.parent.type=='MESH' or (o.parent is not None and o.parent.type=='EMPTY' and not o.name.endswith('_Carroceria')):
        M=np.array(o.matrix_local); c=(np.c_[c,np.ones(len(c))]@M.T)[:,:3]
        R=M[:3,:3]; n=n@R.T
    y0,y1,z0,z1=fence
    m=(c[:,1]>y0)&(c[:,1]<y1)&(c[:,2]>z0)&(c[:,2]<z1)&(np.abs(n[:,2])<rule.get('nzmax',0.95))
    blue=(h>=rule.get('hmin',190))&(h<=rule.get('hmax',262))&(s>=rule.get('smin',0.05))&(s<=rule.get('smax',0.75))&(v<=rule.get('vmax',0.45))&(v>=rule.get('vmin',0.0))
    dark=(v<rule.get('dark_v',0.3))&(s<rule.get('dark_s',0.25)) if rule.get('dark',True) else np.zeros(len(v),bool)
    sel=set(np.where(m&(blue|dark))[0].tolist())
    # grow into lighter reflections of the same window
    soft=m&(((h>=rule.get('hmin',190)-15)&(h<=rule.get('hmax',262)+15)&(v<=rule.get('vmax',0.45)+0.25))|((v<rule.get('vmax',0.45)+0.1)&(s<0.3)))
    if rule.get('dark',True)==False: soft=m&(h>=rule.get('hmin',190)-15)&(h<=rule.get('hmax',262)+15)&(v<=rule.get('vmax',0.45)+0.25)&(s>=rule.get('smin',0.05)*0.6)
    me=o.data; emap={}
    for p in me.polygons:
        for ek in p.edge_keys: emap.setdefault(ek,[]).append(p.index)
    for it in range(rule.get('grow',6)):
        add=set()
        for i in list(sel):
            for ek in me.polygons[i].edge_keys:
                for f in emap[ek]:
                    if f not in sel and soft[f]: add.add(f)
        if not add: break
        sel|=add
    return clean_selection(o,sorted(sel),min_area=rule.get('min_area',0.03))

def make_glass(o, idx, base_name, root, parent=None, split=True):
    """separate window faces into Cristal objects (offset 2 mm inward not needed)"""
    out=[]
    if not idx: return out
    me=o.data
    groups={}
    if split:
        for i in idx:
            p=me.polygons[i]; ny=p.normal.y; nx=p.normal.x
            if abs(nx)>=0.55: k='Laterales'
            elif ny<0: k='Parabrisas'
            else: k='Medallon'
            groups.setdefault(k,[]).append(i)
    else: groups[base_name]=list(idx)
    # separate in one pass per group (indices shift after deletion -> separate all, then split by normals)
    g=K.separate_faces(o,list(idx),o.name+'_cristal_tmp'); K.clean_loose(g)
    mat=cristal_mat()
    g.data.materials.clear(); g.data.materials.append(mat)
    for p in g.data.polygons: p.material_index=0
    if not split:
        g.name=base_name; g.data.name=base_name
        mw=g.matrix_world.copy(); g.parent=parent or root; g.matrix_world=mw
        return [g]
    # split by normal
    bm=bmesh.new(); bm.from_mesh(g.data); bm.faces.ensure_lookup_table()
    cats={}
    for f in bm.faces:
        nx,ny=f.normal.x,f.normal.y
        k='Laterales' if abs(nx)>=0.55 else ('Parabrisas' if ny<0 else 'Medallon')
        cats.setdefault(k,[]).append(f.index)
    bm.free()
    names=list(cats.keys())
    for k in names[1:]:
        ob=K.separate_faces(g,cats_index(g,k),base_name+'_'+k); K.clean_loose(ob); out.append(ob)
    g.name=base_name+'_'+names[0]; g.data.name=g.name; out.append(g)
    for ob in out:
        mw=ob.matrix_world.copy(); ob.parent=parent or root; ob.matrix_world=mw
    return out

def cats_index(g,k):
    res=[]
    for p in g.data.polygons:
        nx,ny=p.normal.x,p.normal.y
        kk='Laterales' if abs(nx)>=0.55 else ('Parabrisas' if ny<0 else 'Medallon')
        if kk==k: res.append(p.index)
    return res


def get_ru(kind='RU01'):
    nm='SRC_'+kind
    if nm in bpy.data.objects: return bpy.data.objects[nm]
    import os, cc_tools as C
    d=os.path.join(C.ROOT,'carros','_fuentes',kind+('COCHE' if kind=='RU01' else 'MOTO'))
    fbx=[os.path.join(r,x) for r,ds,fs in os.walk(d) for x in fs if x.lower().endswith('.fbx')][0]
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fbx)
    new=[o for o in bpy.data.objects if o not in before]
    o=[x for x in new if x.type=='MESH'][0]
    for x in new:
        if x is not o: bpy.data.objects.remove(x)
    o.data.transform(o.matrix_world); o.matrix_world=Matrix.Identity(4)
    P=np.array([tuple(v.co) for v in o.data.vertices]); c=(P.min(0)+P.max(0))/2
    o.data.transform(Matrix.Translation(-Vector(c)))
    o.name=nm; o.location=(0,-30,0); o.hide_set(True)
    return o

def place_ru(code, nm, loc, r, width, root, kind='RU01'):
    src=get_ru(kind)
    P=np.array([tuple(v.co) for v in src.data.vertices]); d=P.max(0)-P.min(0)
    me=src.data.copy(); me.name=code+'_'+nm
    me.transform(Matrix.Diagonal((width/d[0],2*r/d[1],2*r/d[2],1)))
    ob=bpy.data.objects.new(code+'_'+nm,me)
    for cl in root.users_collection: cl.objects.link(ob)
    ob.parent=root; ob.location=loc
    return ob

def tire_xrange(P, cy, r, side, xm):
    dd=np.hypot(P[:,1]-cy,P[:,2]-r)
    T=P[(dd>0.85*r)&(dd<1.03*r)&(P[:,2]<0.5*r)&(side*P[:,0]>0.4*xm)]
    if len(T)<5: return None
    return float(T[:,0].min()),float(T[:,0].max())

def do_wheels_ru(code, w, root, wheels):
    """delete original tires, place RU01; wheels=[(cy,r)] front->rear"""
    P=np.array([tuple(v.co) for v in w.data.vertices]); xm=P[:,0].max()
    names=[]
    for k,(cy,r) in enumerate(wheels):
        ax='D' if k==0 else ('T' if len(wheels)==2 else 'T%d'%k)
        XR={}
        for side in (1,-1):
            xr=tire_xrange(P,cy,r,side,xm)
            if xr is None: xr=(side*0.8*xm-0.15,side*0.8*xm+0.15)
            XR[side]=(min(xr),max(xr))
        cm=(abs(sum(XR[1]))/2+abs(sum(XR[-1]))/2)/2
        wm=max(((XR[1][1]-XR[1][0])+(XR[-1][1]-XR[-1][0]))/2,0.12)
        for side,sn in ((1,'I'),(-1,'D')):
            sx0,sx1=sorted((side*(cm-wm/2),side*(cm+wm/2)))
            x0d,x1d=min(XR[side][0],sx0),max(XR[side][1],sx1)
            idx=K.faces_in(w,lambda p,cy=cy,r=r,x0=x0d,x1=x1d:(p[1]-cy)**2+(p[2]-r)**2<=(0.985*r)**2 and x0-0.01<=p[0]<=x1+0.01)
            x0,x1=sx0,sx1
            bm=bmesh.new(); bm.from_mesh(w.data); bm.faces.ensure_lookup_table()
            bmesh.ops.delete(bm,geom=[bm.faces[i] for i in idx],context='FACES'); bm.to_mesh(w.data); bm.free()
            nm='Rueda_%s%s'%(ax,sn)
            place_ru(code,nm,((x0+x1)/2,cy,r),r,max(x1-x0,0.12),root)
            names.append((nm,round((x0+x1)/2,3),cy,r,round(x1-x0,3)))
    K.clean_loose(w)
    return names

def do_wheels_moto(code, w, root, wheels, hw=0.105, rk=1.0):
    out=[]
    for nm,(cy,r) in zip(('Rueda_D','Rueda_T'),wheels):
        idx=K.faces_in(w,lambda p,cy=cy,r=r:(p[1]-cy)**2+(p[2]-r)**2<=(rk*r)**2 and abs(p[0])<=hw)
        ob=K.separate_faces(w,idx,code+'_'+nm); K.clean_loose(ob)
        P=np.array([tuple(v.co) for v in ob.data.vertices]); c=(P.min(0)+P.max(0))/2
        K.set_origin(ob,ob.matrix_world@Vector((0,c[1],c[2])))
        mw=ob.matrix_world.copy(); ob.parent=root; ob.matrix_world=mw
        out.append((nm,round(float(c[1]),3),round(float(c[2]),3)))
    K.clean_loose(w)
    return out


def top_profile(w, x, y0, y1, step=0.05):
    from mathutils.bvhtree import BVHTree
    me=w.data; T=BVHTree.FromPolygons([v.co.copy() for v in me.vertices],[list(p.vertices) for p in me.polygons])
    out=[]
    for y in np.arange(y0,y1,step):
        h=T.ray_cast(Vector((x,y,50)),Vector((0,0,-1)),100)
        out.append((round(float(y),3),round(h[0].z,3) if h[0] else None))
    return out,T

def half_width_at(w, y, z):
    from mathutils.bvhtree import BVHTree
    me=w.data; T=BVHTree.FromPolygons([v.co.copy() for v in me.vertices],[list(p.vertices) for p in me.polygons])
    h=T.ray_cast(Vector((20,y,z)),Vector((-1,0,0)),40)
    return h[0].x if h[0] else None

def engine_mesh(code, yf, yr, xh, zt, zb, root):
    """dark engine bay (inward box) + engine block with some shape"""
    b=Zb.Builder(code+'_Motor')
    L=yr-yf; W=2*xh; H=zt-zb
    # bay: inward-facing box (skip top)
    x0,x1,y0,y1=-xh+0.02,xh-0.02,yf+0.04,yr-0.03
    b.face([(x0,y0,zb),(x1,y0,zb),(x1,y1,zb),(x0,y1,zb)],'Motor_Bahia')          # floor (up)
    b.face([(x0,y0,zb),(x0,y0,zt),(x1,y0,zt),(x1,y0,zb)],'Motor_Bahia')          # front wall (faces +y)
    b.face([(x1,y1,zb),(x1,y1,zt),(x0,y1,zt),(x0,y1,zb)],'Motor_Bahia')          # back wall (faces -y)
    b.face([(x0,y1,zb),(x0,y1,zt),(x0,y0,zt),(x0,y0,zb)],'Motor_Bahia')          # left wall faces +x
    b.face([(x1,y0,zb),(x1,y0,zt),(x1,y1,zt),(x1,y1,zb)],'Motor_Bahia')          # right wall faces -x
    cy=yf+0.55*L; bw=0.42*W; bl=0.5*L; bh=0.55*H
    bx0,bx1=-bw/2,bw/2; by0,by1=cy-bl/2,cy+bl/2
    b.box(bx0,bx1,by0,by1,zb,zb+bh,'Motor')                                    # bloque
    b.box(bx0+0.05*bw,bx1-0.05*bw,by0+0.04*bl,by1-0.04*bl,zb+bh,zb+bh*1.18,'Motor_Metal')  # tapa de punterias
    for k in range(4):                                                         # bujias/cables
        yy=by0+bl*(k+0.5)/4
        b.cyl(0,yy,0.035*bw/0.5,zb+bh*1.18,zb+bh*1.3,'Motor',sides=6)
    # multiple de admision (tubo a lo largo)
    b.tube([(bx1+0.04,by0,zb+0.7*bh),(bx1+0.06,by1,zb+0.7*bh)],0.06*bh/0.3 if bh>0 else 0.04,'Motor_Metal',sides=8)
    # filtro de aire (cilindro) y bateria
    b.cyl(-0.32*W,yf+0.3*L,0.11*min(W,L),zb,zb+0.6*H,'Motor',sides=10)
    b.box(0.22*W,0.4*W,yr-0.35*L,yr-0.08*L,zb,zb+0.45*H,'Motor_Metal')
    # radiador al frente
    b.box(-0.38*W,0.38*W,y0+0.02,y0+0.06,zb+0.05*H,zt-0.08*H,'Motor_Metal')
    ob=b.finish()
    for cl in ob.users_collection: cl.objects.unlink(ob)
    for cl in root.users_collection: cl.objects.link(ob)
    ob.parent=root
    return ob

XLIM={}
def cut_hood2(code, w, root, yf, yr, xh=None, tol=0.03):
    from mathutils.bvhtree import BVHTree
    ym=(yf+yr)/2
    prof,T=top_profile(w,0.0,yf,yr,0.02)
    zs=[z for y,z in prof if z is not None]; zt_mid=float(np.median(zs))
    if xh is None:
        hw=half_width_at(w,ym,zt_mid-0.08) or 0.8
        xh=0.78*abs(hw)
        if XLIM.get(code): xh=min(xh,XLIM[code])
    for co,no in (((0,yf,0),(0,1,0)),((0,yr,0),(0,1,0)),((xh,0,0),(1,0,0)),((-xh,0,0),(1,0,0))):
        K.bisect(w,co,no)
    me=w.data
    T=BVHTree.FromPolygons([v.co.copy() for v in me.vertices],[list(p.vertices) for p in me.polygons])
    sel=[]
    for p in me.polygons:
        c=p.center
        if not (yf<c.y<yr and abs(c.x)<xh and c.z>zt_mid-0.6): continue
        h=T.ray_cast(Vector((c.x,c.y,c.z+20)),Vector((0,0,-1)),40)
        if h[0] is not None and c.z>h[0].z-tol and p.normal.z>-0.2: sel.append(p.index)
    hood=K.separate_faces(w,sel,code+'_Cofre'); K.clean_loose(hood); K.clean_loose(w)
    bm=bmesh.new(); bm.from_mesh(hood.data)
    d=bmesh.ops.duplicate(bm,geom=bm.faces[:])
    bmesh.ops.reverse_faces(bm,faces=[g for g in d['geom'] if isinstance(g,bmesh.types.BMFace)])
    bmesh.ops.translate(bm,verts=[g for g in d['geom'] if isinstance(g,bmesh.types.BMVert)],vec=(0,0,-0.01))
    bm.to_mesh(hood.data); bm.free()
    P=np.array([tuple(v.co) for v in hood.data.vertices])
    rear=P[P[:,1]>yr-0.03]; hz=float(rear[:,2].max()) if len(rear) else float(P[:,2].max())
    K.set_origin(hood,root.matrix_world@Vector((0,yr,hz)))
    mw=hood.matrix_world.copy(); hood.parent=root; hood.matrix_world=mw
    zlow=float(P[:,2].min())
    zb=zlow-max(0.35,0.45*(zt_mid-zlow+0.3))
    eng=engine_mesh(code,yf,yr,xh,zlow-0.02,zb,root)
    return dict(n=len(sel),hinge=(0,round(yr,3),round(hz,3)),xh=round(xh,3),zb=round(zb,3))

def cut_door2(code, w, root, nm, sx, yf, yr, zb, zt, tol=0.08, arch=None):
    """arch=(cy,cz,R) faces inside this circle (y,z) are kept on the body"""
    from mathutils.bvhtree import BVHTree
    for co,no in (((0,yf,0),(0,1,0)),((0,yr,0),(0,1,0)),((0,0,zb),(0,0,1)),((0,0,zt),(0,0,1))):
        K.bisect(w,co,no)
    me=w.data
    T=BVHTree.FromPolygons([v.co.copy() for v in me.vertices],[list(p.vertices) for p in me.polygons])
    sel=[]
    for p in me.polygons:
        c=p.center
        if not (yf<c.y<yr and zb<c.z<zt and sx*c.x>0.05): continue
        if arch and (c.y-arch[0])**2+(c.z-arch[1])**2<arch[2]**2: continue
        if abs(p.normal.x)<0.35 and c.z>zb+0.55*(zt-zb): continue
        h=T.ray_cast(Vector((sx*30.0,c.y,c.z)),Vector((-sx,0,0)),60)
        if h[0] is not None and sx*c.x>sx*h[0].x-tol: sel.append(p.index)
    door=K.separate_faces(w,sel,code+'_'+nm); K.clean_loose(door); K.clean_loose(w)
    bm=bmesh.new(); bm.from_mesh(door.data)
    d=bmesh.ops.duplicate(bm,geom=bm.faces[:])
    bmesh.ops.reverse_faces(bm,faces=[g for g in d['geom'] if isinstance(g,bmesh.types.BMFace)])
    bmesh.ops.translate(bm,verts=[g for g in d['geom'] if isinstance(g,bmesh.types.BMVert)],vec=(-sx*0.012,0,0))
    bm.to_mesh(door.data); bm.free()
    P=np.array([tuple(v.co) for v in door.data.vertices])
    xo=float(P[:,0].max()) if sx>0 else float(P[:,0].min())
    hinge=Vector((xo-sx*0.03,yf,(zb+zt)/2))
    K.set_origin(door,root.matrix_world@hinge)
    mw=door.matrix_world.copy(); door.parent=root; door.matrix_world=mw
    return door, dict(n=len(sel),hinge=[round(x,3) for x in hinge])

def interior_mesh(code, root, y0, y1, zf, zr, xi, kind='car', drv=1):
    """dark cabin liner (inward) + seats + dashboard + steering wheel"""
    b=Zb.Builder(code+'_Interior')
    M='Interior'
    x0,x1=-xi,xi
    b.face([(x0,y0,zf),(x1,y0,zf),(x1,y1,zf),(x0,y1,zf)],M)        # piso (up)
    b.face([(x0,y0,zr),(x0,y1,zr),(x1,y1,zr),(x1,y0,zr)],M)        # techo (down)
    b.face([(x0,y0,zf),(x0,y0,zr),(x1,y0,zr),(x1,y0,zf)],M)        # frente (faces +y)
    b.face([(x1,y1,zf),(x1,y1,zr),(x0,y1,zr),(x0,y1,zf)],M)        # atras (faces -y)
    b.face([(x0,y1,zf),(x0,y1,zr),(x0,y0,zr),(x0,y0,zf)],M)        # izq (faces +x)
    b.face([(x1,y0,zf),(x1,y0,zr),(x1,y1,zr),(x1,y1,zf)],M)        # der (faces -x)
    H=zr-zf; L=y1-y0
    sh=min(0.42,0.38*H); bh=min(0.62,0.52*H)
    sw=min(0.55,0.42*xi)
    # tablero
    b.box(x0,x1,y0,y0+min(0.5,0.22*L),zf+0.45*H,zf+0.62*H,'Tablero')
    # volante (lado del conductor +X)
    sxw=drv*0.45*xi; ys=y0+min(0.5,0.22*L)+0.12
    pts=[]
    for k in range(13):
        a=2*math.pi*k/12; pts.append((sxw+0.17*math.cos(a),ys+0.05*math.sin(a),zf+0.62*H+0.15*math.sin(a)))
    b.tube(pts,0.02,'Tablero',sides=4,caps=False)
    def seat(cx,cy,w):
        b.box(cx-w/2,cx+w/2,cy-0.25,cy+0.25,zf,zf+sh,'Asiento')
        b.box(cx-w/2,cx+w/2,cy+0.2,cy+0.32,zf+sh,zf+sh+bh,'Asiento')
    rows=[]
    yfront=y0+min(0.5,0.22*L)+0.75
    if kind=='bus':
        y=yfront
        while y+0.3<y1:
            rows.append(y); y+=0.85
    else:
        rows=[yfront]
        if L>2.6: rows.append(yfront+0.95)
    for k,yy in enumerate(rows):
        if k==0:
            seat(0.45*xi,yy,sw); seat(-0.45*xi,yy,sw)
        else:
            b.box(-0.85*xi,0.85*xi,yy-0.25,yy+0.25,zf,zf+sh,'Asiento')
            b.box(-0.85*xi,0.85*xi,yy+0.2,yy+0.32,zf+sh,zf+sh+bh,'Asiento')
    ob=b.finish()
    for cl in ob.users_collection: cl.objects.unlink(ob)
    for cl in root.users_collection: cl.objects.link(ob)
    ob.parent=root
    return ob


SPEC={
 'V12':dict(hood=(-2.05,-1.0),door=(-0.8,0.5,0.32,1.47),fence=(-1.05,1.65,0.92,1.55),rule=dict(dark=False,vmax=0.42)),
 'V13':dict(hood=(-2.15,-0.9),door=(-0.76,0.245,0.46,1.69),fence=(-0.95,1.95,1.1,1.75),rule=dict(vmax=0.33,dark_v=0.3)),
 'V14':dict(hood=(-1.75,-0.8),door=(-0.62,0.5,0.39,1.86),fence=(-0.75,1.55,1.05,1.92),rule=dict(vmax=0.4)),
 'V15':dict(hood=(-2.35,-1.2),door=(-0.9,0.175,0.25,1.35),fence=(-1.15,1.85,0.95,1.4),rule=dict(vmax=0.6,smin=0.12,dark=False)),
 'V16':dict(hood=(-3.25,-2.7),door=(-1.95,-0.83,0.9,3.3),fence=(-3.2,3.6,2.0,3.4),rule=dict(vmax=0.4),kind='bus'),
 'V17':dict(hood=(-3.6,-2.75),door=(-2.45,-1.28,1.88,3.45),fence=(-2.95,-0.95,2.55,3.5),rule=dict(vmax=0.33)),
 'V18':dict(hood=(-7.6,-5.75),door=(-5.69,-3.93,2.07,4.34),fence=(-6.0,-3.85,3.0,4.45),rule=dict(vmax=0.4)),
 'V19':dict(hood=(-2.85,-1.75),door=(-1.55,-0.18,0.86,2.69),fence=(-2.0,3.0,1.9,2.9),rule=dict(vmax=0.4)),
 'V20':dict(hood=(-2.0,-1.0),door=(-0.85,0.5,0.27,1.38),fence=(-1.0,1.5,0.95,1.42),rule=dict(vmax=0.4,smin=0.12)),
 'V21':dict(hood=(-1.9,-0.9),door=(-0.67,0.6,0.45,1.83),fence=(-0.85,1.8,1.15,1.9),rule=dict(vmax=0.4)),
 'V22':dict(hood=(-2.1,-1.0),door=(-0.6,0.6,0.34,0.96),fence=(-1.0,1.2,0.75,1.02),rule=dict(smin=0.25,vmin=0.2,vmax=0.65,hmin=170,dark=False)),
 'V23':dict(hood=(-2.1,-1.05),door=(-0.6,0.5,0.24,0.95),fence=(-1.1,1.2,0.75,1.05),rule=dict(vmax=0.45)),
 'V24':dict(hood=(-2.1,-0.95),door=(-0.6,0.7,0.25,1.39),fence=(-0.9,1.5,0.95,1.33),rule=dict(vmax=0.36,smin=0.14,dark=False)),
}

def new_root(code, loc):
    col=bpy.data.collections.get(code) or bpy.data.collections.new(code)
    if col.name not in [c.name for c in bpy.context.scene.collection.children]: bpy.context.scene.collection.children.link(col)
    root=bpy.data.objects.get(code) or bpy.data.objects.new(code,None)
    if root.name not in col.objects: col.objects.link(root)
    root.location=loc
    return root,col

def clear_vehicle(code):
    root=bpy.data.objects.get(code)
    if not root: return
    for o in list(root.children_recursive):
        me=o.data if o.type=='MESH' else None
        bpy.data.objects.remove(o)
        if me and me.users==0: bpy.data.meshes.remove(me)

def build_car(code, sp=None, steps=('wheels','hood','doors','glass','interior')):
    sp=sp or SPEC[code]
    src=bpy.data.objects['SRC_'+code]
    clear_vehicle(code)
    final_loc=tuple(src.location)
    root,col=new_root(code,(0,0,0))
    w=bpy.data.objects.new(code+'_Carroceria',src.data.copy()); w.data.name=code+'_Carroceria'
    col.objects.link(w); w.parent=root
    src.hide_set(True); bpy.context.view_layer.update()
    rep={}
    wheels=WHEELS[code]
    if 'wheels' in steps:
        rep['ruedas']=do_wheels_ru(code,w,root,wheels)
        XLIM[code]=min(abs(r[1])-r[4]/2 for r in rep['ruedas'] if r[2]==wheels[0][0])-0.06
    if 'hood' in steps and sp.get('hood_cut',True): rep['cofre']=cut_hood2(code,w,root,*sp['hood'])
    doors=[]
    if 'doors' in steps and sp.get('door'):
        yf,yr,zb,zt=sp['door']; cy,r=wheels[0]
        for nm,sx in (('Puerta_I',1),('Puerta_D',-1)):
            d,info=cut_door2(code,w,root,nm,sx,yf,yr,zb,zt,tol=sp.get('dtol',0.08),arch=(cy,r,r+0.12)); doors.append(d); rep[nm]=info
    if 'glass' in steps:
        idx=classify_glass(w,sp['fence'],sp['rule'])
        gl=make_glass(w,idx,code+'_Cristal',root)
        rep['cristales']=[(g.name,len(g.data.polygons)) for g in gl]
        for d in doors:
            di=classify_glass(d,sp['fence'],dict(sp['rule'],min_area=0.02))
            if di:
                g=make_glass(d,di,code+'_Cristal_'+d.name.split('_',1)[1],root,parent=d,split=False)
                rep['cristal_'+d.name]=len(g[0].data.polygons)
    if 'interior' in steps:
        y0,y1,z0,z1=sp['fence']
        P=np.array([tuple(v.co) for v in w.data.vertices]); xm=P[:,0].max()
        zf=max(wheels[0][1]*0.9, (sp['door'][2]+0.08) if sp.get('door') else wheels[0][1])
        gl=[o for o in root.children_recursive if 'Cristal' in o.name]
        gy=[ (o.matrix_world@v.co).y for o in gl for v in o.data.vertices]
        y1g=max(gy) if gy else y1
        zr=z1-0.08
        y0i=sp['hood'][1]+0.05; y1i=min(y1,y1g)-0.1
        interior_props(code,root,y0i,y1i,zf,zr,sp.get('xi',0.7)*xm,kind=sp.get('kind','car'))
        inner_shell(code,root,[w],sp['hood'][1]-0.05,y1g+0.05,zf-0.05)
        rep['interior']=(round(y0i,2),round(y1i,2),round(zf,2),round(zr,2))
    root.location=final_loc
    return rep


def show(code, tag, open_=False, views=((0.55,-0.8,0.45),(-0.6,0.55,0.4),(0.9,0.05,0.25)), res=(500,380)):
    import cc_tools as C, os
    root=bpy.data.objects[code]; kids=set(o.name for o in root.children_recursive)
    objs=[o for o in bpy.context.view_layer.objects if o]
    st={o.name:o.hide_get() for o in objs}
    for o in objs: o.hide_set(o.name not in kids and o is not root)
    if open_:
        for n,ax,a in (('Cofre','x',-50),('Puerta_I','z',-65),('Puerta_D','z',65)):
            ob=bpy.data.objects.get(code+'_'+n)
            if ob: setattr(ob.rotation_euler,ax,math.radians(a))
    P=[ (o.matrix_world@v.co) for o in root.children_recursive if o.type=='MESH' for i,v in enumerate(o.data.vertices) if i%50==0]
    ys=[p.y for p in P]; zs=[p.z for p in P]
    L=max(ys)-min(ys); l=root.location
    paths=[]
    for k,vw in enumerate(views):
        pth=os.path.join(C.REV,'_tmp_v_%s_%s_%d.png'%(code,tag,k))
        C.snap(pth,tuple(l+Vector(vw)*L*1.25),tuple(l+Vector((0,0,(max(zs))*0.4))),40,None,res); paths.append(pth)
    for n in ('Cofre','Puerta_I','Puerta_D'):
        ob=bpy.data.objects.get(code+'_'+n)
        if ob: ob.rotation_euler=(0,0,0)
    for o in objs: o.hide_set(st[o.name])
    return paths


def inner_shell(code, root, src_objs, y0, y1, z0, offset=0.015):
    """dark inner surface: copy body faces inside the cabin region, flipped and pushed inward"""
    bm=bmesh.new()
    for o in src_objs:
        tmp=bmesh.new(); tmp.from_mesh(o.data)
        M=root.matrix_world.inverted()@o.matrix_world
        tmp.transform(M)
        dele=[f for f in tmp.faces if not (y0<f.calc_center_median().y<y1 and f.calc_center_median().z>z0)]
        bmesh.ops.delete(tmp,geom=dele,context='FACES')
        me=bpy.data.meshes.new('_tmp_is'); tmp.to_mesh(me); tmp.free()
        bm.from_mesh(me); bpy.data.meshes.remove(me)
    bm.normal_update()
    for v in bm.verts: v.co-=v.normal*offset
    bmesh.ops.reverse_faces(bm,faces=bm.faces[:])
    me=bpy.data.meshes.new(code+'_Interior_Casco'); bm.to_mesh(me); bm.free()
    me.materials.clear(); me.materials.append(Zb.mat('Interior'))
    for p in me.polygons: p.material_index=0
    ob=bpy.data.objects.new(code+'_Interior_Casco',me)
    for cl in root.users_collection: cl.objects.link(ob)
    ob.parent=root
    return ob

def interior_props(code, root, y0, y1, zf, zr, xi, kind='car', drv=1):
    """floor, dashboard, wheel and seats (no liner box)"""
    b=Zb.Builder(code+'_Interior')
    M='Interior'
    b.face([(-xi,y0,zf),(xi,y0,zf),(xi,y1,zf),(-xi,y1,zf)],M)
    H=zr-zf; L=y1-y0
    sh=min(0.45,0.36*H); bh=min(0.62,0.5*H); sw=min(0.55,0.42*xi)
    dl=min(0.5,0.22*L)
    b.box(-xi,xi,y0,y0+dl,zf+0.42*H,zf+0.58*H,'Tablero')
    sxw=drv*0.45*xi; ys=y0+dl+0.1
    pts=[(sxw+0.17*math.cos(2*math.pi*k/12),ys+0.05*math.sin(2*math.pi*k/12),zf+0.6*H+0.15*math.sin(2*math.pi*k/12)) for k in range(13)]
    b.tube(pts,0.022,'Tablero',sides=4,caps=False)
    def seat(cx,cy,w):
        b.box(cx-w/2,cx+w/2,cy-0.25,cy+0.25,zf,zf+sh,'Asiento')
        b.box(cx-w/2,cx+w/2,cy+0.2,cy+0.33,zf+sh,zf+sh+bh,'Asiento')
    yfront=y0+dl+0.7
    rows=[]
    if kind=='bus':
        y=yfront
        while y+0.35<y1: rows.append(y); y+=0.85
    else:
        rows=[yfront]
        if yfront+0.95+0.35<y1: rows.append(yfront+0.95)
    for k,yy in enumerate(rows):
        if k==0: seat(0.45*xi,yy,sw); seat(-0.45*xi,yy,sw)
        else:
            b.box(-0.8*xi,0.8*xi,yy-0.25,yy+0.25,zf,zf+sh,'Asiento')
            b.box(-0.8*xi,0.8*xi,yy+0.2,yy+0.33,zf+sh,zf+sh+bh,'Asiento')
    ob=b.finish()
    for cl in ob.users_collection: cl.objects.unlink(ob)
    for cl in root.users_collection: cl.objects.link(ob)
    ob.parent=root
    return ob


# ---------------- 18-nov: motos + export ----------------
def absorb_islands(code, w, wheels, hw, rk=1.12, maxv=4000):
    """loose islands of the body that lie inside a wheel circle go to that wheel"""
    bm=bmesh.new(); bm.from_mesh(w.data); bm.faces.ensure_lookup_table()
    seen=set(); moves={}
    for f in bm.faces:
        if f.index in seen: continue
        isl=[]; st=[f]; seen.add(f.index)
        while st:
            g=st.pop(); isl.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen: seen.add(h.index); st.append(h)
        vs={v for g in isl for v in g.verts}
        if len(vs)>maxv: continue
        P=np.array([tuple(v.co) for v in vs])
        for k,(cy,r) in enumerate(wheels):
            d=np.sqrt((P[:,1]-cy)**2+(P[:,2]-r)**2)
            if d.max()<=rk*r and np.abs(P[:,0]).max()<=hw+0.06:
                moves.setdefault(k,[]).extend(g.index for g in isl); break
    bm.free()
    out={}
    for k,idx in moves.items():
        nm=code+('_Rueda_D' if k==0 else '_Rueda_T')
        wh=bpy.data.objects[nm]
        tmp=K.separate_faces(w,idx,'_tmp_isl')
        bm2=bmesh.new(); bm2.from_mesh(wh.data)
        M=wh.matrix_world.inverted()@tmp.matrix_world
        me=tmp.data.copy(); me.transform(M)
        for m in me.materials:
            if m.name not in [x.name for x in wh.data.materials if x]: wh.data.materials.append(m)
        mi=[[x.name for x in wh.data.materials].index(m.name) for m in me.materials]
        for p in me.polygons: p.material_index=mi[p.material_index] if mi else 0
        bm2.from_mesh(me); bm2.to_mesh(wh.data); bm2.free()
        bpy.data.objects.remove(tmp); bpy.data.meshes.remove(me)
        out[nm]=len(idx)
    return out

MOTO={'MT01':dict(hw=0.105),'MT02':dict(hw=0.15)}
def build_moto(code,rk=1.07,hw=None):
    hw=hw or MOTO[code]['hw']
    src=bpy.data.objects['SRC_'+code]
    clear_vehicle(code)
    final=tuple(src.location)
    root,col=new_root(code,(0,0,0))
    w=bpy.data.objects.new(code+'_Carroceria',src.data.copy()); w.data.name=code+'_Carroceria'
    col.objects.link(w); w.parent=root
    src.hide_set(True); bpy.context.view_layer.update()
    rep=do_wheels_moto(code,w,root,WHEELS[code],hw=hw,rk=rk)
    rep2=absorb_islands(code,w,WHEELS[code],hw)
    K.clean_loose(w)
    root.location=final
    return rep,rep2

def export_new(code, out_dir=None):
    """root at origin, children renamed without '<code>_', FBX with embedded textures"""
    import os, cc_tools as _CT
    out_dir=out_dir or os.path.join(_CT.ROOT,'carros')
    root=bpy.data.objects[code]; loc=root.location.copy(); root.location=(0,0,0)
    objs=list(root.children_recursive); ren=[]
    for o in objs:
        if o.name.startswith(code+'_'):
            old=o.name; o.name=old[len(code)+1:]; ren.append((o,old))
    bad=[o.name for o,_ in ren if '.' in o.name]
    bpy.context.view_layer.update()
    vl=bpy.context.view_layer
    for ob in vl.objects: ob.select_set(False)
    for ob in [root]+objs: ob.hide_set(False); ob.select_set(True)
    vl.objects.active=root
    p=os.path.join(out_dir,code+'.fbx')
    bpy.ops.export_scene.fbx(filepath=p,use_selection=True,object_types={'EMPTY','MESH'},global_scale=1.0,apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=False,add_leaf_bones=False,
        bake_anim=False,path_mode='COPY',embed_textures=True,use_mesh_modifiers=True)
    for o,old in ren: o.name=old
    root.location=loc
    return {'bad':bad,'n':len(objs),'MB':round(os.path.getsize(p)/1e6,1)}

"""Revisao de formas do proxy, criada sobre uma copia do .blend existente.

Geometria original do projeto; referencia visual externa, sem extracao de assets.
Cinco materiais e cores de vertice permitem distinguir barba, olhos e cabelo.
"""
from pathlib import Path
from math import sin, cos, pi, sqrt, exp, copysign, asin
import json
import random
import bpy
import bmesh
from mathutils import Vector, Quaternion, Euler

ROOT = Path(__file__).resolve().parents[1]
ROOT.mkdir(parents=True, exist_ok=True)
(ROOT/'previews').mkdir(parents=True, exist_ok=True)
SOURCE = Path(r'E:\Unity_Games\TW1-Remaster\art\source\blender\characters\player-wolf\SM_PlayerWolf_Proxy_v01.blend')
OUT = ROOT / 'SM_PlayerWolf_Proxy_v02.blend'
FBX = ROOT / 'SM_PlayerWolf_Proxy_v02.fbx'
random.seed(24)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
old = bpy.data.objects.get('SM_PlayerWolf_Proxy_v01')
if old is None:
    raise RuntimeError('Objeto fonte esperado nao encontrado; nenhuma fonte foi alterada.')
bpy.data.objects.remove(old, do_unlink=True)
parts = []

def material(name, color, metallic=0., roughness=.65):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    attr = nodes.new('ShaderNodeVertexColor')
    attr.layer_name = 'Color'
    mat.node_tree.links.new(attr.outputs['Color'], bsdf.inputs['Base Color'])
    return mat

skin = material('M_ProxySkin', (.32,.205,.16), roughness=.73)
hair = material('M_ProxyHair', (.46,.50,.53), roughness=.78)
cloth = material('M_ProxyCloth', (.018,.024,.030), roughness=.85)
leather = material('M_ProxyLeather', (.055,.022,.016), roughness=.55)
metal = material('M_ProxyMetal', (.105,.13,.15), metallic=.72, roughness=.43)

def finish(obj, name, mat, color=None, smooth=True):
    obj.name = name
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    c = color or mat.diffuse_color[:3]
    ca = obj.data.color_attributes.new(name='Color', type='FLOAT_COLOR', domain='CORNER')
    for a in ca.data:
        a.color = (*c[:3], 1)
    for p in obj.data.polygons:
        p.use_smooth = smooth
    parts.append(obj)
    return obj

def mesh(name, verts, faces, mat, color=None, smooth=True):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    return finish(obj, name, mat, color, smooth)

def ell(name, center, radius, mat, segments=16, rings=8, color=None):
    segments=max(8,round(segments*.75));rings=max(4,round(rings*.75))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=center)
    o = bpy.context.object
    o.scale = radius
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(o, name, mat, color)

def loft(name, rows, mat, n=24, color=None, exponent=1.):
    n=max(10,round(n*.8))
    # z, x center, y center, half-width, front depth, rear depth
    verts = []
    for z,x,y,w,f,b in rows:
        for j in range(n):
            a=2*pi*j/n
            sx=copysign(abs(sin(a))**exponent, sin(a))
            cy=copysign(abs(cos(a))**exponent, cos(a))
            verts.append((x+w*sx, y-cy*(f if cy>=0 else b), z))
    faces = []
    for i in range(len(rows)-1):
        for j in range(n):
            k=i*n+j; l=i*n+(j+1)%n
            faces.append((k,l,l+n,k+n))
    faces += [tuple(reversed(range(n))),tuple((len(rows)-1)*n+j for j in range(n))]
    return mesh(name,verts,faces,mat,color)

def tube(name, points, radii, mat, n=8, color=None, flatten=1.):
    if n>4:n=max(4,round(n*.75))
    pts=[Vector(p) for p in points]
    verts=[]
    for i,p in enumerate(pts):
        axis=(pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]).normalized()
        reference=Vector((0,1,0)) if abs(axis.y)<.92 else Vector((1,0,0))
        u=axis.cross(reference).normalized()
        v=axis.cross(u).normalized()
        for j in range(n):
            a=2*pi*j/n
            verts.append(p+radii[i]*(cos(a)*u+sin(a)*v*flatten))
    faces=[]
    for i in range(len(pts)-1):
        for j in range(n):
            k=i*n+j;l=i*n+(j+1)%n
            faces.append((k,l,l+n,k+n))
    faces += [tuple(reversed(range(n))),tuple((len(pts)-1)*n+j for j in range(n))]
    return mesh(name,verts,faces,mat,color)

def ribbon(name, points, width, mat, color=None, depth=.003):
    pts=[Vector(p) for p in points]
    vs=[]
    for i,p in enumerate(pts):
        t=(pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]).normalized()
        u=Vector((t.z,0,-t.x)).normalized()*width/2
        vs += [p-u,p+u,p-u+Vector((0,depth,0)),p+u+Vector((0,depth,0))]
    fs=[]
    for i in range(len(pts)-1):
        a=i*4;b=a+4
        fs += [(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a,a+2,b+2,b),(a+1,b+1,b+3,a+3)]
    fs += [(0,1,3,2),(len(vs)-4,len(vs)-2,len(vs)-1,len(vs)-3)]
    return mesh(name,vs,fs,mat,color)

def buckle(name, center, w, h, mat=metal):
    x,y,z=center
    p=[(x-w/2,y,z-h/2),(x-w/2,y,z+h/2),(x+w/2,y,z+h/2),(x+w/2,y,z-h/2),(x-w/2,y,z-h/2)]
    return tube(name,p,[.003]*5,mat,n=6)

# Tronco continuo: caixa toracica, cintura, costas e transicao dos ombros.
loft('Torso_Anatomical',[
    (.94,0,0,.162,.101,.097),(1.03,0,0,.161,.110,.103),
    (1.11,0,0,.150,.104,.101),(1.20,0,0,.173,.116,.110),
    (1.32,0,0,.211,.136,.117),(1.41,0,0,.234,.129,.112),
    (1.47,0,0,.223,.109,.099),(1.515,0,0,.165,.077,.082),
    (1.545,0,0,.083,.065,.066)], cloth,32)
loft('Pelvis_Tailored',[(.825,0,0,.146,.082,.096),(.91,0,0,.186,.109,.12),(.97,0,0,.183,.108,.114),(1.025,0,0,.161,.099,.103)],cloth,24)

# Peitoral curvo; evita a placa retangular do blockout.
for s in (-1,1):
    verts=[]
    for z,width,depth in [(1.235,.164,.119),(1.29,.197,.139),(1.38,.214,.14),(1.445,.194,.121),(1.482,.13,.097)]:
        for j in range(9):
            q=j/8
            x=s*(.008+(width-.008)*q)
            y=-depth*sqrt(max(.05,1-(abs(x)/(width+.04))**2))-.01
            verts.append((x,y,z+.008*sin(q*pi)))
    faces=[(i*9+j,i*9+j+1,(i+1)*9+j+1,(i+1)*9+j) for i in range(4) for j in range(8)]
    mesh(f'Chest_Panel_{s}',verts,faces,leather,(.023,.029,.033))
    ribbon(f'Chest_Seam_{s}',[(s*.01,-.153,1.29),(s*.07,-.15,1.30),(s*.15,-.12,1.325),(s*.199,-.087,1.36)],.005,leather,(.095,.073,.049))

# Cota central com aneis achatados modelados: detalhe legivel sem textura externa.
mailbase=loft('Abdominal_Mail_Base',[(1.04,0,0,.151,.115,.098),(1.14,0,0,.158,.118,.101),(1.235,0,0,.181,.127,.109)],metal,24,(.045,.060,.072))
bm=bmesh.new();bm.from_mesh(mailbase.data)
bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().y>-.022],context='FACES')
bm.to_mesh(mailbase.data);bm.free()
for row in range(10):
    z=1.052+row*.0169
    for col in range(10):
        x=(col-4.5)*.0178 + (row%2)*.0089
        y=-.126+ .017*(abs(x)/.1)**2
        rv=[]
        for r in (1.,.56):
            for j in range(8):
                a=j*2*pi/8
                rv.append((x+.0062*r*cos(a),y+.0017*sin(a),z+.0073*r*sin(a)))
        rf=[(j,(j+1)%8,(j+1)%8+8,j+8) for j in range(8)]
        mesh(f'Mail_Ring_{row}_{col}',rv,rf,metal,(.07,.085,.097))

# Colar protege o pescoco e separa claramente o queixo do tronco.
loft('Collar_Leather',[(1.49,0,0,.10,.077,.086),(1.55,0,0,.081,.068,.074),(1.577,0,0,.069,.061,.065)],leather,24,(.022,.023,.023))
loft('Neck_Anatomical',[(1.531,0,.006,.06,.053,.054),(1.585,0,.007,.057,.052,.052),(1.643,0,.007,.062,.054,.056)],skin,24)

# Faixas e fivela acompanham a cintura oval.
loft('Belt_Main',[(1.007,0,0,.172,.121,.11),(1.039,0,0,.169,.122,.11),(1.057,0,0,.166,.119,.108)],leather,32)
buckle('Belt_Buckle',(.033,-.127,1.033),.049,.033)
tube('Belt_Buckle_Tongue',[(.012,-.13,1.033),(.054,-.13,1.033)],[.002,.002],metal,6)
for x in (-.105,-.07,-.035,.083,.117):
    ell('Belt_Rivet',(x,-.129+abs(x)*.10,1.035),(.0026,.0018,.0026),metal,8,4)

# Faldoes separados, curvos e com abertura para as pernas.
for s in (-1,1):
    vs=[]
    for z,w,y in [(1.0,.169,-.115),(.91,.179,-.12),(.78,.173,-.106),(.705,.155,-.092)]:
        for j in range(7):
            q=j/6;x=s*(.014+q*(w-.014))
            vs.append((x,y+.048*q*q,z+.028*q))
    fs=[(i*7+j,i*7+j+1,(i+1)*7+j+1,(i+1)*7+j) for i in range(3) for j in range(6)]
    mesh(f'Front_Skirt_{s}',vs,fs,cloth,(.027,.033,.039))
    rear=[(x,-y+.01,z) for x,y,z in vs]
    mesh(f'Back_Skirt_{s}',rear,fs,cloth,(.022,.027,.032))
    ribbon(f'Skirt_Edge_{s}',[(s*.017,-.12,.998),(s*.017,-.125,.91),(s*.017,-.112,.78),(s*.017,-.098,.705)],.007,leather)
    # Costuras verticais, sem a antiga caixa rigida.
    for q in (.32,.63,.91):
        ribbon('Skirt_Stitch',[(s*(.014+q*.155),-.118+.048*q*q,.987),(s*(.014+q*.165),-.123+.048*q*q,.91),(s*(.014+q*.159),-.109+.048*q*q,.79)],.0023,leather,(.08,.065,.046))

# Coxas e panturrilhas com variacao de volume e joelhos orientados para frente.
for s in (-1,1):
    x=s*.102
    loft(f'Trousers_Thigh_{s}',[(.50,x,-.002,.06,.064,.068),(.57,x,0,.070,.078,.074),(.66,x,.005,.080,.087,.091),(.78,s*.104,.006,.088,.093,.100),(.88,s*.102,0,.087,.087,.094),(.938,s*.09,0,.079,.074,.081)],cloth,20)
    loft(f'Boot_Calf_{s}',[(.085,x,.004,.055,.058,.060),(.15,x,.005,.052,.056,.062),(.24,x,.009,.061,.061,.077),(.35,x,.009,.074,.067,.089),(.44,x,.002,.071,.067,.079),(.51,x,0,.061,.065,.065)],leather,20,(.026,.018,.015))
    ell(f'Knee_Cap_{s}',(x,-.053,.533),(.060,.035,.062),leather,16,8,(.036,.041,.045))
    loft('Knee_Cuff',[(.497,x,0,.065,.071,.075),(.527,x,0,.066,.076,.077)],leather,20,(.024,.018,.015))
    for z,r in [(.455,.075),(.295,.071),(.155,.057)]:
        loft(f'Boot_Strap_{s}',[(z-.009,x,0,r,.078 if z>.2 else .063,.086 if z>.2 else .066),(z+.009,x,0,r,.078 if z>.2 else .063,.086 if z>.2 else .066)],leather,20)
        buckle('Boot_Buckle',(x+s*.044,-(.069 if z>.2 else .057),z),.019,.019)
    loft(f'Boot_Foot_{s}',[(.014,x,-.041,.066,.137,.089),(.038,x,-.041,.069,.14,.091),(.071,x,-.037,.065,.131,.085),(.103,x,-.018,.056,.095,.071),(.137,x,.001,.052,.062,.061)],leather,20,(.025,.019,.016),.76)
    loft('Boot_Ankle_Join',[(.105,x,.002,.058,.073,.072),(.128,x,.002,.057,.063,.070),(.168,x,.006,.057,.061,.068)],leather,20,(.026,.018,.015))
    loft(f'Boot_Sole_{s}',[(0.,x,-.04,.068,.138,.092),(.014,x,-.04,.071,.140,.093),(.027,x,-.04,.070,.14,.092)],cloth,20,(.010,.012,.013),.76)

# Bracos: transicao deltoide/biceps, cotovelos, antebracos afunilados e maos articuladas.
for s in (-1,1):
    shoulder=Vector((s*.232,0,1.463));elbow=Vector((s*.393,-.003,1.239));wrist=Vector((s*.518,-.018,1.059))
    direction=(wrist-elbow).normalized()
    tube(f'Upper_Arm_{s}',[shoulder,shoulder.lerp(elbow,.25),shoulder.lerp(elbow,.54),shoulder.lerp(elbow,.84),elbow],[.082,.091,.081,.065,.061],cloth,20,(.046,.052,.055),.9)
    ell(f'Elbow_Leather_{s}',elbow,(.067,.061,.063),leather,16,8)
    tube('Elbow_Cuff',[elbow-direction*.020,elbow+direction*.024],[.070,.072],leather,20,(.038,.022,.018),.94)
    tube(f'Forearm_Wrap_{s}',[elbow,elbow.lerp(wrist,.22),elbow.lerp(wrist,.55),elbow.lerp(wrist,.82),wrist],[.060,.075,.066,.048,.042],cloth,20,(.17,.18,.17),.87)
    # Manguito escuro curvo na parte inferior do antebraco.
    tube(f'Bracer_{s}',[elbow.lerp(wrist,.49),elbow.lerp(wrist,.64),elbow.lerp(wrist,.85),wrist],[.069,.065,.053,.047],leather,20,(.024,.028,.030),.94)
    for t,r in [(.53,.07),(.91,.051)]:
        c=elbow.lerp(wrist,t)
        tube('Bracer_Band',[c-direction*.009,c+direction*.009],[r,r],leather,16)
    for t in (.14,.25,.36):
        c=elbow.lerp(wrist,t)
        r=.063+.012*sin(t*pi*2)
        tube('Sleeve_Seam',[c-direction*.0018,c+direction*.0018],[r,r],cloth,16,(.105,.112,.11),.87)
    # Ombreira em superficie aberta, acompanha o ombro em vez de esfera sobreposta.
    vs=[]
    for t in range(5):
        theta=.20+t*.25
        for j in range(13):
            phi=pi*j/12
            vs.append((s*(.233+.121*sin(theta)*cos(phi)),.112*sin(theta)*sin(phi)-.015,1.435+.093*cos(theta)))
    # Casca superior completa em torno do eixo vertical.
    vs=[]
    for r,z in [(.38,1.536),(.70,1.523),(1.,1.492),(1.08,1.462)]:
        for j in range(20):
            a=2*pi*j/20
            vs.append((s*.251+.104*r*sin(a),-.004-.102*r*cos(a),z-.027*max(0,s*sin(a))))
    fs=[(i*20+j,i*20+(j+1)%20,(i+1)*20+(j+1)%20,(i+1)*20+j) for i in range(3) for j in range(20)]
    fs.append(tuple(reversed(range(20))))
    mesh(f'Pauldron_Layer_{s}',vs,fs,metal,(.071,.084,.094))
    edge=[]
    for j in range(21):
        a=2*pi*j/20
        edge.append((s*.251+.11232*sin(a),-.004-.11016*cos(a),1.462-.027*max(0,s*sin(a))))
    tube('Pauldron_Leather_Rim',edge,[.005]*len(edge),leather,6)
    # Palma e dedos em luva, com quatro dedos separados e polegar oponivel.
    palm_end=wrist+direction*.081
    tube(f'Glove_Palm_{s}',[wrist,wrist+direction*.025,wrist+direction*.061,palm_end],[.040,.045,.042,.036],leather,16,(.022,.023,.025),.57)
    across=Vector((direction.z,0,-direction.x)).normalized()
    for i in range(4):
        offset=(i-1.5)*.018
        start=palm_end+across*offset
        length=[.052,.067,.073,.060][i]
        joint=start+direction*length*.52+Vector((0,-.006,0))
        tip=start+direction*length+Vector((0,-.018,0))
        tube(f'Glove_Finger_{s}_{i}',[start,joint,tip],[.011,.0095,.0075],leather,8,(.022,.023,.025),.85)
        ell('Finger_Knuckle',start+Vector((0,-.014,0)),(.010,.006,.009),metal,8,4,(.065,.074,.080))
    thumb_start=wrist+direction*.028+Vector((-s*.027,-.005,-.012))
    tube(f'Glove_Thumb_{s}',[thumb_start,thumb_start+Vector((-s*.024,-.008,-.025)),thumb_start+Vector((-s*.028,-.022,-.052))],[.015,.012,.009],leather,8,(.022,.023,.025))

# Cabeca com planos de testa, orbitas, macas do rosto, mandibula e nuca.
headrows=[(1.602,.035,.060,.025),(1.610,.051,.077,.036),(1.624,.065,.080,.046),(1.644,.077,.076,.059),(1.666,.081,.074,.071),(1.688,.084,.075,.082),(1.710,.083,.073,.086),(1.732,.081,.074,.084),(1.755,.080,.070,.081),(1.778,.080,.065,.077),(1.803,.075,.056,.070),(1.827,.060,.041,.055),(1.846,.037,.022,.034),(1.851,.009,.006,.008)]

def profile(z):
    for k,(a,b) in enumerate(zip(headrows,headrows[1:])):
        if a[0]<=z<=b[0]:
            t=(z-a[0])/(b[0]-a[0])
            p=headrows[max(0,k-1)];q=headrows[min(len(headrows)-1,k+2)]
            return tuple(.5*((2*a[i])+(-p[i]+b[i])*t+(2*p[i]-5*a[i]+4*b[i]-q[i])*t*t+(-p[i]+3*a[i]-3*b[i]+q[i])*t*t*t) for i in range(1,4))
    return headrows[0][1:] if z<headrows[0][0] else headrows[-1][1:]

def facepoint(z,angle,offset=0.):
    w,f,b=profile(z)
    sn=sin(angle);cs=cos(angle)
    power=.78 if z<1.65 else .94
    x=w*copysign(abs(sn)**power,sn)
    y=-cs*(f if cs>=0 else b)
    if cs>0:
        # Bochechas mais proeminentes, orbitas e temporas rebaixadas.
        y-=.007*exp(-((abs(x)-.053)/.024)**2-((z-1.689)/.022)**2)*cs
        y-=.003*exp(-((abs(x)-.035)/.022)**2-((z-1.714)/.012)**2)*cs
        y-=.006*exp(-(x/.030)**2-((z-1.650)/.021)**2)*cs
        y-=.011*exp(-((abs(x)-.033)/.025)**2-((z-(1.732+.09*abs(x)))/.008)**2)*cs
        # Ponte, ponta e asas nasais fazem parte da mesma superficie da face.
        y-=.028*exp(-(x/.0085)**2-((z-1.714)/.023)**2)*cs
        y-=.037*exp(-(x/.013)**2-((z-1.686)/.011)**2)*cs
        y-=.010*exp(-((abs(x)-.012)/.006)**2-((z-1.678)/.006)**2)*cs
    return (x+offset*sn,y-offset*cs,z)

def face_y(x,z):
    w,_,_=profile(z)
    power=.78 if z<1.65 else .94
    a=asin(copysign(min(.999,abs(x/w))**(1/power),x))
    return facepoint(z,a)[1]

hn=128;hz=81
vs=[facepoint(1.602+(.249*i/(hz-1)),2*pi*j/hn) for i in range(hz) for j in range(hn)]
fs=[(i*hn+j,i*hn+(j+1)%hn,(i+1)*hn+(j+1)%hn,(i+1)*hn+j) for i in range(hz-1) for j in range(hn)]
fs += [tuple(reversed(range(hn))),tuple((hz-1)*hn+j for j in range(hn))]
head=mesh('Head_Facial_Planes',vs,fs,skin)
# Otimiza a superficie continua; nao agrega volumes soltos ao rosto.
bpy.context.view_layer.objects.active=head
dec=head.modifiers.new('Facial_Surface_Resolution','DECIMATE');dec.ratio=.19
bpy.ops.object.modifier_apply(modifier=dec.name)

for s in (-1,1):
    ell('Ear',(s*.083,.003,1.701),(.015,.021,.033),skin,12,8)
    ell('Ear_Concha',(s*.091,-.011,1.70),(.006,.009,.018),skin,10,6,(.20,.105,.08))
    # Olhos estreitos sob a sobrancelha; frente inequívoca mesmo sem materiais.
    cx=s*.034;cz=1.714
    eyepts=[(cx-.016,0,cz-.001),(cx-.010,0,cz+.0035),(cx+.002,0,cz+.004),(cx+.015,0,cz+.0005),(cx+.009,0,cz-.0034),(cx-.006,0,cz-.0034)]
    eyepts=[(x,face_y(x,z)-.002,z) for x,y,z in eyepts]
    mesh('Eye_Almond',eyepts,[tuple(range(6))],skin,(.48,.46,.37))
    ey=face_y(cx,cz)-.0035
    iris=ell('Eye_Amber_Iris',(cx,ey,cz),(.0032,.0007,.0033),skin,10,5,(.27,.15,.024))
    ell('Eye_Vertical_Pupil',(cx,ey-.0008,cz),(.00065,.0004,.0028),cloth,8,4,(.006,.004,.002))
    tube('Upper_Eyelid',eyepts[:4],[.001,.0013,.0013,.0009],skin,6,(.245,.143,.102))
    tube('Lower_Eyelid',[eyepts[3],eyepts[4],eyepts[5],eyepts[0]],[.0009,.0011,.0011,.0009],skin,6)
    eyebrow=[]
    for x,z in [(s*.014,1.730),(s*.027,1.737),(s*.043,1.739),(s*.056,1.736),(s*.064,1.730)]:
        eyebrow.append((x,face_y(x,z)-.001,z))
    tube('Eyebrow',eyebrow,[.001,.002,.0022,.0015,.0004],hair,5,(.08,.087,.09),.28)

# Nariz projetado e narinas; perfil com ponte e ponta, nao outro ovo.
for s in (-1,1):
    ell('Nostril',(s*.010,face_y(s*.010,1.677)-.0004,1.677),(.0027,.0008,.0013),skin,8,4,(.057,.026,.018))

def surface_line(name,xzs,rads,mat,color,offset=.001):
    return tube(name,[(x,face_y(x,z)-offset,z) for x,z in xzs],rads,mat,6,color,.5)
surface_line('Upper_Lip',[(-.025,1.649),(-.012,1.652),(0,1.651),(.012,1.652),(.025,1.649)],[.0006,.0011,.001,.0011,.0006],skin,(.23,.128,.105))
surface_line('Mouth_Line',[(-.024,1.648),(-.012,1.649),(0,1.6485),(.012,1.649),(.024,1.648)],[.00035,.0005,.00055,.0005,.00035],skin,(.07,.033,.024),.0015)
surface_line('Lower_Lip',[(-.022,1.646),(0,1.645),(.022,1.646)],[.0005,.0013,.0005],skin,(.27,.152,.128),.0011)

# Barba curta como casca aderida a mandibula (espessura de 1.4 mm).
# Pigmentacao no proprio rosto evita uma segunda casca com intersecoes.
ca=head.data.color_attributes['Color']
for p in head.data.polygons:
    for li in p.loop_indices:
        co=head.data.vertices[head.data.loops[li].vertex_index].co
        x,y,z=co
        limit=1.634+.058*min(1.,abs(x)/.081)**1.65
        is_beard=y<.009 and z<limit and z>1.603
        is_moustache=1.656<z<1.666 and .003<abs(x)<.025 and y<-.068
        if is_beard or is_moustache:
            c=.062+random.random()*.018
            ca.data[li].color=(c*.94,c,c*1.05,1)
for s in (-1,1):
    surface_line('Moustache',[(s*.003,1.663),(s*.014,1.660),(s*.024,1.656)],[.0005,.0011,.0004],hair,(.058,.063,.068),.0009)
for k in range(110):
    a=random.uniform(-1.36,1.36)
    bottom=1.609+.013*abs(sin(a));top=1.631+.055*abs(sin(a))**1.65
    z=random.uniform(bottom,top)
    p=facepoint(z,a,.0010);q=facepoint(z+.003,a+.004,.0010)
    tube('Beard_Short_Fibre',[p,q],[.00023,.00015],hair,3,random.choice([(.12,.13,.14),(.17,.18,.19),(.07,.08,.085)]))

# Cicatriz discreta sobre o olho esquerdo do personagem.
surface_line('Face_Scar',[(.051,1.773),(.045,1.754),(.047,1.737)],[.00045,.0006,.0004],skin,(.39,.238,.193),.0005)
surface_line('Face_Scar_Lower',[(.045,1.705),(.049,1.694),(.056,1.68)],[.0004,.00055,.0003],skin,(.39,.238,.193),.0005)

# Cabelo penteado para tras: casca de couro cabeludo e mechas direcionais finas.
def hair_surface(u,t):
    u=max(-1,min(1,u))
    frontz=1.790+.014*u*u+.001*sin(67*u)
    x=.070*u*(1+.16*sin(pi*t)-.10*t*t)
    edge_y=face_y(.070*u,frontz)-.0008
    e=(1-cos(pi*t))/2
    y=edge_y*(1-e)+(.088*sqrt(1-.34*u*u))*e
    base=frontz*(1-t)+1.741*t
    z=base+.104*sin(pi*t)*sqrt(1-.73*u*u)
    ridge=.0010*(.5+.5*cos(pi*19*(u+1)))*max(0,sin(pi*t))**.5
    z+=ridge*sin(pi*t)
    y-=ridge*cos(pi*t)
    return Vector((x,y,z))

vs=[]
for i in range(17):
    for j in range(77):
        vs.append(hair_surface(-1+2*j/76,i/16))
fs=[(i*77+j,i*77+j+1,(i+1)*77+j+1,(i+1)*77+j) for i in range(16) for j in range(76)]
cap=mesh('Hair_Swept_Cap',vs,fs,hair,(.29,.325,.35))
for p in cap.data.polygons:
    for li in p.loop_indices:
        idx=cap.data.loops[li].vertex_index
        u=-1+2*(idx%77)/76
        c=.235+.10*(.5+.5*cos(pi*19*(u+1)))
        cap.data.color_attributes['Color'].data[li].color=(c,c+.035,c+.06,1)
for s in (-1,1):
    sideverts=[]
    for k in range(17):
        t=k/16
        upper=hair_surface(s,t)
        lower=Vector(facepoint(1.771-.031*t,s*(1.05+.8*t),.0012))
        sideverts += [upper,upper.lerp(lower,.5),lower]
    sidefaces=[(k*3+j,(k+1)*3+j,(k+1)*3+j+1,k*3+j+1) for k in range(16) for j in range(2)]
    mesh('Hair_Side_Cover',sideverts,sidefaces,hair,(.24,.275,.30))
for s in (-1,1):
    for i in range(6):
        x=s*(.073+i*.0018)
        pts=[(x,-.024,1.778+i*.003),(x*.99,.01,1.79),(x*.94,.061,1.764),(s*(.054+i*.003),.082,1.678),(s*(.065+i*.002),.052,1.615)]
        tube('Hair_Temple_Lock',pts,[.001,.0024,.0024,.002,.0004],hair,5,(.25+i*.02,.28+i*.02,.31+i*.02),.55)
tube('Hair_Tied_Tail',[(0,.081,1.792),(0,.112,1.739),(0,.114,1.68),(0,.095,1.62)],[.026,.027,.022,.006],hair,12,(.32,.36,.39),.67)
tube('Hair_Tie',[(0,.110,1.745),(0,.113,1.737)],[.028,.027],leather,12)

# Arnes ajustado ao peito; duas bainhas com empunhaduras acima do ombro.
ribbon('Harness_Diagonal_A',[(-.182,-.084,1.477),(-.119,-.142,1.416),(0,-.156,1.332),(.118,-.125,1.231),(.152,-.084,1.171)],.028,leather,(.073,.045,.028),.005)
ribbon('Harness_Diagonal_B',[(.172,-.082,1.478),(.104,-.147,1.421),(-.004,-.164,1.357),(-.115,-.136,1.277),(-.158,-.079,1.22)],.025,leather,(.066,.042,.028),.005)
buckle('Harness_Buckle_A',(-.093,-.155,1.402),.034,.029)
buckle('Harness_Buckle_B',(.092,-.159,1.415),.032,.026)
chain=[(-.05,-.073,1.56),(-.04,-.112,1.507),(0,-.15,1.451),(.04,-.112,1.507),(.05,-.073,1.56)]
tube('Pendant_Chain',chain,[.0018]*len(chain),metal,6)
# Medalhao angular de lobo: orelhas, focinho e olhos, criado do zero.
v=[(-.024,-.161,1.464),(-.019,-.161,1.487),(-.007,-.161,1.474),(0,-.173,1.48),(.007,-.161,1.474),(.019,-.161,1.487),(.024,-.161,1.464),(.014,-.178,1.45),(0,-.184,1.438),(-.014,-.178,1.45),(0,-.185,1.462)]
f=[(10,i,(i+1)%10) for i in range(10)]
mesh('Wolf_Pendant',v,f,metal,(.25,.28,.30),False)
for s in (-1,1):
    ell('Pendant_Eye',(s*.008,-.182,1.467),(.002,.001,.0015),skin,8,4,(.28,.018,.007))
for index,(start,end,tip) in enumerate([
    ((.115,.134,.80),(-.232,.141,1.655),(-.315,.148,1.863)),
    ((.055,.165,.84),(-.125,.171,1.721),(-.164,.177,1.919))]):
    a,b,c=Vector(start),Vector(end),Vector(tip)
    d=(c-b).normalized()
    tube('Sword_Scabbard',[a,a.lerp(b,.08),b],[.008,.021,.022],leather,10,(.034,.022,.016),.55)
    tube('Sword_Grip',[b,c],[.015,.012],leather,10,(.035,.025,.019))
    for k in range(7):
        p=b.lerp(c,.15+k*.105)
        tube('Sword_Grip_Wrap',[p-d*.002,p+d*.002],[.016,.016],metal,8,(.09,.10,.105))
    across=Vector((d.z,0,-d.x)).normalized()
    p=b+d*.015
    tube('Sword_Guard',[p-across*.076,p,p+across*.076],[.007,.01,.007],metal,8)
    ell('Sword_Pommel',c,(.023,.015,.023),metal,12,6)

# Orçamento dirigido: face e olhos preservados; simplificacao proporcional das
# pecas maiores e acessorios, antes da uniao, com normais suaves mantidas.
reducible=[o for o in parts if o.name.startswith(('Hair_Swept_Cap','Head_Facial_Planes'))]
protected=[o for o in parts if o not in reducible]
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
budget_remaining=14700-sum(tris(o) for o in protected)
ratio=min(1.,budget_remaining/sum(tris(o) for o in reducible))
print('PRE_BUDGET='+str(sum(tris(o) for o in parts))+'; surface_ratio='+str(ratio),flush=True)
if ratio<1:
    for o in reducible:
        if tris(o)<16:continue
        bpy.context.view_layer.objects.active=o
        dec=o.modifiers.new('Proxy_Budget','DECIMATE');dec.ratio=ratio
        bpy.ops.object.modifier_apply(modifier=dec.name)

# Uma malha de exportacao com grupos nomeados preserva a edicao por componente.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:
    vg=o.vertex_groups.new(name=o.name)
    vg.add(list(range(len(o.data.vertices))),1.,'REPLACE')
    o.select_set(True)
bpy.context.view_layer.objects.active=head
bpy.ops.object.join()
character=bpy.context.object
character.name='SM_PlayerWolf_Proxy_v02'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(character.data)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.0000001)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
bm.to_mesh(character.data);bm.free()
corners=[character.matrix_world@Vector(c) for c in character.bound_box]
low=min(p.z for p in corners); high=max(p.z for p in corners)
scale=1.85/(high-low)
for v in character.data.vertices:
    v.co.z-=low
    v.co*=scale
scene.cursor.location=(0,0,0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
character['asset_stage']='refined_pipeline_proxy'
character['reference']='External reference supplied by user; original geometry, no extracted data'
character['revision_notes']='Facial planes, eyes/nose/lips, close beard, swept hair; anatomical contours; articulated glove fingers.'
character['source_file']=str(SOURCE)
collection=bpy.data.collections.get('EXPORT_PlayerWolf_Proxy') or bpy.data.collections.new('EXPORT_PlayerWolf_Proxy')
if collection.name not in scene.collection.children:
    scene.collection.children.link(collection)
for c in list(character.users_collection):
    c.objects.unlink(character)
collection.objects.link(character)

# Budget: simplificacao limitada a acessorios repetidos nao deve apagar a face.
character.data.calc_loop_triangles()
triangles=len(character.data.loop_triangles)
materials=[s.material.name for s in character.material_slots]
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1.
floor=bpy.data.objects.get('PREVIEW_Floor')
if floor:
    floor.scale=(200,200,1)
    m=floor.data.materials[0]
    m.diffuse_color=(.017,.021,.026,1)
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.017,.021,.026,1)
for name,power,size in [('PREVIEW_Key',210,2.),('PREVIEW_Fill',100,2.),('PREVIEW_Rim',260,1.5)]:
    light=bpy.data.objects.get(name)
    light.data.energy=power;light.data.size=size
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.055,.067,.088,1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.3
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
scene.render.film_transparent=False
camera=scene.camera

def camera_view(position,target,lens=70,ortho=None):
    camera.location=position
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO' if ortho else 'PERSP'
    if ortho:camera.data.ortho_scale=ortho
    camera.data.lens=lens

camera_view((2.7,-5.6,2.5),(0,0,.96),85)
scene.render.resolution_x=1000;scene.render.resolution_y=1200
scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v02_full.png')
# Abre a revisao com o personagem enquadrado, materiais e overlays discretos.
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.shading.type='MATERIAL'
            space.overlay.show_overlays=False
            space.region_3d.view_distance=3.1
            space.region_3d.view_location=(0,0,.95)
            space.region_3d.view_rotation=Euler((78*pi/180,0,15*pi/180)).to_quaternion()
            space.region_3d.view_perspective='PERSP'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT))
bpy.ops.export_scene.fbx(filepath=str(FBX),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,mesh_smooth_type='FACE',colors_type='LINEAR')
report={'blender_version':bpy.app.version_string,'source':str(SOURCE),'blend':str(OUT),'fbx':str(FBX),'triangles':triangles,'materials':materials,'height_m':1.85,'vertex_groups':len(character.vertex_groups),'has_vertex_colors':bool(character.data.color_attributes),'budget_pass':triangles<=15000,'scale':list(character.scale),'rotation':list(character.rotation_euler),'limitations':['Proxy sem rig, UV, texturas ou animacao','Lateral e nuca inferidas da referencia frontal','Cores de vertice requerem shader compativel no Unity','Nao houve acesso a alteracoes nao salvas da sessao original']}
(ROOT/'validation_source.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf8')
print('REVISION_REPORT='+json.dumps(report),flush=True)
bpy.ops.render.render(write_still=True)
camera_view((.24,-1.1,1.78),(0,-.008,1.658),85,ortho=.37)
scene.render.resolution_x=1000;scene.render.resolution_y=1100
scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v02_face.png')
bpy.ops.render.render(write_still=True)
camera_view((3,-4,2.3),(0,0,.94),85,ortho=2.1)
scene.render.resolution_x=900;scene.render.resolution_y=1100
scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v02_threequarter.png')
bpy.ops.render.render(write_still=True)
camera_view((3.5,0,1.5),(0,0,.94),85,ortho=2.07)
scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v02_profile.png')
bpy.ops.render.render(write_still=True)
camera_view((2.6,4.5,2.0),(0,0,.94),85,ortho=2.07)
scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v02_back.png')
bpy.ops.render.render(write_still=True)
print('REVISION_RENDER_COMPLETE',flush=True)

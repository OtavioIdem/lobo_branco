"""Acabamento autoral v03 sobre a fonte v02 aprovada.

A malha de trabalho e os materiais procedurais destinam-se ao Blender.
O FBX geometrico nao incorpora a rede procedural; nao e um asset Unity final.
"""
from pathlib import Path
from collections import defaultdict, Counter
from math import sin,cos,pi,sqrt,exp,asin,copysign
import random,json,hashlib,os
import bpy,bmesh
from mathutils import Vector,Euler
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[1]
ROOT.mkdir(parents=True,exist_ok=True);(ROOT/'previews').mkdir(exist_ok=True)
SOURCE=Path(r'E:\Unity_Games\TW1-Remaster\art\source\blender\characters\player-wolf\SM_PlayerWolf_Proxy_v02.blend')
SOURCE_HASH=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene
original=bpy.data.objects.get('SM_PlayerWolf_Proxy_v02')
assert original is not None,'A fonte v02 nao contem o objeto esperado.'
random.seed(73)
MATS={};PARTS=[]
collection=bpy.data.collections.new('AUTHORING_PlayerWolf_v03')
scene.collection.children.link(collection)

def mat(name,color,roughness=.6,metallic=0.,kind='plain'):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    n=m.node_tree.nodes;l=m.node_tree.links;n.clear()
    out=n.new('ShaderNodeOutputMaterial');p=n.new('ShaderNodeBsdfPrincipled');l.new(p.outputs['BSDF'],out.inputs['Surface'])
    p.inputs['Roughness'].default_value=roughness;p.inputs['Metallic'].default_value=metallic
    vc=n.new('ShaderNodeVertexColor');vc.layer_name='Color'
    geo=n.new('ShaderNodeNewGeometry')
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value={'skin':1200,'leather':460,'cloth':1600,'metal':240,'hair':2100}.get(kind,700)
    noise.inputs['Detail'].default_value=2.4;noise.inputs['Roughness'].default_value=.7
    l.new(geo.outputs['Position'],noise.inputs['Vector'])
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.62,.62,.62,1);ramp.color_ramp.elements[1].color=(1,1,1,1)
    l.new(noise.outputs['Fac'],ramp.inputs[0])
    mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.5
    l.new(vc.outputs['Color'],mix.inputs[1]);l.new(ramp.outputs['Color'],mix.inputs[2]);l.new(mix.outputs[0],p.inputs['Base Color'])
    if kind!='plain':
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.24
        bump.inputs['Distance'].default_value={'skin':.00013,'leather':.00030,'cloth':.00024,'metal':.00011,'hair':.00008}.get(kind,.0001)
        l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs['Normal'],p.inputs['Normal'])
        if kind=='cloth':
            tex=n.new('ShaderNodeTexWave');tex.wave_type='BANDS';tex.bands_direction='DIAGONAL';tex.inputs['Scale'].default_value=950
            l.new(geo.outputs['Position'],tex.inputs['Vector'])
            weave=n.new('ShaderNodeMixRGB');weave.blend_type='MULTIPLY';weave.inputs[0].default_value=.6
            l.new(tex.outputs['Color'],weave.inputs[1]);l.new(noise.outputs['Fac'],weave.inputs[2]);l.new(weave.outputs[0],bump.inputs['Height'])
            p.inputs['Sheen Weight'].default_value=.18
        elif kind=='skin':
            p.inputs['Subsurface Weight'].default_value=.035
            p.inputs['Subsurface Radius'].default_value=(.8,.35,.18)
            p.inputs['Subsurface Scale'].default_value=.008
        elif kind=='leather':
            p.inputs['Specular IOR Level'].default_value=.28
        elif kind=='hair':
            p.inputs['Anisotropic'].default_value=.45
    MATS[name]=m
    return m

SKIN=mat('M_PlayerWolf_Skin',(.30,.195,.149),.55,kind='skin')
HAIR=mat('M_PlayerWolf_SilverHair',(.39,.43,.46),.48,kind='hair')
BEARD=mat('M_PlayerWolf_Stubble',(.11,.12,.12),.78,kind='hair')
EYE=mat('M_PlayerWolf_Eyes',(.4,.3,.1),.17)
LEATHER=mat('M_PlayerWolf_DarkLeather',(.031,.019,.013),.57,kind='leather')
STRAPS=mat('M_PlayerWolf_HarnessLeather',(.067,.031,.016),.50,kind='leather')
CLOTH=mat('M_PlayerWolf_WovenCloth',(.018,.023,.027),.82,kind='cloth')
LINEN=mat('M_PlayerWolf_Linen',(.15,.159,.143),.86,kind='cloth')
METAL=mat('M_PlayerWolf_Steel',(.13,.153,.17),.37,.8,'metal')
MAIL=mat('M_PlayerWolf_Chainmail',(.077,.095,.105),.48,.75,'metal')

def assign(o,m,color=None):
    o.data.materials.clear();o.data.materials.append(m)
    attr=o.data.color_attributes.get('Color') or o.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
    if color is not None:
        for a in attr.data:a.color=(*color[:3],1)
    for p in o.data.polygons:p.use_smooth=True
    return o

def mesh(name,vs,fs,m,color=None,scaled=False):
    data=bpy.data.meshes.new(name)
    data.from_pydata([Vector(v)*(1 if scaled else SCALE) for v in vs],[],fs);data.update()
    o=bpy.data.objects.new(name,data);collection.objects.link(o)
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    assign(o,m,color or m.diffuse_color[:3]);PARTS.append(o)
    return o

def smooth_curve(points,steps=30):
    pts=[Vector(p) for p in points];result=[]
    for k in range(steps):
        u=(len(pts)-1)*k/(steps-1);i=min(int(u),len(pts)-2);t=u-i
        p0=pts[max(0,i-1)];p1=pts[i];p2=pts[i+1];p3=pts[min(len(pts)-1,i+2)]
        result.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
    return result

class Tubes:
    def __init__(self):self.v=[];self.f=[];self.colors=[]
    def add(self,points,radii,n=5,flatten=1,color=(.1,.1,.1)):
        points=[Vector(p) for p in points];start=len(self.v)
        for i,p in enumerate(points):
            axis=(points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
            ref=Vector((0,1,0)) if abs(axis.y)<.95 else Vector((1,0,0))
            u=axis.cross(ref).normalized();v=axis.cross(u).normalized()
            for j in range(n):self.v.append(p+radii[i]*(u*cos(2*pi*j/n)+v*sin(2*pi*j/n)*flatten))
        for i in range(len(points)-1):
            for j in range(n):
                a=start+i*n+j;b=start+i*n+(j+1)%n
                self.f.append((a,b,b+n,a+n));self.colors.append(color)
        self.f += [tuple(start+j for j in reversed(range(n))),tuple(start+(len(points)-1)*n+j for j in range(n))]
        self.colors += [color,color]
    def build(self,name,m):
        o=mesh(name,self.v,self.f,m)
        for p,c in zip(o.data.polygons,self.colors):
            for li in p.loop_indices:o.data.color_attributes['Color'].data[li].color=(*c,1)
        return o

def tube(name,points,radii,m,n=8,flatten=1,color=None):
    t=Tubes();t.add(points,radii,n,flatten,color or m.diffuse_color[:3]);return t.build(name,m)

def ell(name,pos,radius,m,color=None,segments=32,rings=18):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=Vector(pos)*SCALE)
    o=bpy.context.object;o.name=name;o.scale=Vector(radius)*SCALE
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    collection.objects.link(o);assign(o,m,color or m.diffuse_color[:3]);PARTS.append(o);return o

# Mantem os componentes do corpo da v02, incluindo seus contornos e proporcoes.
groups={g.index:g.name for g in original.vertex_groups}
faces=defaultdict(list)
for p in original.data.polygons:
    membership=Counter(g.group for vi in p.vertices for g in original.data.vertices[vi].groups)
    if membership:faces[groups[membership.most_common(1)[0][0]]].append(p)
neckindices={i for p in faces['Neck_Anatomical'] for i in p.vertices}
SCALE=max(original.data.vertices[i].co.z for i in neckindices)/1.643
replace=('Head_','Eye_','Ear','Upper_Eyelid','Lower_Eyelid','Eyebrow','Nostril','Upper_Lip','Lower_Lip','Mouth_Line','Moustache','Beard_','Face_Scar','Hair_','Mail_Ring')
for name,polys in faces.items():
    if name.startswith(replace):continue
    indices=sorted({vi for p in polys for vi in p.vertices});mapping={v:i for i,v in enumerate(indices)}
    verts=[original.matrix_world@original.data.vertices[i].co for i in indices]
    f=[tuple(mapping[i] for i in p.vertices) for p in polys]
    oldmat=original.data.materials[polys[0].material_index].name
    chosen={'M_ProxySkin':SKIN,'M_ProxyCloth':CLOTH,'M_ProxyHair':HAIR,'M_ProxyLeather':LEATHER,'M_ProxyMetal':METAL}[oldmat]
    if name.startswith(('Harness','Belt','Boot_Strap','Pauldron_Leather_Rim','Bracer_Band','Elbow_Cuff')):chosen=STRAPS
    if name.startswith(('Forearm_Wrap','Sleeve_Seam')):chosen=LINEN
    if name.startswith(('Pauldron_Layer','Upper_Arm','Abdominal_Mail_Base')):chosen=LEATHER
    o=mesh(name,verts,f,chosen,scaled=True)
    # Cores locais preservadas no que distingue a construcao; tecidos e placas escurecidos.
    for newp,oldp in zip(o.data.polygons,polys):
        for nli,oli in zip(newp.loop_indices,oldp.loop_indices):
            c=original.data.color_attributes['Color'].data[oli].color[:3]
            if name.startswith(('Pauldron_Layer','Chest_Panel','Upper_Arm')):c=(.023,.027,.028)
            if name.startswith('Forearm_Wrap'):c=(.18,.185,.161)
            o.data.color_attributes['Color'].data[nli].color=(*c,1)
    if name.startswith(('Torso_','Pelvis_','Trousers_','Boot_Calf','Boot_Foot','Glove_','Forearm_Wrap','Upper_Arm','Elbow_Leather','Knee_Cap','Neck_')):
        bpy.context.view_layer.objects.active=o
        sub=o.modifiers.new('Surface_Finish','SUBSURF');sub.levels=2;sub.render_levels=2
        bpy.ops.object.modifier_apply(modifier=sub.name)
    elif name.startswith(('Chest_Panel','Front_Skirt','Back_Skirt','Pauldron_Layer')):
        bpy.context.view_layer.objects.active=o
        sub=o.modifiers.new('Panel_Surface','SUBSURF');sub.levels=2;sub.render_levels=2
        bpy.ops.object.modifier_apply(modifier=sub.name)
        solid=o.modifiers.new('Leather_Thickness','SOLIDIFY');solid.thickness=.003*SCALE
        bpy.ops.object.modifier_apply(modifier=solid.name)
bpy.data.objects.remove(original,do_unlink=True)
print('V02_BODY_PRESERVED; SCALE='+str(SCALE),flush=True)

# Rosto: malha continua de alta resolucao com nariz, sulcos e arcadas na superficie.
rows=[(1.602,.037,.071,.027),(1.612,.054,.080,.035),(1.626,.069,.084,.047),(1.646,.078,.077,.059),(1.666,.077,.074,.071),(1.686,.084,.076,.081),(1.710,.082,.073,.086),(1.734,.079,.074,.084),(1.758,.079,.070,.082),(1.781,.078,.065,.075),(1.803,.074,.056,.069),(1.826,.060,.041,.055),(1.846,.035,.022,.034),(1.852,.002,.002,.003)]
def profile(z):
    for k,(a,b) in enumerate(zip(rows,rows[1:])):
        if a[0]<=z<=b[0]:
            t=(z-a[0])/(b[0]-a[0]);p=rows[max(0,k-1)];q=rows[min(len(rows)-1,k+2)]
            return tuple(.5*(2*a[i]+(-p[i]+b[i])*t+(2*p[i]-5*a[i]+4*b[i]-q[i])*t*t+(-p[i]+3*a[i]-3*b[i]+q[i])*t*t*t) for i in range(1,4))
    return rows[0][1:] if z<rows[0][0] else rows[-1][1:]

def surface(z,a,offset=0):
    w,f,b=profile(z);sn=sin(a);cs=cos(a)
    power=.83 if z<1.65 else .96
    x=w*copysign(abs(sn)**power,sn);y=-cs*(f if cs>=0 else b)
    if cs>0:
        y-=.0085*exp(-((abs(x)-.055)/.018)**2-((z-1.690)/.016)**2)*cs
        y+=.006*exp(-((abs(x)-.053)/.020)**2-((z-1.664)/.014)**2)*cs
        y+=.003*exp(-((abs(x)-.034)/.021)**2-((z-1.717)/.010)**2)*cs
        y-=.007*exp(-(x/.029)**2-((z-1.650)/.019)**2)*cs
        y-=.012*exp(-((abs(x)-.032)/.026)**2-((z-(1.735+.04*abs(x)))/.007)**2)*cs
        y-=.025*exp(-(x/.0090)**2-((z-1.713)/.024)**2)*cs
        y-=.031*exp(-(x/.0123)**2-((z-1.686)/.010)**2)*cs
        y-=.010*exp(-((abs(x)-.012)/.0055)**2-((z-1.678)/.006)**2)*cs
        # Prega nasolabial e sulco mentolabial discretos.
        nasox=.019+(1.677-z)*.51
        y+=.0015*exp(-((abs(x)-nasox)/.0022)**2-((z-1.665)/.018)**2)*cs
        y+=.0012*exp(-(x/.019)**4-((z-1.637)/.0018)**2)*cs
        # Linhas de expressao esculpidas, sem tubos aparentes.
        for zz in (1.754,1.765,1.774):
            y+=.00055*exp(-((z-zz-.35*x*x)/.00065)**2)*exp(-(x/.057)**6)
        for xx in (-.006,.006):
            y+=.0008*exp(-((x-xx)/.0008)**2-((z-1.744)/.008)**2)
    return Vector((x+sn*offset,y-cs*offset,z))

def face_y(x,z):
    w,_,_=profile(z);power=.83 if z<1.65 else .96
    return surface(z,asin(copysign(min(.999,abs(x/w))**(1/power),x)))[1]

def smoothstep(a,b,v):
    t=max(0,min(1,(v-a)/(b-a)));return t*t*(3-2*t)

def beard_mask(x,y,z):
    if y>.012:return 0.
    border=1.637+.050*min(1,abs(x)/.082)**1.5
    border+=.0016*sin(x*730)+.001*sin(x*1420)
    m=1-smoothstep(border-.004,border+.003,z)
    must=smoothstep(1.653,1.657,z)*(1-smoothstep(1.666,1.671,z))*(1-smoothstep(.022,.029,abs(x)))
    return max(m,must*.88)*smoothstep(1.600,1.605,z)

hn=192;hz=145;vs=[]
for i in range(hz):
    z=1.602+.25*i/(hz-1)
    for j in range(hn):vs.append(surface(z,2*pi*j/hn))
fs=[]
for i in range(hz-1):
    for j in range(hn):
        inds=(i*hn+j,i*hn+(j+1)%hn,(i+1)*hn+(j+1)%hn,(i+1)*hn+j)
        c=sum((vs[k] for k in inds),Vector())/4
        # Orificios sob as palpebras; os globos oculares ficam realmente encaixados.
        aperture=any(((c.x-s*.034)/.0138)**2+((c.z-1.713)/.0041)**2<1 for s in (-1,1))
        if c.y<-.035 and aperture:continue
        fs.append(inds)
fs += [tuple(reversed(range(hn))),tuple((hz-1)*hn+j for j in range(hn))]
head=mesh('Head_Sculpt_Surface',vs,fs,SKIN)
for p in head.data.polygons:
    for li in p.loop_indices:
        x,y,z=head.data.vertices[head.data.loops[li].vertex_index].co/SCALE
        mask=beard_mask(x,y,z)
        skin=Vector((.315,.208,.166))
        flush=.025*exp(-((abs(x)-.054)/.022)**2-((z-1.687)/.028)**2)
        skin.x+=flush
        c=skin.lerp(Vector((.071,.077,.078)),mask*.60)
        c*=.975+.025*sin(x*570+z*630)
        head.data.color_attributes['Color'].data[li].color=(*c,1)

# Olhos inseridos na orbita e palpebras largas que se fundem com a superficie.
for s in (-1,1):
    cx=s*.034;cz=1.713;cy=face_y(cx,cz)+.0088;rad=.0130
    whiteverts=[(cx,face_y(cx,cz)-.0042,cz)]
    for r in (.25,.5,.75,1.):
        for j in range(64):
            a=2*pi*j/64;x=cx+.0155*r*cos(a);z=cz+.0049*r*sin(a)
            y=face_y(x,z)-.0042*(1-r*r)+.0007*r*r
            whiteverts.append((x,y,z))
    whitefaces=[(0,1+j,1+(j+1)%64) for j in range(64)]
    whitefaces += [(1+i*64+j,1+i*64+(j+1)%64,1+(i+1)*64+(j+1)%64,1+(i+1)*64+j) for i in range(3) for j in range(64)]
    mesh('Eye_Sclera',whiteverts,whitefaces,EYE,(.40,.385,.31))
    front=cy-rad
    ell('Eye_Amber_Iris',(cx,front-.00025,cz),(.00345,.00055,.00345),EYE,(.28,.157,.028),40,20)
    ell('Eye_Slit_Pupil',(cx,front-.00083,cz),(.00075,.00022,.0030),EYE,(.006,.004,.002),24,14)
    ir=Tubes()
    for k in range(40):
        a=2*pi*k/40;r1=.0013;r2=.0033
        pts=[(cx+r1*cos(a),front-.00072,cz+r1*sin(a)),(cx+r2*cos(a+.01),front-.00056,cz+r2*sin(a+.01))]
        ir.add(pts,[.00007,.000025],3,color=(.15+random.random()*.1,.077+random.random()*.05,.012))
    ir.build('Iris_Radial_Detail',EYE)
    ev=[]
    for k in range(5):
        t=k/4
        for j in range(48):
            a=2*pi*j/48
            x=cx+(.0142+t*.0060)*cos(a)
            z=cz+(.0038+t*.0049)*sin(a)+s*(x-cx)*.035
            dx=x-cx;dz=z-cz
            y=face_y(x,z)-.0010*(1-t)-.00015*t
            ev.append((x,y,z))
    ef=[(i*48+j,i*48+(j+1)%48,(i+1)*48+(j+1)%48,(i+1)*48+j) for i in range(4) for j in range(48)]
    mesh('Sculpted_Eyelids',ev,ef,SKIN,(.30,.187,.148))
    # Orelha com helix/antihelix, sem cilindro escuro separado.
    ear=ell('Ear_Anatomy',(s*.080,.004,1.702),(.0125,.019,.029),SKIN,(.305,.186,.145),32,24)
    helix=[]
    for k in range(31):
        a=-.4+1.85*pi*k/30
        helix.append((s*(.087+.002*sin(a)),.002-.013*cos(a),1.704+.024*sin(a)))
    tube('Ear_Helix',helix,[.0018]*31,SKIN,8,color=(.31,.196,.154))
    tube('Ear_Antihelix',smooth_curve([(s*.09,-.005,1.687),(s*.091,.001,1.706),(s*.09,.002,1.719)],18),[.0014]*18,SKIN,6,color=(.25,.139,.106))
    # Sobrancelha espessa, com fios individuais e arco mais fechado.
    bv=[]
    for row in range(3):
        for j in range(33):
            t=j/32;x=s*(.012+.052*t);z=1.732+.005*sin(t*pi)-.003*t
            z+=(row-1)*.0018*sin(pi*t)**.35
            bv.append((x,face_y(x,z)-.00045,z))
    bf=[(i*33+j,i*33+j+1,(i+1)*33+j+1,(i+1)*33+j) for i in range(2) for j in range(32)]
    mesh('Brow_Base',bv,bf,HAIR,(.090,.10,.105))
    brow=Tubes()
    for k in range(62):
        t=k/61;x=s*(.012+.052*t);z=1.732+.005*sin(t*pi)-.003*t
        y=face_y(x,z)-.0008
        p=Vector((x,y,z));q=p+Vector((s*.0022,-.00015,.0020*(1-t)-.0006))
        brow.add([p,q],[.00027,.00009],4,color=(.10+random.random()*.07,.112+random.random()*.07,.12+random.random()*.07))
    brow.build('Brow_Fibres',HAIR)
    ell('Nostril_Shadow',(s*.010,face_y(s*.010,1.677)-.00035,1.6768),(.0023,.0006,.0011),SKIN,(.053,.026,.019),20,12)

# Labios finos como faixas de superficie; linha da boca voltada levemente para baixo.
for upper in (True,False):
    lv=[]
    for row in range(5):
        t=row/4
        for j in range(41):
            u=-1+2*j/40;x=.025*u
            mid=1.6484-.0015*abs(u)**2
            width=(.0029 if upper else .0032)*(1-u*u)
            cupid= -.0008*exp(-(u/.16)**2) if upper else 0
            z=mid+(width*t+cupid*t)*(1 if upper else -1)
            y=face_y(x,z)-(.00035+.0010*sin(pi*t))*(1-u*u)
            lv.append((x,y,z))
    lf=[(i*41+j,i*41+j+1,(i+1)*41+j+1,(i+1)*41+j) for i in range(4) for j in range(40)]
    mesh('Upper_Lip_Surface' if upper else 'Lower_Lip_Surface',lv,lf,SKIN,(.255,.134,.108) if upper else (.284,.156,.133))
mouth=[(x,face_y(x,1.6484-.0015*(x/.025)**2)-.00065,1.6484-.0015*(x/.025)**2) for x in [-.025+i*.05/40 for i in range(41)]]
tube('Mouth_Crease',mouth,[.00022]*41,SKIN,5,color=(.084,.042,.029))

# Fios curtos aderidos a face. Distribuicao e cor variam sem formar um bloco opaco.
beard=Tubes();count=0
for k in range(6500):
    a=random.uniform(-1.53,1.53);z=random.uniform(1.605,1.699)
    p=surface(z,a,.00030)
    if random.random()>beard_mask(*p):continue
    count+=1
    length=random.uniform(.0008,.0023)
    q=surface(z-length,a+random.uniform(-.009,.009),.00048)
    c=random.choice([(.085,.094,.10),(.15,.17,.18),(.24,.265,.28),(.31,.34,.36)])
    beard.add([p,q],[random.uniform(.00006,.00011),.000025],3,color=c)
beard.build('Beard_Close_Stubble',BEARD)
must=Tubes()
for s in (-1,1):
    for k in range(110):
        x=s*random.uniform(.0025,.025);z=random.uniform(1.658,1.666)
        p=Vector((x,face_y(x,z)-.00035,z));q=Vector((x+s*.0014,face_y(x+s*.0014,z-.0014)-.0006,z-.0014))
        must.add([p,q],[.00009,.000025],3,color=(.13,.145,.153))
must.build('Moustache_Fibres',BEARD)

for name,xzs in [('Scar_Upper',[(.05,1.774),(.045,1.758),(.046,1.742),(.048,1.734)]),('Scar_Lower',[(.046,1.705),(.048,1.694),(.054,1.681)])]:
    pts=smooth_curve([(x,face_y(x,z)-.00025,z) for x,z in xzs],28)
    pts=[(p.x,face_y(p.x,p.z)-.00035,p.z) for p in pts]
    tube(name,pts,[.00038*(.4+.6*sin(pi*i/27)) for i in range(28)],SKIN,6,.35,(.375,.231,.189))

# Cabelo: couro cabeludo, massas penteadas e fios seguem o mesmo campo de superficie.
def hair_surface(u,t,offset=0):
    u=max(-.998,min(.998,u));frontz=1.792+.014*u*u+.001*sin(17*u)
    x=.070*u*(1+.14*sin(pi*t)-.10*t*t)
    edge=face_y(.070*u,frontz)-.0006;e=(1-cos(pi*t))/2
    y=edge*(1-e)+(.087*sqrt(1-.32*u*u))*e
    z=frontz*(1-t)+1.743*t+.102*sin(pi*t)*sqrt(1-.76*u*u)
    return Vector((x,y-offset*cos(pi*t),z+offset*sin(pi*t)))
vs=[hair_surface(-1+2*j/64,i/32) for i in range(33) for j in range(65)]
fs=[(i*65+j,i*65+j+1,(i+1)*65+j+1,(i+1)*65+j) for i in range(32) for j in range(64)]
mesh('Hair_Scalp_Surface',vs,fs,HAIR,(.26,.295,.32))
locks=Tubes();strands=Tubes()
for k in range(34):
    u=-.98+1.96*k/33
    pts=[hair_surface(u+.012*sin(t*pi*1.7+k)*sin(t*pi),t,.0012) for t in [i/32 for i in range(33)]]
    r=[.0011+.0010*sin(pi*i/32) for i in range(33)]
    c=.30+random.random()*.07
    locks.add(pts,r,8,.53,(c,c+.036,c+.06))
for k in range(250):
    u=random.uniform(-.995,.995);phase=random.random()*6.2
    pts=[hair_surface(u+.008*sin(t*pi*2+phase)*sin(t*pi),t,.0017+random.random()*.00015) for t in [i/24 for i in range(25)]]
    c=.30+random.random()*.20
    strands.add(pts,[.00015*(.4+.6*sin(pi*i/24)**.35) for i in range(25)],4,.8,(c,c+.035,c+.055))
locks.build('Hair_Swept_Locks',HAIR);strands.build('Hair_Fine_Strands',HAIR)
for s in (-1,1):
    sv=[]
    for k in range(33):
        t=k/32;top=hair_surface(s,t);bottom=surface(1.779-.034*t,s*(1.06+.78*t),.001)
        for j in range(5):sv.append(top.lerp(bottom,j/4))
    sf=[(i*5+j,(i+1)*5+j,(i+1)*5+j+1,i*5+j+1) for i in range(32) for j in range(4)]
    mesh('Hair_Temple_Cover',sv,sf,HAIR,(.27,.304,.33))
    side=Tubes()
    for k in range(36):
        q=k/35
        ctrl=[(s*(.073+.006*q),-.023+q*.016,1.780+q*.017),(s*.083,.028+q*.01,1.754),(s*(.072+.006*q),.067,1.691),(s*(.060+.012*q),.045+q*.018,1.610+q*.026)]
        pts=smooth_curve(ctrl,30);r=[(.0015+.0007*sin(pi*i/29))*(1-.8*(i/29)**4) for i in range(30)]
        c=.25+random.random()*.16;side.add(pts,r,5,.62,(c,c+.035,c+.055))
    side.build('Hair_Long_Side_Locks',HAIR)
tailpath=smooth_curve([(0,.085,1.79),(0,.106,1.746),(0,.113,1.69),(0,.084,1.615)],38)
tube('Hair_Tail',tailpath,[.024*(1-.88*(i/37)**2) for i in range(38)],HAIR,24,.70,(.26,.29,.32))
tailstrands=Tubes()
for k in range(70):
    a=k*2*pi/70;pts=[]
    for i,p in enumerate(tailpath):
        r=.0245*(1-.88*(i/37)**2);pts.append(p+Vector((r*cos(a),r*.73*sin(a),0)))
    c=.28+random.random()*.14;tailstrands.add(pts,[.00016]*38,4,color=(c,c+.035,c+.055))
tailstrands.build('Hair_Tail_Strands',HAIR)
tube('Hair_Leather_Tie',[(0,.106,1.753),(0,.108,1.744)],[.025,.0245],STRAPS,24,.73)

# Aneis com volume e orientacao de superficie na cota de malha.
mail=Tubes()
body_bvhs={o.name:BVHTree.FromObject(o,bpy.context.evaluated_depsgraph_get()) for o in PARTS if o.name.startswith(('Pauldron_Layer','Upper_Arm','Abdominal_Mail_Base'))}
def ring(center,u,v,rx=.0035,rz=.0045,target=None):
    c=Vector(center);u=Vector(u).normalized();v=Vector(v).normalized();normal=u.cross(v).normalized()
    if target:
        bvh=body_bvhs[target]
        nearest,norm,_,_=bvh.find_nearest(c*SCALE)
        if nearest is not None:
            if target.startswith('Abdominal') and norm.y>0:norm=-norm
            elif target.startswith('Pauldron') and norm.z<0:norm=-norm
            elif target.startswith('Upper_Arm') and norm.dot(normal)<0:norm=-norm
            c=nearest/SCALE+norm*.00145
            normal=norm
            u=(u-normal*u.dot(normal)).normalized()
            v=normal.cross(u).normalized()
    p=[c+u*(rx*cos(2*pi*i/12))+v*(rz*sin(2*pi*i/12))+normal*.00065*sin(2*pi*i/12) for i in range(13)]
    shade=random.uniform(.070,.115);mail.add(p,[.00067]*13,4,color=(shade,shade*1.16,shade*1.25))
for row in range(21):
    z=1.050+row*.0083
    w=.15+(z-1.05)*.17
    for col in range(29):
        x=(col-14)*.0071+(row%2)*.0035
        y=-.124-.035*(z-1.05)+.047*(x/.18)**2
        ring((x,y,z),(1,.094*x/.18**2,0),(0,-.035,1),target='Abdominal_Mail_Base')
for s in (-1,1):
    for row in range(10):
        r=.31+row*.070
        for col in range(34):
            a=-pi/2+2*pi*col/34
            x=s*.251+.104*r*sin(a);y=-.004-.102*r*cos(a)
            z=1.548-.072*r*r-.027*max(0,s*sin(a))*r
            u=Vector((.104*r*cos(a),.102*r*sin(a),-.027*s*cos(a)*r if s*sin(a)>0 else 0))
            v=Vector((.104*sin(a),-.102*cos(a),-.144*r-.027*max(0,s*sin(a))))
            ring((x,y,z+.001),u,v,.0031,.0041,target=f'Pauldron_Layer_{s}')
    a=Vector((s*.232,0,1.463));b=Vector((s*.393,-.003,1.239));axis=(b-a).normalized()
    u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u).normalized()
    for row in range(13):
        t=.22+row*.048;c=a.lerp(b,t);r=.087-.022*t
        for col in range(13):
            phi=-.20+pi*col/12
            radial=u*cos(phi)+v*sin(phi)
            p=c+radial*r
            # A metade frontal recebe os aneis; a costura fica visivel atras.
            if p.y>.022:continue
            ring(p,(-u*sin(phi)+v*cos(phi)),axis,.0032,.0040,target=f'Upper_Arm_{s}')
mail.build('Armour_Chainmail_Rings',MAIL)

# Costuras, pesponto e rebites pequenos nos contornos de couro ja existentes.
stitches=Tubes()
for s in (-1,1):
    for j in range(34):
        z=.75+j*.0065;x=s*.028;y=-.114 if z<.85 else -.126
        stitches.add([(x-.001,y-.001,z),(x+.001,y-.001,z+.0024)],[.00030,.00030],4,color=(.13,.096,.059))
    for j in range(25):
        t=j/24;a=-1.5+3*t
        p=Vector((s*.251+.111*sin(a),-.004-.109*cos(a),1.460-.026*max(0,s*sin(a))))
        q=p+Vector((.0017,0,.0017))
        stitches.add([p,q],[.00027,.00027],4,color=(.16,.117,.073))
stitches.build('Leather_Stitches',STRAPS)

# Organizacao e entrega: meshes separados e nomeados mantem a revisao editavel.
for o in PARTS:
    if o.type=='MESH':
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00000008)
        bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.00000001)
        bm.to_mesh(o.data);bm.free()
    o.name='SM_Wolf_v03_'+o.name
root=bpy.data.objects.new('PlayerWolf_v03_ROOT',None);collection.objects.link(root)
for o in PARTS:o.parent=root
root['source_v02_sha256']=SOURCE_HASH
root['stage']='authoring_detail_study'
root['material_pipeline']='Blender procedural; baking and Unity shader integration pending'
root['reference']='User supplied frontal reference; original modeled geometry; no game assets extracted'
bpy.ops.object.select_all(action='DESELECT')
for o in PARTS:o.select_set(True)
bpy.context.view_layer.objects.active=head

# Luz de estudio mais rasante para tornar relevo e materiais verificaveis.
floor=bpy.data.objects.get('PREVIEW_Floor')
if floor:
    floor.hide_set(False);floor.hide_render=False
for name,energy,size,pos in [('PREVIEW_Key',330,1.6,(-1.9,-2.7,3.2)),('PREVIEW_Fill',125,2.0,(2.7,-2.4,2.1)),('PREVIEW_Rim',410,1.2,(1.2,2.1,3.0))]:
    o=bpy.data.objects.get(name);o.location=pos;o.data.energy=energy;o.data.size=size
    o.rotation_euler=(Vector((0,0,1.2))-o.location).to_track_quat('-Z','Y').to_euler()
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.27
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
camera=scene.camera
def view(pos,target,ortho=None,lens=85):
    camera.location=pos;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO' if ortho else 'PERSP';camera.data.lens=lens
    if ortho:camera.data.ortho_scale=ortho
view((2.7,-5.6,2.5),(0,0,.96))
scene.render.resolution_x=1100;scene.render.resolution_y=1350
scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v03_full.png')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            sp=area.spaces.active;sp.shading.type='MATERIAL';sp.overlay.show_overlays=False
            sp.region_3d.view_distance=3.1;sp.region_3d.view_location=(0,0,.95)
            sp.region_3d.view_rotation=Euler((78*pi/180,0,15*pi/180)).to_quaternion()
            sp.region_3d.view_perspective='PERSP'
triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in PARTS)
coords=[o.matrix_world@v.co for o in PARTS for v in o.data.vertices]
height=max(v.z for v in coords)-min(v.z for v in coords)
report={'version':bpy.app.version_string,'source':str(SOURCE),'source_sha256':SOURCE_HASH,'meshes':len(PARTS),'triangles':triangles,'height_m':height,'floor_z':min(v.z for v in coords),'materials':list(MATS),'stage':'authoring_detail_study','procedural_materials':True,'uv_baked':False,'rigged':False,'unity_tested':False,'export_note':'Geometria detalhada e cores de vertice. Materiais procedurais exigem bake ou shaders equivalentes.'}
(ROOT/'validation_v03_source.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf8')
print('V03_REPORT='+json.dumps(report),flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SM_PlayerWolf_Refined_v03.blend'))
bpy.ops.export_scene.fbx(filepath=str(ROOT/'SM_PlayerWolf_Refined_v03.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,mesh_smooth_type='FACE',colors_type='LINEAR')
if os.environ.get('WOLF_SKIP_RENDER')!='1':
    bpy.ops.render.render(write_still=True)
    view((.20,-1.1,1.78),(0,-.008,1.658),.35)
    scene.render.resolution_x=1150;scene.render.resolution_y=1250
    scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v03_face.png');bpy.ops.render.render(write_still=True)
    if os.environ.get('WOLF_FASTPREVIEW')=='1':
        print('V03_RENDER_COMPLETE: full and face',flush=True)
        raise SystemExit(0)
    view((1.4,-3,1.7),(0,0,1.25),.96)
    scene.render.resolution_x=1150;scene.render.resolution_y=1150
    scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v03_armour.png');bpy.ops.render.render(write_still=True)
    view((2.7,4.6,2.2),(0,0,.95),2.06)
    scene.render.resolution_x=950;scene.render.resolution_y=1200
    scene.render.filepath=str(ROOT/'previews'/'PlayerWolf_v03_back.png');bpy.ops.render.render(write_still=True)
    print('V03_RENDER_COMPLETE',flush=True)

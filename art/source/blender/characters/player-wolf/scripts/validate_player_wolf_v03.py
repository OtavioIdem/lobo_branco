"""Valida a revisao de acabamento e o FBX, sem alterar a cena interativa."""
from pathlib import Path
from math import isfinite
import json,hashlib
import bpy

ROOT=Path(__file__).resolve().parents[1]
def inspect(meshes):
    points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
    assert points and all(all(isfinite(c) for c in p) for p in points),'Coordenadas invalidas'
    tris=0;degenerate=0
    for o in meshes:
        o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
        for tri in o.data.loop_triangles:
            a,b,c=(o.data.vertices[i].co for i in tri.vertices)
            degenerate += (b-a).cross(c-a).length<1e-13
    return {'meshes':len(meshes),'triangles':tris,'height_m':max(p.z for p in points)-min(p.z for p in points),'floor_z':min(p.z for p in points),'materials':sorted({s.material.name for o in meshes for s in o.material_slots if s.material}),'color_layers':all(bool(o.data.color_attributes) for o in meshes),'degenerate_triangles':degenerate}
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SM_PlayerWolf_Refined_v03.blend'))
collection=bpy.data.collections['AUTHORING_PlayerWolf_v03']
root=bpy.data.objects['PlayerWolf_v03_ROOT']
assert tuple(root.location)==(0.,0.,0.)
assert tuple(root.scale)==(1.,1.,1.)
source=inspect([o for o in collection.objects if o.type=='MESH'])
assert all(o.parent==root for o in collection.objects if o.type=='MESH')
source_path=Path(r'E:\Unity_Games\TW1-Remaster\art\source\blender\characters\player-wolf\SM_PlayerWolf_Proxy_v02.blend')
assert hashlib.sha256(source_path.read_bytes()).hexdigest()==root['source_v02_sha256'],'A v02 mudou desde o inicio da revisao'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'SM_PlayerWolf_Refined_v03.fbx'),automatic_bone_orientation=False)
export=inspect([o for o in bpy.context.scene.objects if o.type=='MESH'])
for label,data in [('blend',source),('fbx',export)]:
    assert abs(data['height_m']-1.85)<.0001,(label,'height',data['height_m'])
    assert abs(data['floor_z'])<.0001,(label,'floor',data['floor_z'])
    assert data['triangles']<1000000,(label,'working_mesh_sanity')
    assert len(data['materials'])==10,(label,'materials')
    assert data['color_layers'],(label,'vertex_colors')
    assert data['degenerate_triangles']==0,(label,'degenerate',data['degenerate_triangles'])
assert source['meshes']==export['meshes'],'Numero de componentes mudou'
assert source['triangles']==export['triangles'],'Numero de triangulos mudou'
assert source['materials']==export['materials'],'Materiais mudaram'
result={'result':'PASS','blender_version':bpy.app.version_string,'source':source,'fbx_roundtrip':export,'source_v02_unchanged':True,'stage':'High resolution authoring study; not optimized for Unity','unity_tested':False,'procedural_shaders_baked':False}
(ROOT/'validation_v03_roundtrip.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf8')
print('V03_VALIDATION='+json.dumps(result),flush=True)

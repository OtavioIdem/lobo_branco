"""Verifica a fonte e o round-trip real do FBX em Blender separado."""
from pathlib import Path
from math import isfinite
import json
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]

def metrics(objects):
    coordinates=[o.matrix_world @ v.co for o in objects for v in o.data.vertices]
    for co in coordinates:
        assert all(isfinite(v) for v in co),'Coordenada nao finita'
    lo=min(v.z for v in coordinates);hi=max(v.z for v in coordinates)
    triangles=0;degenerate=0
    for o in objects:
        o.data.calc_loop_triangles()
        triangles+=len(o.data.loop_triangles)
        for t in o.data.loop_triangles:
            a,b,c=(o.data.vertices[i].co for i in t.vertices)
            if (b-a).cross(c-a).length<1e-13:degenerate+=1
    return {'meshes':len(objects),'height_m':hi-lo,'floor_z':lo,'triangles':triangles,
        'materials':sorted({s.material.name for o in objects for s in o.material_slots if s.material}),
        'vertex_color_layers':[len(o.data.color_attributes) for o in objects],
        'degenerate_triangles':degenerate,
        'smooth_faces':sum(p.use_smooth for o in objects for p in o.data.polygons)}

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SM_PlayerWolf_Proxy_v02.blend'))
o=bpy.data.objects['SM_PlayerWolf_Proxy_v02']
source=metrics([o])
assert tuple(o.location)==(0,0,0),'Pivo fora da origem'
assert tuple(o.scale)==(1,1,1),'Escala nao aplicada'
assert tuple(o.rotation_euler)==(0,0,0),'Rotacao nao aplicada'
assert len(o.vertex_groups)>20,'Componentes de edicao perdidos'
assert o in list(bpy.data.collections['EXPORT_PlayerWolf_Proxy'].objects)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'SM_PlayerWolf_Proxy_v02.fbx'),automatic_bone_orientation=False)
imported=metrics([o for o in bpy.context.scene.objects if o.type=='MESH'])
for label,m in [('source',source),('fbx',imported)]:
    assert abs(m['height_m']-1.85)<.0001,(label,'Altura',m['height_m'])
    assert abs(m['floor_z'])<.0001,(label,'Base',m['floor_z'])
    assert m['triangles']<=15000,(label,'Orcamento',m['triangles'])
    assert len(m['materials'])==5,(label,'Materiais')
    assert all(m['vertex_color_layers']),(label,'Cores ausentes')
    assert m['degenerate_triangles']==0,(label,'Faces degeneradas',m['degenerate_triangles'])
assert source['triangles']==imported['triangles'],'Triangulos mudaram no FBX'
assert source['materials']==imported['materials'],'Slots mudaram no FBX'
result={'result':'PASS','blender_version':bpy.app.version_string,'source':source,'fbx_roundtrip':imported,'unity_validation':'Nao executada; revisao de modelagem no Blender.'}
(ROOT/'validation_roundtrip.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf8')
print('VALIDATION='+json.dumps(result),flush=True)

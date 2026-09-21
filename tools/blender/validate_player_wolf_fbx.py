"""Valida o round-trip do FBX do proxy PlayerWolf em uma cena Blender limpa."""

from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
FBX_PATH = PROJECT_ROOT / "unity/LoboBranco/Assets/_Project/Art/Characters/PlayerWolf/SM_PlayerWolf_Proxy_v01.fbx"

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(FBX_PATH), automatic_bone_orientation=False)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not meshes:
    raise RuntimeError("Nenhuma malha foi importada do FBX")

corners = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
min_z = min(point.z for point in corners)
max_z = max(point.z for point in corners)
height = max_z - min_z
triangles = sum(len(poly.vertices) - 2 for obj in meshes for poly in obj.data.polygons)
materials = sorted({slot.material.name for obj in meshes for slot in obj.material_slots if slot.material})

if abs(height - 1.85) > 0.0185:
    raise RuntimeError(f"Altura fora da tolerancia de 1%: {height:.6f} m")
if abs(min_z) > 0.01:
    raise RuntimeError(f"Base do modelo fora do chao: min_z={min_z:.6f}")
if triangles > 15000:
    raise RuntimeError(f"Orcamento excedido: {triangles} triangulos")
if len(materials) > 5:
    raise RuntimeError(f"Slots de material excedidos: {len(materials)}")

print(f"FBX_VALIDATION_FILE={FBX_PATH}")
print(f"FBX_VALIDATION_MESHES={len(meshes)}")
print(f"FBX_VALIDATION_HEIGHT={height:.6f}")
print(f"FBX_VALIDATION_MIN_Z={min_z:.6f}")
print(f"FBX_VALIDATION_TRIANGLES={triangles}")
print(f"FBX_VALIDATION_MATERIALS={','.join(materials)}")
print("FBX_VALIDATION_RESULT=PASS")

"""Cria o blockout fan-made do personagem Lobo e exporta o proxy para Unity."""

from pathlib import Path
from math import radians

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
BLEND_PATH = PROJECT_ROOT / "art/source/blender/characters/player-wolf/SM_PlayerWolf_Proxy_v01.blend"
FBX_PATH = PROJECT_ROOT / "unity/LoboBranco/Assets/_Project/Art/Characters/PlayerWolf/SM_PlayerWolf_Proxy_v01.fbx"
PREVIEW_PATH = PROJECT_ROOT / "art/concept/characters/PlayerWolf_Geralt_Blockout_v01.png"


def material(name, color, metallic=0.0, roughness=0.65):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def assign(obj, mat):
    obj.data.materials.append(mat)
    return obj


def bevel(obj, width=0.012, segments=2):
    modifier = obj.modifiers.new("ProxyBevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def add_box(name, location, half_extents, mat, rotation=(0.0, 0.0, 0.0), bevel_width=0.01):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = half_extents
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel_width:
        bevel(obj, bevel_width)
    return assign(obj, mat)


def add_ellipsoid(name, location, radii, mat, segments=20, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = radii
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return assign(obj, mat)


def add_tapered(name, start, end, radius_start, radius_end, mat, vertices=12):
    start_v = Vector(start)
    end_v = Vector(end)
    direction = end_v - start_v
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_start,
        radius2=radius_end,
        depth=direction.length,
        location=(start_v + end_v) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0.0, 0.0, 1.0)).rotation_difference(direction.normalized())
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return assign(obj, mat)


def add_strap(name, x, y, z, angle, mat):
    return add_box(
        name,
        (x, y, z),
        (0.025, 0.014, 0.38),
        mat,
        rotation=(0.0, radians(angle), 0.0),
        bevel_width=0.006,
    )


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


clear_scene()
bpy.context.scene.unit_settings.system = "METRIC"
bpy.context.scene.unit_settings.scale_length = 1.0

skin = material("M_ProxySkin", (0.36, 0.20, 0.15), roughness=0.78)
hair = material("M_ProxyHair", (0.68, 0.72, 0.76), metallic=0.0, roughness=0.72)
cloth = material("M_ProxyCloth", (0.035, 0.042, 0.048), roughness=0.92)
leather = material("M_ProxyLeather", (0.11, 0.035, 0.025), roughness=0.68)
metal = material("M_ProxyMetal", (0.12, 0.14, 0.16), metallic=0.78, roughness=0.38)

character_parts = []

# Pés, botas e pernas.
for side in (-1.0, 1.0):
    x = side * 0.105
    character_parts.append(add_box(f"Boot_{side:+.0f}", (x, -0.045, 0.10), (0.09, 0.16, 0.10), leather, bevel_width=0.025))
    character_parts.append(add_tapered(f"LowerLeg_{side:+.0f}", (x, 0.0, 0.18), (x, 0.0, 0.52), 0.09, 0.105, leather))
    character_parts.append(add_ellipsoid(f"Knee_{side:+.0f}", (x, 0.0, 0.57), (0.105, 0.105, 0.11), cloth, 16, 8))
    character_parts.append(add_tapered(f"UpperLeg_{side:+.0f}", (x, 0.0, 0.61), (side * 0.09, 0.0, 0.96), 0.115, 0.135, cloth))
    character_parts.append(add_box(f"ThighStrap_{side:+.0f}", (x, -0.105, 0.78), (0.12, 0.018, 0.025), leather, bevel_width=0.006))

# Pelve, faldões, tronco e cota de malha.
character_parts.append(add_ellipsoid("Pelvis", (0.0, 0.0, 1.00), (0.23, 0.16, 0.19), cloth))
character_parts.append(add_box("FrontSkirt", (0.0, -0.115, 0.89), (0.22, 0.035, 0.23), cloth, bevel_width=0.025))
character_parts.append(add_box("BackSkirt", (0.0, 0.115, 0.91), (0.23, 0.035, 0.22), cloth, bevel_width=0.025))
character_parts.append(add_box("Belt", (0.0, -0.005, 1.08), (0.25, 0.18, 0.035), leather, bevel_width=0.012))
character_parts.append(add_ellipsoid("MailTorso", (0.0, 0.0, 1.34), (0.29, 0.18, 0.34), metal))
character_parts.append(add_box("ChestArmor", (0.0, -0.155, 1.39), (0.265, 0.045, 0.245), cloth, bevel_width=0.035))
character_parts.append(add_box("AbdomenArmor", (0.0, -0.16, 1.18), (0.22, 0.04, 0.105), cloth, bevel_width=0.025))

# Ombros e braços em A-pose.
arm_points = {
    -1.0: ((-0.25, 0.0, 1.55), (-0.50, 0.0, 1.40), (-0.70, -0.01, 1.20), (-0.76, -0.025, 1.10)),
    1.0: ((0.25, 0.0, 1.55), (0.50, 0.0, 1.40), (0.70, -0.01, 1.20), (0.76, -0.025, 1.10)),
}
for side, (shoulder, elbow, wrist, hand) in arm_points.items():
    character_parts.append(add_ellipsoid(f"Pauldron_{side:+.0f}", shoulder, (0.18, 0.18, 0.13), metal, 16, 8))
    character_parts.append(add_tapered(f"UpperArmMail_{side:+.0f}", shoulder, elbow, 0.115, 0.10, metal))
    character_parts.append(add_ellipsoid(f"Elbow_{side:+.0f}", elbow, (0.105, 0.105, 0.10), leather, 16, 8))
    character_parts.append(add_tapered(f"Forearm_{side:+.0f}", elbow, wrist, 0.10, 0.075, leather))
    character_parts.append(add_ellipsoid(f"Hand_{side:+.0f}", hand, (0.07, 0.055, 0.105), leather, 16, 8))

# Pescoço, cabeça, cabelo branco e barba.
character_parts.append(add_tapered("Neck", (0.0, 0.0, 1.57), (0.0, 0.0, 1.67), 0.085, 0.09, skin, 14))
character_parts.append(add_ellipsoid("Head", (0.0, -0.005, 1.75), (0.115, 0.105, 0.145), skin, 24, 14))
character_parts.append(add_ellipsoid("HairCap", (0.0, 0.015, 1.825), (0.122, 0.112, 0.085), hair, 20, 10))
character_parts.append(add_ellipsoid("SweptHair", (0.0, 0.075, 1.78), (0.105, 0.075, 0.105), hair, 18, 10))
character_parts.append(add_tapered("HairTie", (0.0, 0.105, 1.79), (0.0, 0.18, 1.70), 0.045, 0.025, hair, 10))
character_parts.append(add_ellipsoid("Beard", (0.0, -0.095, 1.70), (0.095, 0.035, 0.075), hair, 18, 10))

# Arnês cruzado e medalhão simplificado.
character_parts.append(add_strap("Harness_L", -0.09, -0.205, 1.39, -24.0, leather))
character_parts.append(add_strap("Harness_R", 0.09, -0.205, 1.39, 24.0, leather))
bpy.ops.mesh.primitive_torus_add(major_radius=0.035, minor_radius=0.009, major_segments=16, minor_segments=6, location=(0.0, -0.222, 1.48), rotation=(radians(90), 0.0, 0.0))
medallion = bpy.context.object
medallion.name = "MedallionProxy"
character_parts.append(assign(medallion, metal))

# Duas bainhas e cabos cruzados nas costas.
for index, side in enumerate((-1.0, 1.0), start=1):
    start = (side * 0.17, 0.18, 0.78)
    end = (-side * 0.18, 0.20, 1.78)
    hilt_end = (-side * 0.25, 0.21, 1.98)
    character_parts.append(add_tapered(f"Scabbard_{index}", start, end, 0.035, 0.045, leather, 12))
    character_parts.append(add_tapered(f"SwordGrip_{index}", end, hilt_end, 0.028, 0.024, leather, 10))
    guard_center = Vector(end) + (Vector(hilt_end) - Vector(end)).normalized() * 0.025
    character_parts.append(add_box(f"SwordGuard_{index}", guard_center, (0.11, 0.018, 0.015), metal, rotation=(0.0, radians(side * 19.0), 0.0), bevel_width=0.004))

# Une toda a geometria do proxy em um único asset, preservando slots de material.
bpy.ops.object.select_all(action="DESELECT")
for obj in character_parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = character_parts[0]
bpy.ops.object.join()
character = bpy.context.object
character.name = "SM_PlayerWolf_Proxy_v01"

# Normaliza o resultado para exatamente 1,85 m e coloca o pivô no chão entre os pés.
world_corners = [character.matrix_world @ Vector(corner) for corner in character.bound_box]
min_z = min(point.z for point in world_corners)
max_z = max(point.z for point in world_corners)
uniform_scale = 1.85 / (max_z - min_z)
character.scale = (uniform_scale, uniform_scale, uniform_scale)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
world_corners = [character.matrix_world @ Vector(corner) for corner in character.bound_box]
character.location.z -= min(point.z for point in world_corners)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
character["asset_stage"] = "pipeline_proxy"
character["target_height_m"] = 1.85
character["source_reference"] = "External user-supplied visual reference; no extracted asset data"

for path in (BLEND_PATH, FBX_PATH, PREVIEW_PATH):
    path.parent.mkdir(parents=True, exist_ok=True)

# Exporta apenas o personagem antes de adicionar objetos de preview.
bpy.ops.object.select_all(action="DESELECT")
character.select_set(True)
bpy.context.view_layer.objects.active = character
bpy.ops.export_scene.fbx(
    filepath=str(FBX_PATH),
    use_selection=True,
    object_types={"MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z",
    axis_up="Y",
    bake_anim=False,
    add_leaf_bones=False,
    mesh_smooth_type="FACE",
)

# Cena de preview, fora da seleção exportada.
preview_mat = material("M_PreviewFloor", (0.055, 0.06, 0.065), roughness=0.95)
floor = add_box("PREVIEW_Floor", (0.0, 0.0, -0.035), (1.6, 1.3, 0.035), preview_mat, bevel_width=0.0)

bpy.ops.object.camera_add(location=(3.15, -5.15, 2.45))
camera = bpy.context.object
camera.name = "PREVIEW_Camera"
camera.data.lens = 72
look_at(camera, (0.0, 0.0, 0.95))
bpy.context.scene.camera = camera

bpy.ops.object.light_add(type="AREA", location=(-2.2, -3.0, 4.0))
key = bpy.context.object
key.name = "PREVIEW_Key"
key.data.energy = 1050
key.data.shape = "DISK"
key.data.size = 4.0
look_at(key, (0.0, 0.0, 1.0))

bpy.ops.object.light_add(type="AREA", location=(2.7, -1.0, 2.4))
fill = bpy.context.object
fill.name = "PREVIEW_Fill"
fill.data.energy = 650
fill.data.size = 3.0
look_at(fill, (0.0, 0.0, 1.0))

bpy.ops.object.light_add(type="AREA", location=(0.0, 2.5, 3.0))
rim = bpy.context.object
rim.name = "PREVIEW_Rim"
rim.data.energy = 900
rim.data.size = 2.5
look_at(rim, (0.0, 0.0, 1.25))

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(PREVIEW_PATH)
scene.render.film_transparent = False
scene.world.color = (0.018, 0.022, 0.028)
scene.view_settings.look = "AgX - Medium High Contrast"

bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
bpy.ops.render.render(write_still=True)

triangles = sum(len(poly.vertices) - 2 for poly in character.data.polygons)
world_corners = [character.matrix_world @ Vector(corner) for corner in character.bound_box]
height = max(point.z for point in world_corners) - min(point.z for point in world_corners)
print(f"PLAYER_WOLF_PROXY_BLEND={BLEND_PATH}")
print(f"PLAYER_WOLF_PROXY_FBX={FBX_PATH}")
print(f"PLAYER_WOLF_PROXY_PREVIEW={PREVIEW_PATH}")
print(f"PLAYER_WOLF_PROXY_HEIGHT={height:.6f}")
print(f"PLAYER_WOLF_PROXY_TRIANGLES={triangles}")

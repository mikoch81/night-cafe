#!/usr/bin/env python3
"""Build the Bréve Deck as a 3D model and export it for Unity.

    blender -b -P tools/shell_model.py            # -> Assets/Art/device/breve_deck.fbx (+ textures)

The shell is rendered live in Unity (URP Lit), so this script only produces geometry with UVs
and named parts; materials, lights and the LCD render texture are assigned by the scene setup
(NightCafeSetup.BuildDevice). Units: 1 = 1 cm-ish, matching Unity units 1:1 (the importer is set
to ignore the file scale). Numbers here are mirrored by DeviceLayout.cs / DeviceConfig.cs.

Parts (object names are the contract with the setup):
  Body        wood top with an aluminium chamfer and a recessed LCD window (material slots 0 wood, 1 alu)
  Screen      rounded-rectangle face inside the window, UV 0..1 -> the LCD render texture
  Glass       same shape a hair above the screen, transparent glossy
  Cap_LU/LD/RU/RD   button caps (r 0.6) with a domed top, resting in aluminium collars
  Collar_*    the collars
  LeverRail   aluminium slot for the mode switch, LeverKnob the sliding knob
  LabelA/B    engraved mode letters, Brand the maker's mark, Grille the speaker holes
"""

import math
import os
import shutil
import sys

import bpy
import bmesh
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "Assets", "Art", "device")
FBX_PATH = os.path.join(OUT_DIR, "breve_deck.fbx")
TEXTURE_SRC = os.path.join(ROOT, "art", "textures")
TEXTURE_DST = os.path.join(OUT_DIR, "textures")

# ---- dimensions (DeviceLayout.cs mirrors BODY / LCD, DeviceConfig.cs the button and lever spots)
BODY = 22.0, 11.0, 1.4
BODY_RADIUS = 0.86
CHAMFER = 0.35                   # aluminium chamfer around the wood top
LCD = 13.2, 9.26                 # 1272:892, the screen_bg proportion
LCD_RADIUS = 0.30
LCD_CENTRE_Y = 0.30
LCD_DEPTH = 0.25                 # screen face below the wood top
BUTTONS = {"LU": (-9.0, 1.6), "LD": (-9.0, -1.6), "RU": (9.0, 1.6), "RD": (9.0, -1.6)}
BUTTON_RADIUS = 0.6
CAP_HEIGHT = 0.42                # above the wood top, at rest
LEVER = (0.0, -4.85)
LEVER_RAIL = 3.4, 0.7
LEVER_SLOT = 2.9, 0.3
KNOB = 0.9, 0.9, 0.35
KNOB_X = 0.9                     # DeviceConfig.leverKnobX (mode A = -x)
BRAND = (-8.5, -4.85)
GRILLE = (9.6, -4.85)

TOP = BODY[2]


# ---------------------------------------------------------------- bmesh helpers

def rounded_loop(bm, width, height, radius, centre=(0, 0), z=0.0, segments=12):
    hw, hh = width / 2, height / 2
    cx0, cy0 = centre
    corners = [(hw - radius, hh - radius, 0), (-hw + radius, hh - radius, 90),
               (-hw + radius, -hh + radius, 180), (hw - radius, -hh + radius, 270)]
    verts = []
    for cx, cy, start in corners:
        for i in range(segments + 1):
            a = math.radians(start + 90 * i / segments)
            verts.append(bm.verts.new((cx0 + cx + radius * math.cos(a), cy0 + cy + radius * math.sin(a), z)))
    return verts


def finish(bm, name, materials=1):
    """bmesh -> object in the scene, with box-projected UVs and `materials` empty slots."""
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    box_uv(bm)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for _ in range(materials):
        mesh.materials.append(None)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    mesh.shade_smooth()
    return obj


def box_uv(bm, scale=4.0):
    """Planar projection per face along its dominant normal axis - what Blender's box mapping did
    in the shader, baked into real UVs so Unity can use the same textures. `scale` = texture
    repeat length in units (4 cm of wood grain per tile)."""
    uv_layer = bm.loops.layers.uv.verify()
    for face in bm.faces:
        n = face.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for loop in face.loops:
            p = loop.vert.co
            if az >= ax and az >= ay:
                u, v = p.x, p.y
            elif ax >= ay:
                u, v = p.y, p.z
            else:
                u, v = p.x, p.z
            loop[uv_layer].uv = (u / scale, v / scale)


def extrude_region(bm, faces, depth):
    result = bmesh.ops.extrude_face_region(bm, geom=faces)
    verts = [g for g in result["geom"] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=verts, vec=(0, 0, depth))
    return result


def rounded_slab(name, width, height, radius, depth, centre=(0, 0), z=0.0, materials=1):
    bm = bmesh.new()
    face = bm.faces.new(rounded_loop(bm, width, height, radius, centre, z))
    extrude_region(bm, [face], depth)
    return finish(bm, name, materials)


def rounded_face(name, width, height, radius, centre=(0, 0), z=0.0):
    """A single face with UVs spanning 0..1 across its bounding box (the LCD screen)."""
    bm = bmesh.new()
    face = bm.faces.new(rounded_loop(bm, width, height, radius, centre, z))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    uv_layer = bm.loops.layers.uv.verify()
    for loop in face.loops:
        p = loop.vert.co
        loop[uv_layer].uv = ((p.x - centre[0]) / width + 0.5, (p.y - centre[1]) / height + 0.5)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(None)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def ring(name, outer, outer_radius, inner, inner_radius, depth, z=0.0, inner_centre=(0, 0)):
    bm = bmesh.new()
    o = rounded_loop(bm, outer[0], outer[1], outer_radius, z=z)
    i = rounded_loop(bm, inner[0], inner[1], inner_radius, inner_centre, z=z)
    n = len(o)
    faces = [bm.faces.new((o[k], o[(k + 1) % n], i[(k + 1) % n], i[k])) for k in range(n)]
    extrude_region(bm, faces, depth)
    return finish(bm, name)


def annulus(name, r_out, r_in, depth, z=0.0, segments=64):
    bm = bmesh.new()
    o = [bm.verts.new((r_out * math.cos(2 * math.pi * k / segments), r_out * math.sin(2 * math.pi * k / segments), z)) for k in range(segments)]
    i = [bm.verts.new((r_in * math.cos(2 * math.pi * k / segments), r_in * math.sin(2 * math.pi * k / segments), z)) for k in range(segments)]
    faces = [bm.faces.new((o[k], o[(k + 1) % segments], i[(k + 1) % segments], i[k])) for k in range(segments)]
    extrude_region(bm, faces, depth)
    return finish(bm, name)


def disc(name, radius, depth, z=0.0, segments=64):
    bm = bmesh.new()
    verts = [bm.verts.new((radius * math.cos(2 * math.pi * k / segments), radius * math.sin(2 * math.pi * k / segments), z)) for k in range(segments)]
    face = bm.faces.new(verts)
    extrude_region(bm, [face], depth)
    return finish(bm, name)


def bevel_top(obj, offset, segments=6, z_min=None):
    """Bevels the edges whose both ends sit on the object's top plane (or above z_min)."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    top = max(v.co.z for v in bm.verts) if z_min is None else z_min
    edges = [e for e in bm.edges if all(v.co.z >= top - 1e-4 for v in e.verts)]
    bmesh.ops.bevel(bm, geom=edges, offset=offset, segments=segments, profile=0.5, affect="EDGES",
                    clamp_overlap=True)  # the corner arcs are short segments; unclamped they tear
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    box_uv(bm)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.shade_smooth()


def assign_by_normal(obj, up_slot, side_slot, threshold=0.92):
    """Flat top faces get `up_slot` (wood), everything tilted or vertical `side_slot` (metal)."""
    for poly in obj.data.polygons:
        poly.material_index = up_slot if poly.normal.z > threshold else side_slot


def text_mesh(name, body, size, location, extrude=0.02):
    curve = bpy.data.curves.new(name, type="FONT")
    curve.body = body
    curve.size = size
    curve.extrude = extrude
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.location = (location[0], location[1], TOP)
    bpy.context.view_layer.objects.active = obj
    for other in bpy.context.selected_objects:
        other.select_set(False)
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.view_layer.objects.active
    obj.name = name
    obj.data.materials.clear()
    obj.data.materials.append(None)
    # the text mesh comes with flat UVs; give it box UVs like everything else
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    box_uv(bm, scale=1.0)
    bm.to_mesh(obj.data)
    bm.free()
    return obj


# ---------------------------------------------------------------- the device

def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # Body: wood slab with the LCD window through it. The top edges (outer rim and window rim)
    # get an aluminium chamfer, and the window walls are metal too - a metal-lined recess.
    body = ring("Body", BODY[:2], BODY_RADIUS, LCD, LCD_RADIUS, TOP, inner_centre=(0, LCD_CENTRE_Y))
    body.data.materials.append(None)          # slot 1 = aluminium
    bevel_top(body, CHAMFER, segments=8)
    assign_by_normal(body, up_slot=0, side_slot=1)

    # Screen face and glass, recessed below the wood.
    rounded_face("Screen", LCD[0] - 0.02, LCD[1] - 0.02, LCD_RADIUS, (0, LCD_CENTRE_Y), TOP - LCD_DEPTH)
    rounded_face("Glass", LCD[0] - 0.02, LCD[1] - 0.02, LCD_RADIUS, (0, LCD_CENTRE_Y), TOP - LCD_DEPTH + 0.06)

    # Buttons: collar let into the wood, domed cap standing proud of it.
    for key, (x, y) in BUTTONS.items():
        collar = annulus(f"Collar_{key}", BUTTON_RADIUS + 0.12, BUTTON_RADIUS + 0.01, 0.06, z=TOP)
        collar.location = (x, y, 0)
        well = disc(f"Well_{key}", BUTTON_RADIUS + 0.02, 0.02, z=TOP - 0.3)
        well.location = (x, y, 0)
        cap = disc(f"Cap_{key}", BUTTON_RADIUS, CAP_HEIGHT + 0.3, z=TOP - 0.3)
        bevel_top(cap, 0.16, segments=8)
        cap.location = (x, y, 0)

    # Mode switch: aluminium rail with a dark slot, a knob that slides between A and B.
    rail = ring("LeverRail", LEVER_RAIL, LEVER_RAIL[1] / 2, LEVER_SLOT, LEVER_SLOT[1] / 2, 0.08, z=TOP)
    rail.location = (LEVER[0], LEVER[1], 0)
    slot = rounded_slab("LeverSlot", LEVER_SLOT[0] + 0.2, LEVER_SLOT[1] + 0.2, 0.25, 0.02, z=TOP - 0.15)
    slot.location = (LEVER[0], LEVER[1], 0)
    knob = rounded_slab("LeverKnob", KNOB[0], KNOB[1], 0.18, KNOB[2] + 0.2, z=TOP - 0.2)
    bevel_top(knob, 0.12, segments=6)
    knob.location = (LEVER[0] - KNOB_X, LEVER[1], 0)
    text_mesh("LabelA", "A", 0.5, (LEVER[0] - LEVER_RAIL[0] / 2 - 0.45, LEVER[1]))
    text_mesh("LabelB", "B", 0.5, (LEVER[0] + LEVER_RAIL[0] / 2 + 0.45, LEVER[1]))

    # Maker's mark and speaker grille.
    text_mesh("Brand", "BRÉVE DECK", 0.42, BRAND)
    bm = bmesh.new()
    for row in range(3):
        for col in range(6):
            cx = GRILLE[0] - 0.55 + col * 0.22
            cy = GRILLE[1] + 0.22 - row * 0.22
            verts = [bm.verts.new((cx + 0.07 * math.cos(2 * math.pi * k / 16), cy + 0.07 * math.sin(2 * math.pi * k / 16), TOP - 0.05)) for k in range(16)]
            face = bm.faces.new(verts)
            extrude_region(bm, [face], 0.06)
    finish(bm, "Grille")


def export():
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=False,
        # The exporter always writes centimetres (x100 from Blender metres); 0.01 undoes that so
        # the file's raw units are the model's units, which Unity reads 1:1 (useFileScale off).
        global_scale=0.01,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,       # axis conversion baked into the meshes, no -90 on the nodes
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        path_mode="STRIP",
    )
    print("wrote", os.path.relpath(FBX_PATH, ROOT))


def copy_textures():
    """The wood and metal sets Unity's materials read (Color + NormalGL only; roughness is a
    per-material smoothness value for now)."""
    os.makedirs(TEXTURE_DST, exist_ok=True)
    for name in ("Wood027", "Wood095", "Wood028", "Metal009"):
        for kind in ("Color", "NormalGL"):
            src = os.path.join(TEXTURE_SRC, name, f"{name}_1K-JPG_{kind}.jpg")
            dst = os.path.join(TEXTURE_DST, f"{name}_{kind}.jpg")
            shutil.copyfile(src, dst)
    print("copied textures to", os.path.relpath(TEXTURE_DST, ROOT))


if __name__ == "__main__":
    build()
    export()
    copy_textures()

#!/usr/bin/env python3
"""Render the Bréve Deck shell (Assets/Art/device/*.png) from a Blender model built in code.

    blender -b -P tools/shell_render.py -- [--skin walnut|ash|onyx|neon|all] [--samples 128]
                                            [--engine CYCLES|BLENDER_EEVEE] [--out Assets/Art/device]

The canvases and the placement of every part match the hand-drawn v1 SVGs, so DeviceLayout,
DeviceConfig and the sprite pivots do not change:

  device_shell   1920x1080  body 1880x1020 (rx 86) with the LCD cutout at x 446..1474, y 136..848
  button_normal  560x560    disc r 264 (the game scales it to 180 px)
  button_pressed 560x560    same disc pushed in
  lever_track    1040x176   slot
  lever_knob     304x304    knob

One Blender unit is 100 px, like Unity's 100 PPU. Textures are the CC0 sets in art/textures.
"""

import argparse
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TEXTURES = os.path.join(ROOT, "art", "textures")

SKINS = {
    # wood set, wood tint, edge glow (neon: the chassis band becomes a purple neon tube)
    "walnut": dict(wood="Wood027", tint=(1.0, 0.92, 0.85), glow=0.0),
    "ash": dict(wood="Wood095", tint=(1.0, 0.98, 0.92), glow=0.0),
    "onyx": dict(wood="Wood028", tint=(0.55, 0.55, 0.6), glow=0.0),
    "neon": dict(wood="Wood028", tint=(0.8, 0.6, 1.1), glow=1.0),
}

CANVAS = 19.2, 10.8          # device_shell canvas in units
BODY = 18.8, 10.2, 1.2       # painted wood extents and thickness
BODY_RADIUS = 0.86
FRAME = 10.64, 7.48          # aluminium bezel around the LCD
FRAME_RADIUS = 0.46
LCD = 10.28, 7.12            # glass cutout
LCD_RADIUS = 0.36
LCD_CENTRE_Y = 0.48          # cutout centre sits 48 px above the shell centre


# ---------------------------------------------------------------- mesh helpers

def rounded_rect_mesh(name, width, height, radius, depth, segments=12):
    """Extruded rounded rectangle centred on the origin, top face at z = depth."""
    bm = bmesh.new()
    hw, hh = width / 2, height / 2
    corners = [(hw - radius, hh - radius, 0), (-hw + radius, hh - radius, 90),
               (-hw + radius, -hh + radius, 180), (hw - radius, -hh + radius, 270)]
    verts = []
    for cx, cy, start in corners:
        for i in range(segments + 1):
            a = math.radians(start + 90 * i / segments)
            verts.append(bm.verts.new((cx + radius * math.cos(a), cy + radius * math.sin(a), 0)))
    face = bm.faces.new(verts)
    extruded = bmesh.ops.extrude_face_region(bm, geom=[face])
    top = [g for g in extruded["geom"] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=top, vec=(0, 0, depth))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def rounded_loop(bm, width, height, radius, centre=(0, 0), segments=12):
    hw, hh = width / 2, height / 2
    cx0, cy0 = centre
    corners = [(hw - radius, hh - radius, 0), (-hw + radius, hh - radius, 90),
               (-hw + radius, -hh + radius, 180), (hw - radius, -hh + radius, 270)]
    verts = []
    for cx, cy, start in corners:
        for i in range(segments + 1):
            a = math.radians(start + 90 * i / segments)
            verts.append(bm.verts.new((cx0 + cx + radius * math.cos(a), cy0 + cy + radius * math.sin(a), 0)))
    return verts


def rounded_ring_mesh(name, outer, outer_radius, inner, inner_radius, depth, inner_centre=(0, 0)):
    """A rounded rectangle with a rounded window through it, extruded to `depth`. Built from two
    loops bridged by quads: booleans proved unreliable in background renders."""
    bm = bmesh.new()
    o = rounded_loop(bm, outer[0], outer[1], outer_radius)
    i = rounded_loop(bm, inner[0], inner[1], inner_radius, inner_centre)
    faces = []
    n = len(o)
    for k in range(n):
        faces.append(bm.faces.new((o[k], o[(k + 1) % n], i[(k + 1) % n], i[k])))
    extruded = bmesh.ops.extrude_face_region(bm, geom=faces)
    top = [g for g in extruded["geom"] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=top, vec=(0, 0, depth))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def cylinder(name, radius, depth, segments=96):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments, radius=radius, depth=depth, location=(0, 0, depth / 2))
    obj = bpy.context.active_object
    obj.name = name
    return obj


def bevel(obj, width, segments=6, angle=30):
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(angle)
    mod.harden_normals = True
    return mod


def smooth(obj):
    """Smooth shading; the bevel modifier's harden_normals keeps the flat faces flat."""
    obj.data.shade_smooth()


def boolean_cut(obj, cutter):
    mod = obj.modifiers.new("Cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cutter
    cutter.hide_render = True
    cutter.hide_viewport = True


# ---------------------------------------------------------------- materials

def texture_node(nodes, path, colorspace):
    node = nodes.new("ShaderNodeTexImage")
    node.image = bpy.data.images.load(path, check_existing=True)
    node.image.colorspace_settings.name = colorspace
    node.projection = "BOX"
    return node


def pbr_material(name, texture_set, tint=(1, 1, 1), scale=0.11, metallic=0.0, roughness_bias=0.0,
                 glow=0.0, glow_colour=(0.55, 0.3, 1.0)):
    """Principled material driven by an ambientCG set (Color / NormalGL / Roughness), box mapped."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes["Principled BSDF"]
    folder = os.path.join(TEXTURES, texture_set)
    prefix = os.path.join(folder, texture_set + "_1K-JPG_")

    coords = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (scale, scale, scale)
    links.new(coords.outputs["Object"], mapping.inputs["Vector"])

    colour = texture_node(nodes, prefix + "Color.jpg", "sRGB")
    links.new(mapping.outputs["Vector"], colour.inputs["Vector"])
    tint_node = nodes.new("ShaderNodeMixRGB")
    tint_node.blend_type = "MULTIPLY"
    tint_node.inputs["Fac"].default_value = 1.0
    tint_node.inputs["Color2"].default_value = (*tint, 1.0)
    links.new(colour.outputs["Color"], tint_node.inputs["Color1"])
    links.new(tint_node.outputs["Color"], bsdf.inputs["Base Color"])

    rough = texture_node(nodes, prefix + "Roughness.jpg", "Non-Color")
    links.new(mapping.outputs["Vector"], rough.inputs["Vector"])
    rough_math = nodes.new("ShaderNodeMath")
    rough_math.operation = "ADD"
    rough_math.inputs[1].default_value = roughness_bias
    rough_math.use_clamp = True
    links.new(rough.outputs["Color"], rough_math.inputs[0])
    links.new(rough_math.outputs["Value"], bsdf.inputs["Roughness"])

    normal = texture_node(nodes, prefix + "NormalGL.jpg", "Non-Color")
    links.new(mapping.outputs["Vector"], normal.inputs["Vector"])
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.inputs["Strength"].default_value = 0.6
    links.new(normal.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], bsdf.inputs["Normal"])

    bsdf.inputs["Metallic"].default_value = metallic
    if glow > 0:
        bsdf.inputs["Emission Color"].default_value = (*glow_colour, 1.0)
        bsdf.inputs["Emission Strength"].default_value = glow * 0.35
    return mat


def flat_material(name, colour, roughness=0.5, metallic=0.0, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*colour, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if emission > 0:
        bsdf.inputs["Emission Color"].default_value = (*colour, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission
    return mat


def unlit_material(name, colour):
    """Exact colour regardless of lighting - the LCD glass must be PaletteConfig.glassBlack, the
    game paints screen_bg.png over it and any lit rim would show around the edges."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.remove(nodes["Principled BSDF"])
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (*colour, 1.0)
    emission.inputs["Strength"].default_value = 1.0
    links.new(emission.outputs["Emission"], nodes["Material Output"].inputs["Surface"])
    return mat


# ---------------------------------------------------------------- scene

def reset_scene(width_px, height_px, ortho_width, samples, engine):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = engine
    scene.render.resolution_x = width_px
    scene.render.resolution_y = height_px
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.compression = 60
    scene.view_settings.view_transform = "Standard"  # sRGB, no filmic tone curve on a sprite

    if engine == "CYCLES":
        scene.cycles.samples = samples
        scene.cycles.use_denoising = True
        prefs = bpy.context.preferences.addons["cycles"].preferences
        prefs.compute_device_type = "OPTIX"
        prefs.get_devices()
        for device in prefs.devices:
            device.use = device.type == "OPTIX"
        scene.cycles.device = "GPU"
    else:
        scene.eevee.taa_render_samples = samples

    cam_data = bpy.data.cameras.new("Camera")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho_width
    cam = bpy.data.objects.new("Camera", cam_data)
    cam.location = (0, 0, 20)
    bpy.context.collection.objects.link(cam)
    scene.camera = cam

    # Key light from the top-left like the LCD glare, a soft fill, and a dim warm world.
    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 4.0
    sun_data.angle = math.radians(6)
    sun = bpy.data.objects.new("Sun", sun_data)
    sun.rotation_euler = (math.radians(35), math.radians(-25), math.radians(20))
    bpy.context.collection.objects.link(sun)

    fill_data = bpy.data.lights.new("Fill", "AREA")
    fill_data.energy = 1500
    fill_data.size = 30
    fill = bpy.data.objects.new("Fill", fill_data)
    fill.location = (6, -8, 14)
    fill.rotation_euler = (math.radians(30), math.radians(20), 0)
    bpy.context.collection.objects.link(fill)

    world = bpy.data.worlds.new("World")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.35, 0.25, 0.18, 1.0)
    bg.inputs["Strength"].default_value = 0.6
    scene.world = world
    return scene


def build_shell(skin):
    wood = pbr_material("Wood", skin["wood"], tint=skin["tint"], scale=0.09)
    alu = pbr_material("Aluminium", "Metal009", tint=(0.9, 0.9, 0.9), scale=0.35, metallic=1.0, roughness_bias=0.15)
    glass = unlit_material("Glass", (0.0051, 0.0032, 0.0022))  # #120d09 in linear
    dark = flat_material("Dark", (0.14, 0.09, 0.06), roughness=0.7)

    # Aluminium chassis with the wood top inset into it (a 15 px metal band frames the wood);
    # both are rings around the LCD window, so the recess is real geometry.
    window = (0, LCD_CENTRE_Y)
    chassis = rounded_ring_mesh("Chassis", BODY[:2], BODY_RADIUS, LCD, LCD_RADIUS, BODY[2] - 0.08, window)
    bevel(chassis, 0.08, segments=4)
    smooth(chassis)
    if skin["glow"] > 0:
        chassis.data.materials.append(flat_material("NeonTube", (0.45, 0.22, 0.9), roughness=0.3, emission=1.2 * skin["glow"]))
    else:
        chassis.data.materials.append(alu)
    inset = 0.30 if skin["glow"] <= 0 else 0.56   # the neon tube needs a wider band to read
    body = rounded_ring_mesh("Body", (BODY[0] - inset, BODY[1] - inset), BODY_RADIUS - inset / 2, LCD, LCD_RADIUS, BODY[2], window)
    bevel(body, 0.10, segments=6)
    smooth(body)
    body.data.materials.append(wood)

    # Bezel: aluminium ring sitting proud of the wood, then the glass at the bottom of the recess.
    frame = rounded_ring_mesh("Frame", FRAME, FRAME_RADIUS, LCD, LCD_RADIUS, 0.10)
    frame.location = (0, LCD_CENTRE_Y, BODY[2])
    bevel(frame, 0.03, segments=4)
    smooth(frame)
    frame.data.materials.append(alu)
    lcd = rounded_rect_mesh("Lcd", LCD[0] + 0.4, LCD[1] + 0.4, LCD_RADIUS, 0.05)
    lcd.location = (0, LCD_CENTRE_Y, BODY[2] - 0.30)
    lcd.data.materials.append(glass)

    # Speaker grille (bottom right) and the label plate (bottom left), like v1.
    for i, (x, y) in enumerate([(1700, 960), (1734, 960), (1768, 960), (1717, 988), (1751, 988), (1785, 988)]):
        hole = cylinder(f"Hole{i}", 0.07, 0.02)
        hole.location = ((x - 960) / 100, (540 - y) / 100, BODY[2] + 0.005)
        hole.data.materials.append(glass)
    plate = rounded_rect_mesh("Plate", 2.5, 0.62, 0.14, 0.03)
    plate.location = ((150 + 125 - 960) / 100, (540 - 967) / 100, BODY[2])
    plate.data.materials.append(dark)


def build_button(pressed):
    alu = pbr_material("Aluminium", "Metal009", tint=(0.9, 0.9, 0.9), scale=0.35, metallic=1.0, roughness_bias=0.15)
    # Pressed = lit: the game swaps to this sprite for 100 ms as touch feedback (GDD 4).
    face = (flat_material("Face", (0.72, 0.66, 0.55), roughness=0.55) if not pressed
            else flat_material("Lit", (1.0, 0.62, 0.22), roughness=0.5, emission=0.7))
    rim = cylinder("Rim", 2.64, 0.5)
    bevel(rim, 0.12, segments=6)
    smooth(rim)
    rim.data.materials.append(alu)
    cap = cylinder("Cap", 2.30, 0.5 + (0.16 if not pressed else 0.02))
    bevel(cap, 0.35, segments=10)
    smooth(cap)
    cap.data.materials.append(face)


def build_lever_track():
    alu = pbr_material("Aluminium", "Metal009", tint=(0.9, 0.9, 0.9), scale=0.35, metallic=1.0, roughness_bias=0.15)
    dark = unlit_material("Slot", (0.012, 0.007, 0.004))
    rail = rounded_ring_mesh("Rail", (10.4, 1.76), 0.88, (9.4, 0.8), 0.4, 0.3)
    bevel(rail, 0.10, segments=6)
    smooth(rail)
    rail.data.materials.append(alu)
    floor = rounded_rect_mesh("SlotFloor", 9.6, 1.0, 0.5, 0.02)
    floor.location.z = 0.10
    floor.data.materials.append(dark)


def build_lever_knob():
    alu = pbr_material("Aluminium", "Metal009", tint=(0.9, 0.9, 0.9), scale=0.35, metallic=1.0, roughness_bias=0.15)
    face = flat_material("Face", (0.72, 0.66, 0.55), roughness=0.55)
    rim = cylinder("Rim", 1.36, 0.5)
    bevel(rim, 0.10, segments=6)
    smooth(rim)
    rim.data.materials.append(alu)
    cap = cylinder("Cap", 1.16, 0.62)
    bevel(cap, 0.22, segments=10)
    smooth(cap)
    cap.data.materials.append(face)


def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("wrote", os.path.relpath(path, ROOT))


# ---------------------------------------------------------------- main

def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--skin", default="all")
    parser.add_argument("--samples", type=int, default=128)
    parser.add_argument("--engine", default="CYCLES")
    parser.add_argument("--out", default=os.path.join(ROOT, "Assets", "Art", "device"))
    parser.add_argument("--parts", default="shell,buttons,lever")
    args = parser.parse_args(argv)
    os.makedirs(args.out, exist_ok=True)
    parts = args.parts.split(",")

    if "shell" in parts:
        skins = list(SKINS) if args.skin == "all" else [args.skin]
        for name in skins:
            reset_scene(1920, 1080, CANVAS[0], args.samples, args.engine)
            build_shell(SKINS[name])
            suffix = "" if name == "walnut" else f"_{name}"
            render(os.path.join(args.out, f"device_shell{suffix}.png"))

    if "buttons" in parts:
        for pressed in (False, True):
            reset_scene(560, 560, 5.6, args.samples, args.engine)
            build_button(pressed)
            render(os.path.join(args.out, "button_pressed.png" if pressed else "button_normal.png"))

    if "lever" in parts:
        reset_scene(1040, 176, 10.4, args.samples, args.engine)
        build_lever_track()
        render(os.path.join(args.out, "lever_track.png"))
        reset_scene(304, 304, 3.04, args.samples, args.engine)
        build_lever_knob()
        render(os.path.join(args.out, "lever_knob.png"))


if __name__ == "__main__":
    main()

extends SceneTree
## Reassemble AL's packed portrait through its original UV mesh (not an AI redraw).
## Run with Godot 4.5.1 --path <isolated GL project> --script <absolute script path>.
const SOURCE = "D:/GitClone/AzurLane/data/painting/tashigan_idol_tex/"
const OUTPUT = "D:/GitClone/TashkentSpire2/TashkentSpire2/images/ui/battle_rewards/tashigan_idol_painting.png"
const RAW = "D:/GitClone/.codex-build/tashkent-mu-skin-qa-20261108/tashigan_idol_raw.png"

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var vertices = PackedVector3Array()
	var uv = PackedVector2Array()
	var indices = PackedInt32Array()
	var minimum = Vector2(INF, INF)
	var maximum = Vector2(-INF, -INF)
	for line in FileAccess.get_file_as_string(SOURCE+"tashigan_idol-mesh.obj").split("\n"):
		var parts = line.strip_edges().split(" ", false)
		if parts.is_empty(): continue
		if parts[0] == "v":
			var point = Vector2(-float(parts[1]), -float(parts[2]))
			vertices.append(Vector3(point.x, point.y, 0))
			minimum = minimum.min(point)
			maximum = maximum.max(point)
		if parts[0] == "vt": uv.append(Vector2(float(parts[1]), 1.0-float(parts[2])))
		if parts[0] == "f":
			for j in range(1, 4):
				var pair = parts[j].split("/")
				assert(pair[0] == pair[1])
				indices.append(int(pair[0])-1)
	var arrays = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = vertices
	arrays[Mesh.ARRAY_TEX_UV] = uv
	arrays[Mesh.ARRAY_INDEX] = indices
	var mesh = ArrayMesh.new()
	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	var fit = 1800.0/maxf((maximum-minimum).x, (maximum-minimum).y)
	var viewport = SubViewport.new()
	viewport.size = Vector2i(((maximum-minimum)*fit).ceil())+Vector2i(4,4)
	viewport.transparent_bg = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	var drawing = MeshInstance2D.new()
	drawing.mesh = mesh
	# Use the original Texture2D, not the same-named Sprite crop (2028 vs 2048 px).
	# UVs address the full texture; the crop causes visible seams and missing strips.
	var texture = Image.load_from_file(RAW)
	assert(texture != null and texture.get_size() == Vector2i(2048,1706))
	drawing.texture = ImageTexture.create_from_image(texture)
	drawing.position = -minimum*fit+Vector2(2,2)
	drawing.scale = Vector2.ONE*fit
	viewport.add_child(drawing)
	await process_frame
	await RenderingServer.frame_post_draw
	var rendered = viewport.get_texture().get_image()
	var cropped = rendered.get_region(rendered.get_used_rect())
	assert(cropped.save_png(OUTPUT) == OK)
	print("Reassembled Mu portrait: ", cropped.get_size())
	quit()

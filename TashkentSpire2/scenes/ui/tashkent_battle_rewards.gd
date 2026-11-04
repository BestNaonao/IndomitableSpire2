@tool
extends Control
## Standalone view: no game DLL, input handlers, rewards, or networking.

const DESIGN_SIZE = Vector2(1920, 900)
const ENTRANCE_SPEED_SCALE = 0.5
const ENTRANCE_DURATION = 1.25 / ENTRANCE_SPEED_SCALE
const LOC_PREFIX = "TASHKENTSPIRE2-BATTLE_REWARDS."
const PAINTINGS = ["tashigan", "tashigan_2", "tashigan_3", "tashigan_4_full"]
var _live = false
var _safe_top = -1.0
var _header_safe_top = -1.0
var _entrance_time = ENTRANCE_DURATION
var _rest_positions: Dictionary = {}
var _active_skin = 0
var _nameplate_minimum = 0.0

@export_range(0, 500, 1) var preview_safe_top: float = 180.0:
	set(value):
		preview_safe_top = value
		if is_node_ready(): update_layout()

@export_enum("Simplified Chinese", "English", "Japanese", "Korean") var preview_language: int = 0:
	set(value):
		preview_language = clampi(value, 0, 3)
		if is_node_ready() and not _live: _refresh_preview()
@export_range(0, 3, 1) var preview_skin: int = 0:
	set(value):
		preview_skin = clampi(value, 0, 3)
		if is_node_ready() and not _live: _refresh_preview()

var layout_scale: float:
	get: return maxf(0.001, minf(size.x / DESIGN_SIZE.x, (size.y - safe_top) / DESIGN_SIZE.y))
var entrance_speed_scale: float:
	get: return ENTRANCE_SPEED_SCALE
var safe_top: float:
	get: return _safe_top if _safe_top >= 0 else preview_safe_top * minf(size.x / 1920.0, size.y / 1080.0)
var header_safe_top: float:
	get: return _header_safe_top if _header_safe_top >= 0 else maxf(0.0, safe_top - 18.0 * minf(size.x / 1920.0, size.y / 1080.0))
var header_offset: float:
	get: return maxf(-40.0, (header_safe_top - layout_origin.y) / layout_scale - 20.0)
var layout_origin: Vector2:
	get: return Vector2((size.x - DESIGN_SIZE.x * layout_scale) * 0.5, safe_top + (size.y - safe_top - DESIGN_SIZE.y * layout_scale) * 0.5)

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	resized.connect(update_layout)
	# Order the whole banner after the portrait at the same canvas Z. Higher
	# vanilla overlay screens can still cover the complete decoration normally.
	for node_name in ["VictoryBanner", "Grade", "VictoryTitle", "ResultTitle", "EnglishCaption"]:
		$Design.move_child($Design.get_node(NodePath(node_name)), $Design.get_child_count() - 1)
	for child in $Design.get_children():
		_rest_positions[child.name] = child.position
	set_process(false)
	_refresh_preview()
	update_layout()

func set_safe_top(value: float, header_minimum: float = -1.0) -> void:
	_safe_top = maxf(0.0, value)
	_header_safe_top = header_minimum
	update_layout()

func prepare_entrance() -> void:
	_entrance_time = 0.0
	set_process(false)
	_apply_entrance()

func play_entrance() -> void:
	prepare_entrance()
	set_process(true)

func finish_entrance() -> void:
	_entrance_time = ENTRANCE_DURATION
	set_process(false)
	_apply_entrance()

func _process(delta: float) -> void:
	_entrance_time = minf(_entrance_time + delta, ENTRANCE_DURATION)
	_apply_entrance()
	if _entrance_time >= ENTRANCE_DURATION: set_process(false)

func _phase(start: float, duration: float) -> float:
	var t = clampf((_entrance_time * ENTRANCE_SPEED_SCALE - start) / duration, 0.0, 1.0)
	return 1.0 - pow(1.0 - t, 3.0)

func _reveal(names: Array, progress: float, offset: Vector2, base_offset: Vector2 = Vector2.ZERO) -> void:
	for node_name in names:
		var node = $Design.get_node(NodePath(node_name))
		node.position = _rest_positions[node_name] + base_offset + offset * (1.0 - progress)
		node.modulate.a = progress

func _apply_entrance() -> void:
	# Inspired by AL's scale / AL_jiesuan_* clips, kept entirely below the HUD.
	# No vertical overshoot, large full-screen grade stamp, or repeating flashes.
	var grade_progress = _phase(0.10, 0.333)
	var banner_offset = Vector2(0, header_offset)
	$Design/Grade.position = _rest_positions[&"Grade"] + banner_offset
	$Design/Grade.pivot_offset = Vector2($Design/Grade.size.x * 0.5, 0)
	$Design/Grade.scale = Vector2.ONE * lerpf(1.25, 1.0, grade_progress)
	$Design/Grade.modulate.a = grade_progress
	_reveal(["VictoryTitle", "ResultTitle", "EnglishCaption"], _phase(0.23, 0.43), Vector2(70, 0), banner_offset)
	$Design/VictoryBanner.position = banner_offset
	var header = _phase(0.0, 0.333)
	$Design/VictoryBanner.visible = header > 0.01
	$Design/VictoryBanner/Panel.polygon = PackedVector2Array([Vector2(0,20),Vector2(1140*header,20),Vector2(1035*header,182),Vector2(0,182)])
	$Design/VictoryBanner/Edge.polygon = PackedVector2Array([Vector2(1130*header,20),Vector2(1150*header,20),Vector2(1045*header,182),Vector2(1025*header,182)])
	$Design/VictoryBanner/Underline.points = PackedVector2Array([Vector2(0,185),Vector2(1000*header,185)])
	_reveal(["Painting"], _phase(0.12, 0.60), Vector2(110, 0))
	_reveal(["NamePlate", "NameAccent", "CharacterEnglishName", "CharacterName", "SkinName"], _phase(0.56, 0.36), Vector2(50, 0))
	_reveal(["PreviewRewards"], _phase(0.12, 0.30), Vector2(-36, 0))
	_reveal(["PreviewProceed"], _phase(0.25, 0.333), Vector2(40, 0))
	queue_redraw()

func _load_strings(language: String) -> Dictionary:
	var path = "res://TashkentSpire2/localization/"+language+"/gameplay_ui.json"
	if not FileAccess.file_exists(path):
		path = "res://TashkentSpire2/localization/eng/gameplay_ui.json"
	return JSON.parse_string(FileAccess.get_file_as_string(path))

func _refresh_preview() -> void:
	_nameplate_minimum = 0.0
	_apply(preview_skin, _load_strings(["zhs", "eng", "jpn", "kor"][preview_language]), null, "", "")

func configure(skin: int, strings: Dictionary, font: Font, character_name: String, skin_name: String, nameplate_minimum: float = 0.0) -> void:
	_live = true
	_nameplate_minimum = nameplate_minimum
	$Design/PreviewRewards.hide()
	$Design/PreviewProceed.hide()
	_apply(skin, strings, font, character_name, skin_name)

func _apply(skin: int, strings: Dictionary, font: Font, character_name: String, skin_name: String) -> void:
	var text = func(key: String) -> String: return str(strings.get(LOC_PREFIX+key, key))
	_active_skin = clampi(skin,0,3)
	$Design/Painting/Image.texture = load("res://TashkentSpire2/images/ui/battle_rewards/"+PAINTINGS[_active_skin]+"_painting.png")
	# Restore measuring space before switching to a longer skin/language.
	$Design/CharacterName.size.x = 542
	$Design/SkinName.size.x = 532
	_set_label("Design/VictoryTitle", text.call("victory"), font, 76, 34)
	_set_label("Design/ResultTitle", text.call("result"), font, 34, 22)
	_set_label("Design/CharacterName", character_name if character_name != "" else text.call("character"), font, 44, 26)
	_set_label("Design/SkinName", skin_name if skin_name != "" else text.call("previewSkin"+str(clampi(skin,0,3))), font, 23, 16)
	_set_label("Design/PreviewProceed/Label", text.call("continue"), font, 36, 24)
	_set_label("Design/PreviewRewards/Header", text.call("loot"), font, 44, 28)
	_set_label("Design/PreviewRewards/Gold/Label", text.call("previewGold"), font, 28, 22)
	_set_label("Design/PreviewRewards/Card/Label", text.call("previewCard"), font, 28, 22)
	_set_label("Design/PreviewRewards/Potion/Label", text.call("previewPotion"), font, 28, 22)
	_fit_portrait()
	_fit_nameplate()

func _fit_portrait() -> void:
	var zoom = 1.18 if _active_skin == 2 else (1.25 if _active_skin == 3 else 1.0)
	$Design/Painting/Image.size = $Design/Painting.size * zoom
	$Design/Painting/Image.position = ($Design/Painting.size - $Design/Painting/Image.size) * Vector2(0.5,0.4)

func _fit_nameplate() -> void:
	var character_label = $Design/CharacterName as Label
	var skin_label = $Design/SkinName as Label
	var english_label = $Design/CharacterEnglishName as Label
	var widest = maxf(_nameplate_minimum, english_label.get_theme_font("font").get_string_size(english_label.text, HORIZONTAL_ALIGNMENT_LEFT,-1,22).x + 7.0)
	if not _live:
		# Editor/standalone preview measures all four scripts using system fallback.
		for language in ["zhs", "eng", "jpn", "kor"]:
			var table = _load_strings(language)
			widest = maxf(widest, character_label.get_theme_font("font").get_string_size(str(table[LOC_PREFIX+"character"]),HORIZONTAL_ALIGNMENT_LEFT,-1,44).x)
			widest = maxf(widest, skin_label.get_theme_font("font").get_string_size(str(table[LOC_PREFIX+"previewSkin"+str(_active_skin)]),HORIZONTAL_ALIGNMENT_LEFT,-1,23).x)
	for label in [character_label,skin_label]:
		widest = maxf(widest,label.get_theme_font("font").get_string_size(label.text,HORIZONTAL_ALIGNMENT_LEFT,-1,label.get_theme_font_size("font_size")).x)
	character_label.size.x = widest + 8.0
	skin_label.size.x = widest + 8.0
	english_label.size.x = widest + 8.0
	var right = 858.0 + widest + 32.0
	$Design/NamePlate.polygon = PackedVector2Array([Vector2(850,705),Vector2(right+65,705),Vector2(right,850),Vector2(790,850)])

func _set_label(path: String, text: String, font: Font, maximum: int, minimum: int) -> void:
	var label = get_node(path) as Label
	if font != null: label.add_theme_font_override("font", font)
	label.text = text
	var actual = label.get_theme_font("font")
	var font_size = maximum
	while font_size > minimum and actual.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x > label.size.x-12:
		font_size -= 1
	label.add_theme_font_size_override("font_size", font_size)

func update_layout() -> void:
	if not is_node_ready() or size.x <= 0 or size.y <= 0: return
	$Design.scale = Vector2.ONE * layout_scale
	$Design.position = layout_origin
	_fit_portrait()
	_apply_entrance()

func _poly(color: String, points: Array) -> void:
	draw_colored_polygon(PackedVector2Array(points), Color(color))

func _draw() -> void:
	if size.x <= 0 or size.y <= 0: return
	# Background is full-bleed, including behind relics/HUD. Only foreground
	# titles, rewards and portrait use the inset safe-area transform below.
	draw_set_transform(Vector2.ZERO, 0, size / Vector2(1920,1080))
	draw_polygon(PackedVector2Array([Vector2(0,0),Vector2(1920,0),Vector2(1920,1080),Vector2(0,1080)]), PackedColorArray([Color("29354d"),Color("4a5975"),Color("303e57"),Color("202a40")]))
	for x in range(-1200,2500,145): draw_line(Vector2(x,1080),Vector2(x+760,0),Color(0.78,0.85,1,0.055),1.5,true)
	for y in range(160,1080,150): draw_line(Vector2(0,y),Vector2(1920,y),Color(0.8,0.86,1,0.04),1)
	var backdrop = _phase(0.0, 0.45)
	var shift = 100.0 * (1.0 - backdrop)
	_poly("b3c2dc", [Vector2(1685+shift,0),Vector2(1920+shift,0),Vector2(1920+shift,900),Vector2(1085+shift,1080)])
	_poly("eff1f7", [Vector2(1715+shift,0),Vector2(1760+shift,0),Vector2(1110+shift,1030),Vector2(1065+shift,1030)])
	_poly("cad4e6", [Vector2(1798+shift,0),Vector2(1820+shift,0),Vector2(1210+shift,970),Vector2(1188+shift,970)])
	draw_set_transform(layout_origin, 0, Vector2.ONE*layout_scale)
	var footer = _phase(0.55, 0.55)
	draw_line(Vector2(0,874),Vector2(1920*footer,874),Color("d7dfe9"),2)
	draw_line(Vector2(72,894),Vector2(72+1318*footer,894),Color("bdc9dc"),2)
	draw_line(Vector2(72,894),Vector2(72+668*footer,894),Color("f6c54b"),4)
	for i in range(3): _poly("ffffff",[Vector2(25+i*22,880),Vector2(39+i*22,880),Vector2(34+i*22,890),Vector2(20+i*22,890)])

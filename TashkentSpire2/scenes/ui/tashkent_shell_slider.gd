extends Control
## Presentation only: the existing overlay still confirms and synchronizes one integer.
signal selection_changed(amount: int)

const LAST_POINT := 6
const GRABBER_SIZE := 64.0
const AVAILABLE := Color("82e4ef")
const OVERDRAW := Color("f3aa78")
const UNAVAILABLE := Color("566175")
const FOCUS := Color("f2cd78")

var available_ammo := 0
var max_selection := 0
var focus_threshold := -1
var selected := 0
var strings: Dictionary = {}
@onready var slider: HSlider = $Slider
@onready var handle: TextureRect = $Handle

func _ready() -> void:
	slider.value_changed.connect(_on_value_changed)
	slider.focus_entered.connect(queue_redraw)
	slider.focus_exited.connect(queue_redraw)
	resized.connect(_refresh)
	visibility_changed.connect(_on_visibility_changed)
	_refresh()

func configure(ammo: int, maximum: int, threshold: int, selection: int,
		localized: Dictionary, font: Font = null) -> void:
	available_ammo = maxi(0, ammo)
	max_selection = clampi(maximum, 0, LAST_POINT)
	focus_threshold = threshold
	strings = localized
	if font != null:
		var localized_theme := Theme.new()
		localized_theme.default_font = font
		theme = localized_theme
	set_selection(selection)

func set_selection(amount: int) -> void:
	selected = clampi(amount, 0, max_selection)
	if is_node_ready():
		slider.set_value_no_signal(selected)
		_refresh()

func _on_value_changed(value: float) -> void:
	var amount := clampi(roundi(value), 0, max_selection)
	slider.set_value_no_signal(amount)
	if amount == selected:
		return
	selected = amount
	_refresh()
	selection_changed.emit(selected)

func _on_visibility_changed() -> void:
	# Native Slider cancels mouse dragging when hidden. Do not keep keyboard
	# focus on an invisible slider while the combat peek overlay is showing.
	if is_node_ready() and not is_visible_in_tree():
		slider.release_focus()

func _text(key: String, amount: int = 0) -> String:
	return str(strings.get(key, key)).replace("{amount}", str(amount))

func point_x(amount: int) -> float:
	return slider.position.x + GRABBER_SIZE * 0.5 + \
		maxf(0.0, slider.size.x - GRABBER_SIZE) * clampi(amount, 0, LAST_POINT) / LAST_POINT

func _place_tag(label: Label, amount: int, width: float) -> void:
	label.position.x = clampf(point_x(amount) - width * 0.5, 18.0, size.x - width - 18.0)
	label.size.x = width

func _refresh() -> void:
	if not is_node_ready():
		return
	$AmmoTag.text = _text("available", available_ammo)
	_place_tag($AmmoTag, available_ammo, 280.0)
	for amount in range(LAST_POINT + 1):
		var number: Label = get_node("Point%d" % amount)
		number.position.x = point_x(amount) - 24.0
		number.modulate = _point_color(amount)
		number.add_theme_font_size_override("font_size", 26 if amount == selected else 22)
	handle.position = Vector2(point_x(selected) - GRABBER_SIZE * 0.5,
		slider.position.y + (slider.size.y - GRABBER_SIZE) * 0.5)
	queue_redraw()

func _point_color(amount: int) -> Color:
	if amount > max_selection:
		return UNAVAILABLE
	return AVAILABLE if amount <= available_ammo else OVERDRAW

func _draw() -> void:
	if not is_node_ready():
		return
	var y := slider.position.y + slider.size.y * 0.5
	for amount in range(LAST_POINT):
		var color := _point_color(amount + 1)
		draw_line(Vector2(point_x(amount), y), Vector2(point_x(amount + 1), y), color.darkened(0.4), 5.0, true)
	for amount in range(LAST_POINT + 1):
		var at := Vector2(point_x(amount), y)
		draw_circle(at, 10.0, Color("131d30"))
		draw_arc(at, 9.0, 0.0, TAU, 24, _point_color(amount), 3.0, true)
		if amount <= selected:
			draw_circle(at, 4.0, _point_color(amount))
		if amount > max_selection:
			draw_line(at - Vector2(4, 4), at + Vector2(4, 4), UNAVAILABLE, 2.0, true)
	var ammo_x := point_x(available_ammo)
	if slider.has_focus():
		draw_arc(Vector2(point_x(selected), y), 34.0, 0.0, TAU, 40, Color.WHITE, 2.0, true)
	draw_line(Vector2(ammo_x, 62), Vector2(ammo_x, y - 15), AVAILABLE, 2.0, true)
	draw_colored_polygon(PackedVector2Array([Vector2(ammo_x - 5, y - 17),
		Vector2(ammo_x + 5, y - 17), Vector2(ammo_x, y - 11)]), AVAILABLE)
	if focus_threshold >= 0 and focus_threshold <= LAST_POINT:
		var focus_x := point_x(focus_threshold)
		draw_arc(Vector2(focus_x, y), 16.0, 0.0, TAU, 32, FOCUS, 2.0, true)
		draw_line(Vector2(focus_x, y + 19), Vector2(focus_x, 130), FOCUS, 2.0, true)

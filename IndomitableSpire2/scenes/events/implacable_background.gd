extends Control

# NAncientBgContainer offsets/scales isolated ancient sprites for each aspect ratio.
# These illustrations already include the background, so compensate that transform
# and let the TextureRect cover the viewport without stretching the artwork.
func _ready() -> void:
	var container := get_parent_control()
	if container != null:
		container.item_rect_changed.connect(_queue_fit)
	get_viewport().size_changed.connect(_queue_fit)
	_queue_fit()

func _queue_fit() -> void:
	_fit_to_viewport.call_deferred()

func _fit_to_viewport() -> void:
	if not is_inside_tree():
		return
	var container := get_parent_control()
	if container != null:
		scale = Vector2.ONE / container.get_global_transform().get_scale()
	global_position = Vector2.ZERO
	size = get_viewport_rect().size

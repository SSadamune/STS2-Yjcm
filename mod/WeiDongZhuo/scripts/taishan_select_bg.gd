extends Control

const Runtime = preload("res://WeiDongZhuo/scripts/spine_runtime.gd")
const BACKGROUND_BOUNDS = Rect2(-853.138, -437.780, 1453.875, 846.696)
const CHARACTER_BOUNDS = Rect2(-745.336, -584.784, 1043.492, 836.950)
const CHARACTER_BOTTOM = 0.74
const CHARACTER_CENTER_X = 0.53
var background: Node2D
var character: Node2D

func _ready():
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	clip_contents = true
	background = Runtime.create_sprite(self, "Background", "background")
	character = Runtime.create_sprite(self, "Character", "character")
	if background != null:
		background.name = "BackgroundSpine"
		add_child(background)
	if character != null:
		character.name = "CharacterSpine"
		add_child(character)
	resized.connect(_layout)
	_layout()

func _layout():
	var viewport_size = size if size.x > 1 and size.y > 1 else get_viewport_rect().size
	if background != null:
		# Fit the actual backdrop plane, not flying stones/lightning outside it.
		var fit = maxf(viewport_size.x / BACKGROUND_BOUNDS.size.x, viewport_size.y / BACKGROUND_BOUNDS.size.y) * 1.025
		background.scale = Vector2.ONE * fit
		background.position = viewport_size * 0.5 - BACKGROUND_BOUNDS.get_center() * fit
	if character != null:
		var fit = minf(viewport_size.y * 0.86 / CHARACTER_BOUNDS.size.y, viewport_size.x * 0.66 / CHARACTER_BOUNDS.size.x)
		character.scale = Vector2.ONE * fit
		character.position = Vector2(viewport_size.x * CHARACTER_CENTER_X - CHARACTER_BOUNDS.get_center().x * fit,
			viewport_size.y * CHARACTER_BOTTOM - CHARACTER_BOUNDS.end.y * fit)

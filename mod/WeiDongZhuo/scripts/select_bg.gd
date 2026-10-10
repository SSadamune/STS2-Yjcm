extends Control

const Runtime = preload("res://WeiDongZhuo/scripts/spine_runtime.gd")
var composition: Node2D

func _ready():
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	composition = Node2D.new()
	add_child(composition)
	var background = Runtime.create_sprite(self, "Background", "background")
	var character = Runtime.create_sprite(self, "Character", "character")
	if background != null:
		background.name = "BackgroundSpine"
		composition.add_child(background)
	if character != null:
		character.name = "CharacterSpine"
		composition.add_child(character)
	resized.connect(_layout)
	_layout()

func _layout():
	if composition == null:
		return
	var viewport_size = size if size.x > 0 and size.y > 0 else get_viewport_rect().size
	# The source lobby is intentionally oversized and has finite ground planes.
	# Fill/crop its central viewport so raw outside edges are never letterboxed.
	var fit_scale = maxf(viewport_size.x / 2000.0, viewport_size.y / 1125.0)
	composition.scale = Vector2.ONE * fit_scale
	composition.position = viewport_size * 0.5 - Vector2(-60.0, 0.0) * fit_scale

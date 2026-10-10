extends Node2D

const Runtime = preload("res://WeiDongZhuo/scripts/spine_runtime.gd")

func _ready():
	var character = Runtime.create_sprite(self, "Combat", "character")
	if character == null:
		return
	character.name = "WeiDongZhuoSpine"
	# Measured play bounds: X -730.189..284.460, Godot Y -412.531..413.644.
	# Keep the wide weapon clear of the left edge while placing the body nearer its health bar.
	character.scale = Vector2.ONE * 0.42
	character.position = Vector2(95, -174)
	add_child(character)

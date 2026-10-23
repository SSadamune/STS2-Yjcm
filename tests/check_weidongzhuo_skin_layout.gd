extends SceneTree

func _initialize():
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--pack="):
			if not ProjectSettings.load_resource_pack(argument.trim_prefix("--pack="), true):
				push_error("Could not mount packaged skin layout")
				quit(1)
				return
	var scene = load("res://WeiDongZhuo/scenes/taishan_select_bg.tscn")
	var background_bounds = Rect2(-853.138, -437.780, 1453.875, 846.696)
	var character_bounds = Rect2(-745.336, -584.784, 1043.492, 836.950)
	for viewport_size in [Vector2(1280,720),Vector2(1920,1080),Vector2(2560,1440),Vector2(2560,1080),Vector2(1024,768)]:
		var preview = scene.instantiate()
		preview.set_anchors_preset(Control.PRESET_TOP_LEFT)
		preview.size = viewport_size
		preview.background = Node2D.new()
		preview.character = Node2D.new()
		preview.call("_layout")
		var background_rect = Rect2(preview.background.position + background_bounds.position * preview.background.scale,
			background_bounds.size * preview.background.scale)
		var body_rect = Rect2(preview.character.position + character_bounds.position * preview.character.scale,
			character_bounds.size * preview.character.scale)
		assert(background_rect.position.x <= 0 and background_rect.position.y <= 0)
		assert(background_rect.end.x >= viewport_size.x and background_rect.end.y >= viewport_size.y)
		assert(body_rect.position.x >= 0 and body_rect.end.x <= viewport_size.x)
		if absf(body_rect.get_center().x / viewport_size.x - 0.53) > 0.001:
			push_error("Select portrait must move right to the 53% horizontal anchor")
			preview.background.free()
			preview.character.free()
			preview.free()
			quit(1)
			return
		# Raised composition may crop the cape above the screen; the body must clear bottom UI.
		if absf(body_rect.end.y / viewport_size.y - 0.74) > 0.001:
			push_error("Select portrait is too low: body bottom must anchor at 74%")
			quit(1)
			return
		assert(body_rect.position.y < viewport_size.y * 0.05)
		assert(is_equal_approx(preview.character.scale.x, minf(viewport_size.y * 0.86 / character_bounds.size.y, viewport_size.x * 0.66 / character_bounds.size.x)))
		assert(preview.mouse_filter == Control.MOUSE_FILTER_IGNORE)
		preview.background.free()
		preview.character.free()
		preview.free()
	print("TAISHAN_LAYOUT_OK resolutions=5 cover=true centerX=53% bottom=74% scale=unchanged")
	quit()

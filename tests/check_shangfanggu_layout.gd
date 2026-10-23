extends SceneTree

func _initialize():
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--pack="):
			if not ProjectSettings.load_resource_pack(argument.trim_prefix("--pack="), true):
				push_error("Could not mount packaged runtime resources")
				quit(1)
				return
			print("SHANGFANGGU_PACK_MOUNT_OK")
	call_deferred("check_layout")

func check_layout():
	var packed = load("res://Yjcm/scenes/shang_fang_gu_event_background.tscn") as PackedScene
	if packed == null:
		push_error("ShangFangGu background scene did not load")
		quit(1)
		return
	var scene = packed.instantiate() as Control
	root.add_child(scene)
	var clip = scene.get_node("ArtworkClip") as Control
	var artwork = clip.get_node("Artwork") as TextureRect
	var shade = scene.get_node("ReadingShade") as TextureRect
	if artwork.texture == null or artwork.expand_mode != TextureRect.EXPAND_IGNORE_SIZE or not clip.clip_contents:
		push_error("Artwork must ignore source minimum size and clip its overflow")
		quit(1)
		return
	for size in [Vector2(1920,1080), Vector2(1280,720), Vector2(2560,1440), Vector2(2560,1080), Vector2(1600,1200)]:
		scene.set_anchors_and_offsets_preset(Control.PRESET_TOP_LEFT)
		scene.size = size
		await process_frame
		await process_frame
		var rect = artwork.get_rect()
		var image_size = artwork.texture.get_size()
		if absf(clip.size.x - size.x) > 1 or absf(clip.position.y - size.y * 0.11) > 1 or absf(clip.get_rect().end.y - size.y) > 1:
			push_error("Background must fill the event area without side panels or margins")
			quit(1)
			return
		if rect.position.y != 0 or rect.position.x > 1 or rect.end.x < clip.size.x - 1 or rect.end.y < clip.size.y - 1:
			push_error("Cover image must align to top and cover the entire event area at " + str(size))
			quit(1)
			return
		if absf(rect.size.x / rect.size.y - image_size.x / image_size.y) > 0.001:
			push_error("Cover image must not distort the character")
			quit(1)
			return
		print("SHANGFANGGU_LAYOUT_SIZE_OK ", size, " clip=", clip.get_rect(), " artwork=", rect)
	var gradient = (shade.texture as GradientTexture2D).gradient
	if gradient.sample(0.25).a > 0.15 or gradient.sample(0.75).a < 0.55 or gradient.sample(0.75).a > 0.8:
		push_error("Reading shade must be a translucent gradient, not an opaque split panel")
		quit(1)
		return
	for child in scene.find_children("*", "Control", true, false):
		if child is Control and child.mouse_filter != Control.MOUSE_FILTER_IGNORE:
			push_error("Background must not intercept event input")
			quit(1)
			return
	print("SHANGFANGGU_LAYOUT_OK full-bleed, top-aligned cover, preserved aspect, gradient, input passthrough")
	if "--render" in OS.get_cmdline_user_args():
		scene.size = Vector2(1920,1080)
		root.size = Vector2i(1920,1080)
		await process_frame
		await process_frame
		await RenderingServer.frame_post_draw
		var path = "res://../artifacts/reports/shangfanggu-background.png"
		var result = root.get_texture().get_image().save_png(path)
		if result != OK:
			push_error("Preview save failed: " + str(result))
			quit(1)
			return
		print("SHANGFANGGU_PREVIEW_OK ", path)
	scene.queue_free()
	quit(0)

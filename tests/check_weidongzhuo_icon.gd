extends SceneTree

func _initialize():
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--pack="):
			if not ProjectSettings.load_resource_pack(argument.trim_prefix("--pack="), true):
				fail("Could not mount packaged resources")
				return
	call_deferred("check_icon")

func fail(message: String):
	push_error(message)
	quit(1)

func check_icon():
	var packed = load("res://WeiDongZhuo/scenes/icon.tscn") as PackedScene
	if packed == null:
		fail("Dong Zhuo portrait scene is missing")
		return
	# The game's NTopBarPortrait only adds Character.Icon; it never assigns a size.
	var host = Control.new()
	root.add_child(host)
	var icon = packed.instantiate() as TextureRect
	host.add_child(icon)
	await process_frame
	if icon.size != Vector2(54, 54) or not icon.is_visible_in_tree():
		fail("Top-bar portrait must have a visible rectangle without parent sizing")
		return
	if icon.texture == null or icon.texture.resource_path != "res://WeiDongZhuo/images/avatar.png":
		fail("Top-bar portrait must reuse the existing character avatar")
		return
	var pixels = icon.texture.get_image()
	if pixels == null or pixels.get_used_rect().size == Vector2i.ZERO:
		fail("Character avatar is empty")
		return
	if icon.mouse_filter != Control.MOUSE_FILTER_IGNORE:
		fail("Portrait must not block top-bar input")
		return
	print("WEIDONGZHUO_ICON_OK size=54x54 avatar=existing parent-sized=false")
	host.queue_free()
	quit()

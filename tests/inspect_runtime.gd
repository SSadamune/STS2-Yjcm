extends SceneTree

func _initialize():
	var args = OS.get_cmdline_user_args()
	if not ProjectSettings.load_resource_pack(args[0]):
		quit(1)
		return
	var frame_path = "res://materials/cards/frames/card_frame_red_mat.tres"
	print("FRAME_SOURCE=", FileAccess.get_file_as_string(frame_path))
	var mat = load(frame_path)
	if mat is ShaderMaterial:
		print("FRAME_SHADER=", mat.shader.resource_path)
		print("FRAME_CODE=", mat.shader.code)
		for param in mat.shader.get_shader_uniform_list():
			print("PARAM ", param.name, " = ", mat.get_shader_parameter(param.name))
	print("SPINE_NATIVE=", ClassDB.class_exists("SpineSprite"))
	quit(0)

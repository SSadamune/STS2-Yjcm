extends SceneTree

func _init():
	var scene = load("res://WeiDongZhuo/scenes/creature.tscn") as PackedScene
	if scene == null:
		push_error("Combat scene did not load")
		quit(1)
		return
	var state = scene.get_state()
	for i in range(state.get_node_count()):
		if state.get_node_name(i) != "OrbPos":
			continue
		for j in range(state.get_node_property_count(i)):
			if state.get_node_property_name(i, j) == "position" and state.get_node_property_value(i, j) == Vector2(0, -380):
				print("COMBAT_SCENE_PARSE_OK OrbPos=(0,-380)")
				quit(0)
				return
	push_error("Combat scene OrbPos mismatch")
	quit(1)

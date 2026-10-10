shader_type canvas_item;

// Preserve the native frame silhouette, alpha and UI fades; map only its palette.
uniform vec4 shadow_color : source_color = vec4(0.17, 0.095, 0.045, 1.0);
uniform vec4 body_color : source_color = vec4(0.66, 0.43, 0.15, 1.0);
uniform vec4 highlight_color : source_color = vec4(1.0, 0.85, 0.49, 1.0);
varying vec4 vertex_modulate;

void vertex() {
	vertex_modulate = COLOR;
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	float value = max(tex.r, max(tex.g, tex.b));
	vec3 bronze = mix(shadow_color.rgb, body_color.rgb, smoothstep(0.05, 0.6, value));
	vec3 gold = mix(bronze, highlight_color.rgb, smoothstep(0.58, 1.0, value));
	COLOR = vec4(gold, tex.a) * vertex_modulate;
}

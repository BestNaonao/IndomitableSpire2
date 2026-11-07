shader_type canvas_item;
render_mode unshaded;

varying vec4 draw_tint;

void vertex() {
	draw_tint = COLOR;
}

void fragment() {
	vec4 water = texture(TEXTURE, UV);
	// Keep the flying torpedo's original white foam, without modifying its atlas.
	float foam = water.a * pow(water.r, 1.5);
	COLOR = vec4(vec3(1.0), foam * draw_tint.a);
}

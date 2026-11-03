shader_type canvas_item;
render_mode unshaded;

varying vec4 draw_tint;

void vertex() {
	draw_tint = COLOR;
}

void fragment() {
	vec4 water = texture(TEXTURE, UV);
	// The source wave has blue water under its white crest. Keep only the foam;
	// the same material whitens both sequence atlases without modifying originals.
	float foam = water.a * pow(water.r, 1.5);
	COLOR = vec4(vec3(1.0), foam * draw_tint.a);
}

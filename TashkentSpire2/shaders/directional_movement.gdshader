shader_type canvas_item;
render_mode unshaded;

uniform bool retreat = false;
varying vec4 draw_tint;

void vertex() {
    draw_tint = COLOR;
}

void fragment() {
    vec4 arrow = texture(TEXTURE, UV);
    // A red multiply would darken the cyan source. Remap its value instead,
    // retaining the original edge shading and alpha without changing the PNG.
    float value = max(arrow.r, max(arrow.g, arrow.b));
    vec3 rgb = retreat ? value * vec3(1.0, 0.18, 0.22) : arrow.rgb;
    COLOR = vec4(rgb * draw_tint.rgb, arrow.a * draw_tint.a);
}

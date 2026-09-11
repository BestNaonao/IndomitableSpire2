shader_type canvas_item;
render_mode blend_mix;

// The vanilla AncientBorder node has a fixed alpha of 0.502. Remove only
// that local attenuation while preserving texture alpha and ancestor fades.
uniform float source_node_alpha : hint_range(0.01, 1.0) = 0.502;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    vec4 modulation = COLOR;
    float inherited_alpha = clamp(modulation.a / source_node_alpha, 0.0, 1.0);
    COLOR = vec4(tex.rgb * modulation.rgb, tex.a * inherited_alpha);
}

shader_type canvas_item;
render_mode unshaded;

// Ammo-load arcs only: white with an empty center for readable digits.
uniform vec2 sheet_grid = vec2(1.0);
varying vec4 draw_tint;

void vertex() {
    draw_tint = COLOR;
}

void fragment() {
    vec4 sample_color = texture(TEXTURE, UV);
    float mask = sample_color.a;
    float radius = length(fract(UV * sheet_grid) - vec2(0.5));
    mask *= smoothstep(0.28, 0.34, radius);
    COLOR = vec4(draw_tint.rgb, draw_tint.a * mask);
}

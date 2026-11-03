shader_type canvas_item;
render_mode unshaded, blend_add;

varying vec4 sprite_tint;

void vertex() {
    sprite_tint = COLOR;
}

// Reuse the original Azur Lane gradient as a radial falloff. A circular sector
// prevents its rectangular source footprint from crossing the card/health UI.
void fragment() {
    vec2 point = UV * 2.0 - 1.0;
    float radius = length(point);
    float angle = atan(point.y, point.x);
    float disk = 1.0 - smoothstep(0.93, 1.0, radius);
    float sector = smoothstep(-0.72, -0.04, angle) * (1.0 - smoothstep(0.0, 0.05, angle));
    float energy = 0.3 + 0.7 * texture(TEXTURE, vec2(clamp(radius * 0.55, 0.0, 1.0), 0.5)).r;
    COLOR = vec4(sprite_tint.rgb * energy, sprite_tint.a * disk * sector);
}

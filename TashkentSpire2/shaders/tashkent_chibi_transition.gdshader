shader_type canvas_item;
render_mode unshaded;

// NTransition drives threshold from 0 to 1 while leaving character select.
// Keep the endpoints transparent -> opaque black so scene loading remains hidden.
uniform sampler2D calm_tex : source_color, filter_linear, repeat_disable;
uniform sampler2D reaction_tex : source_color, filter_linear, repeat_disable;
uniform float threshold : hint_range(0.0, 1.0) = 0.0;
uniform float chibi_height : hint_range(0.4, 1.0) = 0.86;

const float CALM_ASPECT = 1.0;
const float REACTION_ASPECT = 1261.0 / 1247.0;

mat2 rotate_2d(float angle) {
	float s = sin(angle);
	float c = cos(angle);
	return mat2(vec2(c, -s), vec2(s, c));
}

void fragment() {
	float progress = clamp(threshold, 0.0, 1.0);
	// Spend more wall-clock time on the entrance while keeping the reaction,
	// shake, and fade-out at their previous pace.
	float t = progress < 0.46
		? progress * (0.27 / 0.46)
		: 0.27 + (progress - 0.46) * (0.73 / 0.54);

	// SCREEN_UV keeps the character centered on NTransition's oversized ColorRect.
	float screen_aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
	vec2 centered = (SCREEN_UV - vec2(0.5, 0.52)) * vec2(screen_aspect, 1.0);

	// Recreate the GIF's closed-eye opening, quick paw-pop, and excited shake.
	float enter = smoothstep(0.0, 0.20, t);
	float reaction_mix = smoothstep(0.15, 0.27, t);
	float overshoot = sin(clamp((t - 0.08) / 0.20, 0.0, 1.0) * PI) * 0.035;
	float calm_scale = mix(0.88, 0.96, enter);
	float reaction_scale = mix(0.88, 1.0, enter) + overshoot;
	float shake_window = smoothstep(0.18, 0.27, t) * (1.0 - smoothstep(0.68, 0.76, t));
	float angle = sin(t * 105.0) * 0.012 * shake_window;
	vec2 shake = vec2(sin(t * 157.0), cos(t * 131.0)) * 0.005 * shake_window;
	vec2 reaction_centered = rotate_2d(angle) * (centered - shake);

	float calm_height = min(chibi_height, screen_aspect * 0.92 / CALM_ASPECT);
	vec2 calm_uv = centered / (vec2(calm_height * CALM_ASPECT, calm_height) * calm_scale) + vec2(0.5);
	vec4 calm = texture(calm_tex, clamp(calm_uv, vec2(0.0), vec2(1.0)));
	calm.a *= step(0.0, calm_uv.x) * step(calm_uv.x, 1.0) * step(0.0, calm_uv.y) * step(calm_uv.y, 1.0);

	float reaction_height = min(chibi_height, screen_aspect * 0.92 / REACTION_ASPECT);
	vec2 reaction_uv = reaction_centered / (vec2(reaction_height * REACTION_ASPECT, reaction_height) * reaction_scale) + vec2(0.5);
	vec4 reaction = texture(reaction_tex, clamp(reaction_uv, vec2(0.0), vec2(1.0)));
	reaction.a *= step(0.0, reaction_uv.x) * step(reaction_uv.x, 1.0) * step(0.0, reaction_uv.y) * step(reaction_uv.y, 1.0);

	vec4 chibi = mix(calm, reaction, reaction_mix);

	// A short brightness kick echoes the yellow surprise marks in the reference GIF.
	float flash = smoothstep(0.16, 0.22, t) * (1.0 - smoothstep(0.30, 0.38, t));
	chibi.rgb *= 1.0 + flash * 0.16;

	float appear = smoothstep(0.0, 0.18, t);
	float disappear = 1.0 - smoothstep(0.72, 0.90, t);
	float chibi_alpha = chibi.a * appear * disappear;

	// Once the character fades away, finish at opaque black for the scene swap.
	float black_alpha = smoothstep(0.78, 1.0, t);
	float out_alpha = chibi_alpha + black_alpha * (1.0 - chibi_alpha);
	vec3 out_rgb = chibi.rgb * chibi_alpha / max(out_alpha, 0.00001);
	COLOR = vec4(out_rgb, out_alpha);
}

shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;

// Same stable, multi-scale ink diffusion and wet rim as Nagato's sakura
// transition entrance. No TIME-driven noise: reversing cannot flicker/pop.
float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float ink_noise(vec2 p) {
	vec2 cell = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash21(cell), hash21(cell + vec2(1.0, 0.0)), f.x),
		mix(hash21(cell + vec2(0.0, 1.0)), hash21(cell + vec2(1.0)), f.x), f.y);
}

void fragment() {
	vec4 scene = texture(TEXTURE, UV);
	float phase = clamp(progress, 0.0, 1.0);
	if (phase <= 0.0) {
		scene.a = 0.0;
	} else if (phase < 1.0) {
		float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
		vec2 p = (SCREEN_UV - vec2(0.66, 0.42)) * vec2(aspect, 1.0);
		float wash = ink_noise(p * 4.5 + vec2(7.1, 3.8));
		float fibers = ink_noise(p * 19.0 + vec2(2.8, 11.2));
		float grain = ink_noise(p * 75.0);
		vec2 distortion = vec2(wash - 0.5, ink_noise(p * 5.2 + vec2(15.3)) - 0.5);
		float distance = length((p + distortion * 0.18) * vec2(0.82, 1.08))
			/ max(aspect * 0.65, 0.82);
		float field = distance + wash * 0.22 + fibers * 0.075 + grain * 0.015;
		float spread = mix(-0.10, 1.28, phase);
		float feather = mix(0.045, 0.085, wash);
		float reveal = 1.0 - smoothstep(spread - feather, spread + feather, field);
		reveal *= smoothstep(0.0, 0.07, phase);
		reveal = mix(reveal, 1.0, smoothstep(0.82, 1.0, phase));
		float rim = (1.0 - smoothstep(0.0, feather * 2.1, abs(field - spread)))
			* (1.0 - smoothstep(0.66, 1.0, phase));
		float grey = dot(scene.rgb, vec3(0.299, 0.587, 0.114));
		scene.rgb = mix(scene.rgb, vec3(grey), rim * 0.42) * (1.0 - rim * 0.30);
		scene.a *= reveal;
	}
	COLOR = scene;
}

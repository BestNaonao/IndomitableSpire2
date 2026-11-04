shader_type canvas_item;
render_mode unshaded;
void vertex() {
    // Preserve the dark outline/shadow draw passes. Only the gold face receives
    // the metal highlight, so any locale can use the same editable font label.
    // Vertex color only: keep Godot's native fragment path for both ordinary
    // editor fonts and the game's MSDF locale fonts.
    if (COLOR.r > 0.7 && COLOR.g > 0.5) {
        float band = clamp(VERTEX.y / 100.0, 0.0, 1.0);
        vec3 gold = mix(vec3(1.0, 0.97, 0.74), vec3(0.96, 0.62, 0.16), band);
        gold += vec3(0.10, 0.08, 0.03) * exp(-pow((band - 0.43) / 0.07, 2.0));
        COLOR.rgb = gold;
    }
}

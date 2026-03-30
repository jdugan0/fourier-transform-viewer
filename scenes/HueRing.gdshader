shader_type canvas_item;

uniform float inner_radius : hint_range(0.0, 0.5) = 0.35;
uniform float outer_radius : hint_range(0.0, 0.5) = 0.5;

vec3 hsv2rgb(float h) {
    return clamp(abs(mod(h * 6.0 + vec3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);
}

void fragment() {
    vec2 uv = UV * 2.0 - 1.0;
    float dist = length(uv);
    float angle = atan(uv.y, uv.x);
    float hue = (angle / TAU) + 0.5;

    float ring = step(inner_radius * 2.0, dist) * step(dist, outer_radius * 2.0);
    COLOR = vec4(hsv2rgb(hue), ring);
}
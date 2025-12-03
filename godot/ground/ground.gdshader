shader_type spatial;

uniform float scale_uv;
uniform vec2 offset_uv;

uniform bool is_albedo;
uniform sampler2D texture_albedo: source_color;

uniform bool is_displacement;
uniform sampler2D texture_displacement;
uniform float scale_displacement;

uniform bool is_normal;
uniform float scale_normal;
uniform sampler2D texture_normal;

uniform bool is_ao;
uniform float scale_ao;
uniform sampler2D texture_ao;

uniform bool is_roughness;
uniform float scale_roughness;
uniform sampler2D texture_roughness;

void vertex() {
	vec2 uv = UV * scale_uv + offset_uv;

	//Displacement
	if (is_displacement) VERTEX.y += (texture(texture_displacement, uv).r - 0.5) * scale_displacement;
}

void fragment() {
	vec2 uv = UV * scale_uv + offset_uv;

	//Albedo
	if (is_albedo) ALBEDO = texture(texture_albedo, uv).rgb;

	//Normal
	if (is_normal) {
		vec3 _normal_map = texture(texture_normal, uv).rgb;
		NORMAL_MAP = _normal_map * scale_normal;
	}

	//Roughness
	if (is_roughness) {
		float _roughness = texture(texture_roughness, uv).r;
		ROUGHNESS = _roughness * scale_roughness;
	}

	//Ambient Occlusion
	if (is_ao) {
		float _ao = texture(texture_ao, uv).r;
		ALBEDO *= _ao * scale_ao;
	}
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
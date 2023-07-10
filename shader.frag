#version 330 core
out vec4 FragColor;

in vec2 texCoord;
in float light;

uniform sampler2D atlas;
uniform float globalLightLevel;
uniform float timer;

void main()
{
	// Retrieve the texture color from the atlas
    float l = 1f / (light * globalLightLevel);
    vec4 texColor = texture(atlas, texCoord) * vec4(l, l, l,1);
    
    // Interpolate between the texture color and shaded color based on ambient occlusion value
    float revealVal = timer;
    if(revealVal > 1) revealVal = 1;
    FragColor = texColor * vec4(1,1,1,revealVal);
}
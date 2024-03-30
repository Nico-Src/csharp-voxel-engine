#version 330 core
out vec4 FragColor;

in vec3 voxColor;
in float voxAO;
in float voxLight;

uniform vec3 ambientColor;  // Ambient light color
uniform float ambientStrength;  // Ambient light strength
uniform float ambientIntensity;  // Ambient light intensity

void main()
{
    // Modulate the voxel color by the ambient occlusion factor
    vec3 finalColor = voxColor * (1.0 - voxAO);  // Apply inverse occlusion

    // Apply ambient lighting
    vec3 ambientLight = ambientColor * ambientStrength * ambientIntensity;
    finalColor += ambientLight;

    // Modulate the final color by voxel light intensity
    finalColor *= voxLight;  // Apply voxel light intensity

    // Output the final color
    FragColor = vec4(finalColor, 1.0);
}
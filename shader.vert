#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;
layout (location = 2) in float aoFactor;
layout (location = 3) in float lightFactor;

out vec3 voxColor;
out float voxAO;
out float voxLight;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
	gl_Position = vec4(aPosition, 1.0) * model * view * projection;
	voxColor = aColor;
	voxAO = aoFactor;
	voxLight = lightFactor;
}
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in float aLight;

out vec2 texCoord;
out float light;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float time;

void main()
{
	float offset = 25f - time * 10f;
	if(offset < 0) offset = 0;
	gl_Position = vec4((aPosition - vec3(0,offset,0)), 1.0) * model * view * projection;
	texCoord = aTexCoord;
	light = aLight;
}
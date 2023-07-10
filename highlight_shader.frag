#version 330 core
out vec4 FragColor;

in vec2 texCoord;

uniform sampler2D tex;

void main()
{
	FragColor = texture(tex, texCoord) * vec4(1.0,1.0,1.0,0.4);
}
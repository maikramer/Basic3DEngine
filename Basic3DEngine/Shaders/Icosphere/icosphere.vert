#version 330

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TexCoord;

uniform ProjectionViewWorldColor
{
    mat4 projection;
    mat4 view;
    mat4 world;
    vec4 color;
};

out vec4 fsin_Color;
out vec3 fsin_Normal;

void main()
{
    vec4 worldPosition = world * vec4(Position, 1.0);
    vec4 viewPosition = view * worldPosition;
    vec4 clipPosition = projection * viewPosition;
    gl_Position = clipPosition;
    fsin_Color = color;
    fsin_Normal = mat3(world) * Normal; // Transform normal to world space
} 
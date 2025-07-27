#version 330

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;

uniform ProjectionViewWorld
{
    mat4 projection;
    mat4 view;
    mat4 world;
};

out vec4 fsin_Color;

void main()
{
    vec4 worldPosition = world * vec4(Position, 1);
    vec4 viewPosition = view * worldPosition;
    vec4 clipPosition = projection * viewPosition;
    gl_Position = clipPosition;
    fsin_Color = Color;
} 
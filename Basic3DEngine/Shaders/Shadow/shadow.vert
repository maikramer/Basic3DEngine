#version 450

layout(location = 0) in vec3 Position;

// Shadow MVP matrix
layout(binding = 0) uniform ShadowMVP {
    mat4 mvpMatrix;
};

void main()
{
    // Transformar posição para espaço da luz
    gl_Position = mvpMatrix * vec4(Position, 1.0);
}
#version 330

layout(location = 0) in vec3 Position;

// Shadow matrices
uniform ShadowMVP {
    mat4 projection;
    mat4 view;
    mat4 world;
};

void main()
{
    // Transformar posição para espaço da luz (projeção * view * world)
    gl_Position = projection * view * world * vec4(Position, 1.0);
}
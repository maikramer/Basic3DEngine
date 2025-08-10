#version 330

in vec3 Position;

out vec3 FragDirection;

// MVP matrix como uniform buffer
uniform MVP {
    mat4 mvpMatrix;
};

void main()
{
    // Usar a posição do vértice como direção para o fragment shader
    FragDirection = Position;
    
    // Transformar posição usando MVP matrix
    vec4 pos = mvpMatrix * vec4(Position, 1.0);
    
    // Garantir que o skybox sempre fique no fundo (z = w)
    gl_Position = pos.xyww;
}
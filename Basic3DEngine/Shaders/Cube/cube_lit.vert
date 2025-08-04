#version 330

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec4 Color;

uniform ProjectionViewWorld
{
    mat4 projection;
    mat4 view;
    mat4 world;
};

out vec3 fsin_WorldPos;      // Posição no mundo
out vec3 fsin_Normal;        // Normal no espaço mundial
out vec4 fsin_Color;         // Cor base
out vec3 fsin_ViewPos;       // Posição da câmera no mundo

void main()
{
    // Transformar posição para o mundo
    vec4 worldPosition = world * vec4(Position, 1.0);
    fsin_WorldPos = worldPosition.xyz;
    
    // Transformar normal para o mundo (sem translação)
    mat3 normalMatrix = mat3(transpose(inverse(world)));
    fsin_Normal = normalize(normalMatrix * Normal);
    
    // Calcular posição da câmera no mundo (inverso da view matrix)
    fsin_ViewPos = vec3(inverse(view)[3]);
    
    // Posição final no clip space
    vec4 viewPosition = view * worldPosition;
    gl_Position = projection * viewPosition;
    
    // Passar cor
    fsin_Color = Color;
} 
#version 330

in vec3 fsin_WorldPos;
in vec3 fsin_Normal;
in vec4 fsin_Color;
in vec3 fsin_ViewPos;
in vec4 fsin_ShadowCoord;

out vec4 OutputColor;

// Shadow map
uniform sampler2D ShadowMap;

// TESTE SIMPLES: Usar uniform buffer direto
layout(std140) uniform LightingData
{
    // Ambient (16 bytes)
    vec3 ambientColor;
    float ambientIntensity;
    
    // Material (16 bytes)
    float materialShininess;
    float materialSpecularIntensity;
    float pad1;
    float pad2;
    
    // Light counts (16 bytes)
    float numDirectionalLights;
    float numPointLights;
    float numSpotLights;
    float pad3;
    
    // Directional lights (32 bytes each, max 4 = 128 bytes)
    // Direction (vec3 + padding) + Color+Intensity (vec4)
    vec4 directionalLightDirections[4];
    vec4 directionalLightColors[4];
    
    // Point lights (48 bytes each, max 4 = 192 bytes)
    // Position (vec3 + padding) + Color+Intensity (vec4) + Range+Attenuation+padding (vec4)
    vec4 pointLightPositions[4];
    vec4 pointLightColors[4];
    vec4 pointLightParams[4]; // x=range, y=attenuation, z,w=padding
};

float calculateShadowFactor()
{
    // Converter shadow coord para [0,1] range
    vec3 shadowCoords = fsin_ShadowCoord.xyz / fsin_ShadowCoord.w;
    shadowCoords = shadowCoords * 0.5 + 0.5;
    
    // Se fora da shadow map, sem sombra
    if (shadowCoords.x < 0.0 || shadowCoords.x > 1.0 || 
        shadowCoords.y < 0.0 || shadowCoords.y > 1.0 ||
        shadowCoords.z > 1.0) {
        return 1.0;
    }
    
    // Bias para evitar shadow acne
    float bias = 0.005;
    float currentDepth = shadowCoords.z - bias;
    
    // PCF (Percentage Closer Filtering) para sombras suaves
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(ShadowMap, 0);
    
    for(int x = -1; x <= 1; ++x) {
        for(int y = -1; y <= 1; ++y) {
            float pcfDepth = texture(ShadowMap, shadowCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth > pcfDepth ? 0.0 : 1.0;
        }
    }
    shadow /= 9.0; // 3x3 grid
    
    return shadow;
}

vec3 calculateDirectionalLight(int index, vec3 normal, vec3 viewDir, vec3 baseColor)
{
    if (index >= int(numDirectionalLights)) return vec3(0.0);
    
    // Direção da luz normalizada (invertida porque vem EM DIREÇÃO à superfície)
    vec3 lightDir = normalize(-directionalLightDirections[index].xyz);
    
    // Componente difusa (Lambert)
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * directionalLightColors[index].rgb * directionalLightColors[index].a;
    
    // Componente especular (Blinn-Phong)
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), materialShininess);
    vec3 specular = spec * directionalLightColors[index].rgb * directionalLightColors[index].a * materialSpecularIntensity;
    
    // Aplicar sombras apenas na primeira luz direcional (luz principal)
    float shadowFactor = (index == 0) ? calculateShadowFactor() : 1.0;
    
    return (diffuse + specular) * baseColor * shadowFactor;
}

vec3 calculatePointLight(int index, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 baseColor)
{
    if (index >= int(numPointLights)) return vec3(0.0);
    
    // Distância da luz até o fragmento
    vec3 lightToFrag = pointLightPositions[index].xyz - fragPos;
    float distance = length(lightToFrag);
    
    // Verificar se está dentro do alcance
    float range = pointLightParams[index].x;
    if (distance > range) {
        return vec3(0.0);
    }
    
    // Direção da luz normalizada
    vec3 lightDir = normalize(lightToFrag);
    
    // Atenuação com base na distância
    float attenuation = pointLightParams[index].y;
    float att = 1.0 / (1.0 + attenuation * distance * distance);
    
    // Componente difusa (Lambert)
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * pointLightColors[index].rgb * pointLightColors[index].a;
    
    // Componente especular (Blinn-Phong)
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), materialShininess);
    vec3 specular = spec * pointLightColors[index].rgb * pointLightColors[index].a * materialSpecularIntensity;
    
    return (diffuse + specular) * baseColor * att;
}

void main()
{
    // Normalizar entradas
    vec3 normal = normalize(fsin_Normal);
    vec3 viewDir = normalize(fsin_ViewPos - fsin_WorldPos);
    vec3 baseColor = fsin_Color.rgb;
    
    // Luz ambiente
    vec3 ambient = ambientColor * ambientIntensity * baseColor;
    
    // Soma de todas as luzes direcionais
    vec3 directionalContribution = vec3(0.0);
    for (int i = 0; i < int(numDirectionalLights) && i < 4; ++i) {
        directionalContribution += calculateDirectionalLight(i, normal, viewDir, baseColor);
    }
    
    // Soma de todas as luzes pontuais  
    vec3 pointContribution = vec3(0.0);
    for (int i = 0; i < int(numPointLights) && i < 4; ++i) {
        pointContribution += calculatePointLight(i, normal, fsin_WorldPos, viewDir, baseColor);
    }
    
    // Resultado final
    vec3 finalColor = ambient + directionalContribution + pointContribution;
    
    // Gamma correction para visual mais realista
    finalColor = pow(finalColor, vec3(1.0/2.2));
    
    // Garantir que não ultrapasse 1.0 e não seja negativo
    finalColor = clamp(finalColor, 0.0, 1.0);
    
    OutputColor = vec4(finalColor, fsin_Color.a);
} 
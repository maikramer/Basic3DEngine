#version 330

in vec4 fsin_Color;
in vec3 fsin_Normal;
in vec4 fsin_WorldPos;
in vec4 fsin_ShadowCoord;

out vec4 OutputColor;

uniform sampler2D ShadowMap;

float calculateShadowFactor()
{
    vec3 shadowCoords = fsin_ShadowCoord.xyz / max(fsin_ShadowCoord.w, 1e-6);
    shadowCoords = shadowCoords * 0.5 + 0.5;
    if (shadowCoords.x < 0.0 || shadowCoords.x > 1.0 ||
        shadowCoords.y < 0.0 || shadowCoords.y > 1.0 ||
        shadowCoords.z > 1.0) {
        return 1.0;
    }
    // Bias com dependência do ângulo entre normal e luz direcional aproximada
    vec3 lightDir = normalize(vec3(-0.3, -0.8, -0.2));
    float ndotl = max(dot(normalize(fsin_Normal), -lightDir), 0.0);
    float slopeBias = (1.0 - ndotl) * 0.01;
    float bias = 0.001 + slopeBias;
    float currentDepth = shadowCoords.z - bias;
    vec2 texelSize = 1.0 / textureSize(ShadowMap, 0);
    float shadow = 0.0;
    for(int x=-1; x<=1; ++x){
        for(int y=-1; y<=1; ++y){
            float pcf = texture(ShadowMap, shadowCoords.xy + vec2(x,y)*texelSize).r;
            shadow += (currentDepth <= pcf ? 1.0 : 0.0);
        }
    }
    return shadow / 9.0;
}

void main()
{
    vec3 normal = normalize(fsin_Normal);
    vec3 lightDir = normalize(vec3(-0.3, -0.8, -0.2));
    float diffuse = max(dot(normal, -lightDir), 0.0);
    float shadow = calculateShadowFactor();
    vec3 color = fsin_Color.rgb * (0.15 + diffuse * shadow);
    OutputColor = vec4(color, fsin_Color.a);
}
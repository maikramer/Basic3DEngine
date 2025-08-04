#version 450

layout(location = 0) in vec3 FragDirection;
layout(location = 0) out vec4 FragColor;

// Uniform buffers
layout(binding = 1) uniform TimeData {
    float time;
    float windSpeed;
    float cloudScale;
    float sunIntensity;
};

layout(binding = 2) uniform LightData {
    vec4 sunDirection;
    vec4 sunColor;
};

// Noise functions for procedural clouds
float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

float noise(vec2 st) {
    vec2 i = floor(st);
    vec2 f = fract(st);
    
    float a = random(i);
    float b = random(i + vec2(1.0, 0.0));
    float c = random(i + vec2(0.0, 1.0));
    float d = random(i + vec2(1.0, 1.0));
    
    vec2 u = f * f * (3.0 - 2.0 * f);
    
    return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

float fbm(vec2 st) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for (int i = 0; i < 5; i++) {
        value += amplitude * noise(st * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    
    return value;
}

vec3 getSkyColor(vec3 direction) {
    float y = direction.y;
    
    // Cores do céu muito mais vibrantes e realistas
    vec3 skyTop = vec3(0.1, 0.3, 0.8);        // Azul céu profundo
    vec3 skyHorizon = vec3(0.5, 0.7, 1.0);    // Azul claro brilhante
    vec3 skyBottom = vec3(0.8, 0.9, 0.95);    // Quase branco no horizonte inferior
    
    // Gradiente mais dramático
    vec3 skyColor;
    if (y > 0.0) {
        // Acima do horizonte: gradiente mais forte
        float t = smoothstep(0.0, 0.8, y);
        skyColor = mix(skyHorizon, skyTop, t);
    } else {
        // Abaixo do horizonte: transição suave para cores mais claras
        float t = smoothstep(-0.2, 0.0, y);
        skyColor = mix(skyBottom, skyHorizon, t);
    }
    
    return skyColor;
}

vec3 getClouds(vec3 direction) {
    // Só renderizar nuvens na parte superior do céu
    if (direction.y < 0.1) {
        return vec3(0.0);
    }
    
    // Projetar direção 3D em coordenadas 2D para o noise
    vec2 cloudUV = direction.xz / (direction.y + 0.1) * cloudScale;
    
    // Adicionar movimento baseado no tempo
    cloudUV += vec2(time * windSpeed * 0.08, time * windSpeed * 0.03);
    
    // Múltiplas camadas de nuvens mais dramáticas
    float cloud1 = fbm(cloudUV * 0.3);
    float cloud2 = fbm(cloudUV * 0.8 + vec2(100.0, 200.0));
    float cloud3 = fbm(cloudUV * 1.5 + vec2(300.0, 400.0));
    
    // Combinar camadas com mais contraste
    float cloudDensity = cloud1 * 0.7 + cloud2 * 0.4 + cloud3 * 0.2;
    
    // Reduzir nuvens perto do horizonte
    float heightFactor = smoothstep(0.1, 0.6, direction.y);
    cloudDensity *= heightFactor;
    
    // Criar bordas mais definidas das nuvens
    cloudDensity = smoothstep(0.3, 0.7, cloudDensity);
    
    // Nuvens mais brilhantes e visíveis
    return vec3(cloudDensity * 1.2);
}

vec3 getSun(vec3 direction) {
    vec3 sunDir = normalize(sunDirection.xyz);
    float sunDot = dot(direction, -sunDir);
    
    // Sol principal mais visível
    float sunSize = 0.03;
    float sunDistance = distance(direction, -sunDir);
    float sun = 1.0 - smoothstep(0.0, sunSize, sunDistance);
    sun = pow(sun, 4.0);
    
    // Halo do sol mais forte
    float halo = pow(max(0.0, sunDot), 8.0) * 0.6;
    
    // Glow atmosférico mais visível
    float glow = pow(max(0.0, sunDot), 1.5) * 0.3;
    
    // Cor do sol mais quente
    vec3 sunSrcColor = vec3(1.0, 0.9, 0.7);
    vec3 sunEffect = (sun * 3.0 + halo + glow) * sunSrcColor * sunIntensity;
    
    return sunEffect;
}

vec3 getAtmosphere(vec3 direction) {
    vec3 sunDir = normalize(sunDirection.xyz);
    float sunDot = dot(direction, -sunDir);
    
    // Scattering atmosférico mais visível
    float rayleigh = 1.0 + sunDot * sunDot * 0.8;
    vec3 rayleighColor = vec3(0.4, 0.7, 1.0) * rayleigh * 0.2;
    
    // Mie scattering mais forte ao redor do sol
    float mie = pow(max(0.0, sunDot), 6.0);
    vec3 mieColor = vec3(1.0, 0.8, 0.6) * mie * 0.4;
    
    return rayleighColor + mieColor;
}

void main() {
    vec3 direction = normalize(FragDirection);
    
    // Componentes do skybox
    vec3 skyColor = getSkyColor(direction);
    vec3 clouds = getClouds(direction);
    vec3 sun = getSun(direction);
    vec3 atmosphere = getAtmosphere(direction);
    
    // Combinar todos os elementos de forma mais natural
    vec3 finalColor = skyColor;
    
    // Adicionar efeitos atmosféricos
    finalColor += atmosphere;
    
    // Adicionar sol
    finalColor += sun;
    
    // Adicionar nuvens com mistura mais realista
    vec3 cloudColor = vec3(0.9, 0.95, 1.0); // Nuvens levemente azuladas
    finalColor = mix(finalColor, cloudColor, clouds.r * 0.7);
    
    // Aumentar a saturação e brilho geral
    finalColor *= 1.3;
    
    // Gamma correction mais suave
    finalColor = pow(finalColor, vec3(1.0 / 2.0));
    
    // Clamp para evitar over-saturation
    finalColor = clamp(finalColor, 0.0, 1.0);
    
    FragColor = vec4(finalColor, 1.0);
}
#version 330

in vec4 fsin_Color;
in vec3 fsin_Normal;

out vec4 OutputColor;

void main()
{
    // Iluminação simples baseada na normal
    vec3 lightDir = normalize(vec3(0.5, 0.5, 1.0));
    float lightIntensity = max(dot(normalize(fsin_Normal), lightDir), 0.3);
    OutputColor = vec4(fsin_Color.rgb * lightIntensity, fsin_Color.a);
} 
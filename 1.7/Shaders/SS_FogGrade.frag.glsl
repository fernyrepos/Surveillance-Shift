#version 450

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;       // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST;  // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _Grade;       // @default = vec4(1.0, 1.0, 1.0, 0.0)
    float _BlurRadius; // @default = 0.0
} u;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _GrabSurveillanceFog;

const int TAPS = 40;
const float GOLDEN_ANGLE = 2.39996323;
const float TAU = 6.2831853;

float interleavedNoise(vec2 p)
{
    return fract(52.9829189 * fract(dot(p, vec2(0.06711056, 0.00583715))));
}

void main()
{
    float mask = texture(_MainTex, vUv * u._MainTex_ST.zw + u._MainTex_ST.xy).a * u._Color.a * vColor.a;
    if (mask <= 0.003)
        discard;

    vec2 size = vec2(textureSize(_GrabSurveillanceFog, 0));
    vec2 uv = gl_FragCoord.xy / size;
    float radius = u._BlurRadius * size.y;

    vec3 color;
    if (radius < 0.75)
    {
        color = texture(_GrabSurveillanceFog, uv).rgb;
    }
    else
    {
        float spin = interleavedNoise(gl_FragCoord.xy) * TAU;
        vec3 sum = vec3(0.0);
        for (int i = 0; i < TAPS; i++)
        {
            float r = sqrt((float(i) + 0.5) / float(TAPS)) * radius;
            float a = float(i) * GOLDEN_ANGLE + spin;
            sum += texture(_GrabSurveillanceFog, uv + vec2(cos(a), sin(a)) * r / size).rgb;
        }
        color = sum / float(TAPS);
    }

    float lum = dot(color, vec3(0.22, 0.707, 0.071));
    color = mix(vec3(lum), color, u._Grade.y);
    color = (color - 0.5) * u._Grade.x + 0.5;
    color *= u._Grade.z;

    outColor = vec4(clamp(color, 0.0, 1.0), mask);
}

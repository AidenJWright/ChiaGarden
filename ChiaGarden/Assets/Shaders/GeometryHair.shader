// Per input triangle, emits 7 blades arranged like Voronoi cells:
//   1 main blade  (5 hinges, full length)
//   2 half blades (3 hinges, 50% length)
//   4 short blades(2 hinges, 25% length)
// Each blade's root is a random barycentric point inside the source triangle.
// Blade shape is a quadratic bezier so gravity produces a smooth arc.
// Desktop/PC only — geometry shaders unsupported on mobile/WebGL.
Shader "Hair/GeometryHair"
{
    Properties
    {
        _RootColor       ("Root Color",       Color)        = (0.1, 0.4, 0.1, 1)
        _TipColor        ("Tip Color",        Color)        = (0.3, 0.8, 0.3, 1)
        _HairLength      ("Hair Length",      Float)        = 0.15
        _StrandWidth     ("Strand Width",     Float)        = 0.01
        _GravityStrength ("Gravity Strength", Range(0, 1))  = 0.35
        _ColorJitter     ("Color Jitter",     Range(0, 0.5))= 0.15
        _WindDirection   ("Wind Direction",   Vector)       = (1, 0, 0, 0)
        _WindStrength    ("Wind Strength",    Float)        = 0.05
        _WindFrequency   ("Wind Frequency",   Float)        = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _RootColor;
                half4  _TipColor;
                float  _HairLength;
                float  _StrandWidth;
                float  _GravityStrength;
                float  _ColorJitter;
                float4 _WindDirection;
                float  _WindStrength;
                float  _WindFrequency;
            CBUFFER_END

            // ---- Structs ----

            struct v2g
            {
                float3 posWS  : TEXCOORD0;
                float3 normWS : TEXCOORD1;
            };

            struct g2f
            {
                float4 hcs   : SV_POSITION;
                half4  color : TEXCOORD0;
            };

            // ---- Vertex ----

            v2g vert(float4 posOS : POSITION, float3 normOS : NORMAL)
            {
                v2g o;
                o.posWS  = TransformObjectToWorld(posOS.xyz);
                o.normWS = TransformObjectToWorldNormal(normOS);
                return o;
            }

            // ---- Utility ----

            // Returns two pseudo-random values in [0, 1) from a 2D seed.
            float2 Hash22(float2 p)
            {
                p  = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac(float2(p.x * p.y, p.x + p.y));
            }

            float Hash21(float2 p) { return Hash22(p).x; }

            // Maps a [0,1)^2 sample to a uniform barycentric coordinate inside
            // the triangle. Points outside the triangle are folded back in.
            float3 ToBary(float2 r)
            {
                if (r.x + r.y > 1.0) r = 1.0 - r;
                return float3(r.x, r.y, 1.0 - r.x - r.y);
            }

            float3 BaryLerp3(float3 a, float3 b, float3 c, float3 bary)
            {
                return bary.x * a + bary.y * b + bary.z * c;
            }

            // ---- Blade emitter ----
            // Emits one tapered blade as a triangle strip.
            //   root     – world-space base position
            //   norm     – world-space surface normal at root
            //   bladeLen – total length of the blade
            //   nRows    – number of vertex rows (= hinge count + 2)
            //   cRand    – [0,1] random value driving per-blade color jitter
            void EmitBlade(float3 root, float3 norm, float bladeLen, int nRows,
                           float cRand, inout TriangleStream<g2f> stream)
            {
                // Wind displaces the tip; each blade gets a slightly different phase
                // based on its world X so adjacent blades don't sway in lockstep.
                float windPhase  = _Time.y * _WindFrequency + root.x * 2.0;
                float3 windTip   = float3(_WindDirection.x, 0.0, _WindDirection.z)
                                   * (_WindStrength * sin(windPhase));

                // Quadratic bezier:
                //   P0 = root (planted)
                //   P2 = natural tip if straight + gravity droop + wind
                //   P1 = control point pulled toward the normal to give the
                //        blade a natural outward-then-drooping arc
                float3 P0 = root;
                float3 P2 = root
                          + norm          * ((1.0 - _GravityStrength) * bladeLen)
                          + float3(0,-1,0) * (_GravityStrength * bladeLen)
                          + windTip;
                float3 P1 = lerp(P0, P2, 0.35) + norm * bladeLen * 0.45;

                // Billboard right vector perpendicular to the blade normal and view dir
                float3 toCamera = normalize(GetCameraPositionWS() - root);
                float3 right    = cross(norm, toCamera);
                float  rLen     = length(right);
                if (rLen < 0.0001) return;
                right /= rLen;

                // Per-blade brightness jitter keeps hue consistent while varying value
                half brightness = lerp(1.0 - _ColorJitter, 1.0 + _ColorJitter, cRand);
                half3 rc = saturate(_RootColor.rgb * brightness);
                half3 tc = saturate(_TipColor.rgb  * brightness);

                g2f v;
                for (int row = 0; row < nRows; row++)
                {
                    float t   = (float)row / (float)(nRows - 1);
                    float mt  = 1.0 - t;

                    // Bezier position at this height along the blade
                    float3 pos = mt * mt * P0 + 2.0 * mt * t * P1 + t * t * P2;

                    // Width tapers sharply near the tip (quadratic falloff)
                    float hw = _StrandWidth * 0.5 * (1.0 - t * t * 0.92);

                    half4 color = half4(lerp(rc, tc, t), 1.0);
                    v.color = color;

                    v.hcs = TransformWorldToHClip(pos - right * hw); stream.Append(v);
                    v.hcs = TransformWorldToHClip(pos + right * hw); stream.Append(v);
                }
                stream.RestartStrip();
            }

            // ---- Geometry shader ----
            // 1 main (7 rows) + 2 half (5 rows) + 4 short (4 rows)
            // = 14 + 10 + 16 = 40... wait let me recount:
            // 1 * 7*2 = 14, 2 * 5*2 = 20, 4 * 4*2 = 32  → total 66
            [maxvertexcount(66)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> stream)
            {
                // Use triangle centroid to derive a stable per-triangle seed
                float3 ctd = (input[0].posWS + input[1].posWS + input[2].posWS) / 3.0;
                float2 seed     = ctd.xz + ctd.y * float2(73.1, 91.3);

                // Cache edge vectors for barycentric interpolation
                float3 p0 = input[0].posWS, p1 = input[1].posWS, p2 = input[2].posWS;
                float3 n0 = input[0].normWS, n1 = input[1].normWS, n2 = input[2].normWS;

                float3 bary; float2 r;

                // ---- Main blade: 5 hinges → 7 rows, full length ----
                r    = Hash22(seed);
                bary = ToBary(r);
                EmitBlade(
                    BaryLerp3(p0, p1, p2, bary),
                    normalize(BaryLerp3(n0, n1, n2, bary)),
                    _HairLength, 7,
                    Hash21(seed + float2(0.3, 0.7)),
                    stream);

                // ---- Half blades: 3 hinges → 5 rows, 50% length ----
                r    = Hash22(seed + float2(1.7, 3.1));
                bary = ToBary(r);
                EmitBlade(
                    BaryLerp3(p0, p1, p2, bary),
                    normalize(BaryLerp3(n0, n1, n2, bary)),
                    _HairLength * 0.5, 5,
                    Hash21(seed + float2(1.3, 2.9)),
                    stream);

                r    = Hash22(seed + float2(5.3, 2.7));
                bary = ToBary(r);
                EmitBlade(
                    BaryLerp3(p0, p1, p2, bary),
                    normalize(BaryLerp3(n0, n1, n2, bary)),
                    _HairLength * 0.5, 5,
                    Hash21(seed + float2(4.7, 3.3)),
                    stream);

                // ---- Short blades: 2 hinges → 4 rows, 25% length ----
                r    = Hash22(seed + float2(8.1, 1.3));
                bary = ToBary(r);
                EmitBlade(
                    BaryLerp3(p0, p1, p2, bary),
                    normalize(BaryLerp3(n0, n1, n2, bary)),
                    _HairLength * 0.25, 4,
                    Hash21(seed + float2(7.9, 2.1)),
                    stream);

                r    = Hash22(seed + float2(2.9, 6.7));
                bary = ToBary(r);
                EmitBlade(
                    BaryLerp3(p0, p1, p2, bary),
                    normalize(BaryLerp3(n0, n1, n2, bary)),
                    _HairLength * 0.25, 4,
                    Hash21(seed + float2(3.1, 5.9)),
                    stream);

                r    = Hash22(seed + float2(4.1, 8.3));
                bary = ToBary(r);
                EmitBlade(
                    BaryLerp3(p0, p1, p2, bary),
                    normalize(BaryLerp3(n0, n1, n2, bary)),
                    _HairLength * 0.25, 4,
                    Hash21(seed + float2(5.3, 7.7)),
                    stream);

                r    = Hash22(seed + float2(6.7, 4.9));
                bary = ToBary(r);
                EmitBlade(
                    BaryLerp3(p0, p1, p2, bary),
                    normalize(BaryLerp3(n0, n1, n2, bary)),
                    _HairLength * 0.25, 4,
                    Hash21(seed + float2(7.1, 4.3)),
                    stream);
            }

            // ---- Fragment ----

            half4 frag(g2f IN) : SV_Target
            {
                return IN.color;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}

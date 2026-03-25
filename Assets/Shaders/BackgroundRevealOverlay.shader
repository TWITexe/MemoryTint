Shader "MemoryTint/BackgroundRevealOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _GlobalReveal ("Global Reveal", Range(0, 1)) = 0
        _SoftEdge ("Soft Edge", Range(0.0001, 0.5)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #define MAX_REVEAL_SHAPES 16
            #define MAX_REVEAL_VERTICES 192
            #define MAX_VERTICES_PER_SHAPE 24

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                half4 color         : COLOR;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _GlobalReveal;
                float _SoftEdge;
                float _RevealShapeCount;
                float4 _RevealShapeMeta[MAX_REVEAL_SHAPES];
                float4 _RevealVertices[MAX_REVEAL_VERTICES];
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color * _Color * unity_SpriteColor;
                o.screenPos = ComputeScreenPos(o.positionCS);

                return o;
            }

            float SignedDistanceToPolygon(float2 p, int startIndex, int vertexCount)
            {
                int lastIndex = startIndex + max(vertexCount - 1, 0);
                float2 previous = _RevealVertices[lastIndex].xy;
                float minDistanceSquared = dot(p - previous, p - previous);
                float signValue = 1.0;

                [loop]
                for (int i = 0; i < MAX_VERTICES_PER_SHAPE; i++)
                {
                    if (i >= vertexCount)
                    {
                        break;
                    }

                    int currentIndex = startIndex + i;
                    float2 current = _RevealVertices[currentIndex].xy;

                    float2 edge = current - previous;
                    float2 toPoint = p - previous;
                    float edgeLengthSquared = max(dot(edge, edge), 0.000001);
                    float h = saturate(dot(toPoint, edge) / edgeLengthSquared);
                    float2 closestPoint = previous + (edge * h);
                    float2 diff = p - closestPoint;
                    minDistanceSquared = min(minDistanceSquared, dot(diff, diff));

                    bool checkA = p.y >= previous.y;
                    bool checkB = p.y < current.y;
                    bool checkC = (edge.x * toPoint.y) > (edge.y * toPoint.x);
                    if ((checkA && checkB && checkC) || (!checkA && !checkB && !checkC))
                    {
                        signValue *= -1.0;
                    }

                    previous = current;
                }

                return signValue * sqrt(minDistanceSquared);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;

                float2 screenUV = i.screenPos.xy / max(i.screenPos.w, 0.00001);
                float mask = saturate(_GlobalReveal);
                int shapeCount = min((int)_RevealShapeCount, MAX_REVEAL_SHAPES);

                [loop]
                for (int shapeIndex = 0; shapeIndex < MAX_REVEAL_SHAPES; shapeIndex++)
                {
                    if (shapeIndex >= shapeCount)
                    {
                        break;
                    }

                    int startIndex = (int)_RevealShapeMeta[shapeIndex].x;
                    int vertexCount = min((int)_RevealShapeMeta[shapeIndex].y, MAX_VERTICES_PER_SHAPE);
                    float halo = max(_RevealShapeMeta[shapeIndex].z, 0.0);
                    float strength = max(_RevealShapeMeta[shapeIndex].w, 0.0);

                    if (vertexCount < 3 || startIndex < 0 || (startIndex + vertexCount) > MAX_REVEAL_VERTICES)
                    {
                        continue;
                    }

                    float signedDistance = SignedDistanceToPolygon(screenUV, startIndex, vertexCount);
                    float reveal = 1.0 - smoothstep(0.0, max(_SoftEdge, 0.0001), signedDistance - halo);
                    mask = max(mask, saturate(reveal * strength));
                }

                col.a *= (1.0 - saturate(mask));
                return col;
            }
            ENDHLSL
        }
    }
}

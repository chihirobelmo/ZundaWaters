// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/ColorByHeight" {
    Properties {
        _ColorLow ("Color at Lowest Point", Color) = (1, 0, 0, 1)
        _ColorSurface ("Color at WaterSurface Point", Color) = (0, 1, 0, 1)
        _ColorLand ("Color at LandSurface Point", Color) = (0, 1, 0, 1)
        _ColorHigh ("Color at Highest Point", Color) = (0, 1, 0, 1)
        _HeightLow ("Lowest Point", Range(-2000, 8000)) = -2000
        _HeightMid ("Middle Point", Range(-2000, 8000)) = 0
        _HeightHigh ("Highest Point", Range(-2000, 8000)) = 8000
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            Tags { "LightMode" = "ForwardBase" }  // Use ForwardBase light mode

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            // #pragma multi_compile_fwdbase  // Compile for forward rendering base pass
            // #pragma multi_compile_shadowcaster  // Add this line
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldViewDir : TEXCOORD2;
                //float4 lightmapUV : TEXCOORD2;  // Manually define the lightmap UV coordinates
                //float4 screenPos : TEXCOORD3;  // Manually define the screen position
                float3 worldRefl : TEXCOORD4;
                SHADOW_COORDS(5)
                //UNITY_FOG_COORDS(1)  // Add fog coordinates
            };

            float _HeightLow;
            float _HeightMid;
            float _HeightHigh;
            float4 _ColorLow;
            float4 _ColorSurface;
            float4 _ColorLand;
            float4 _ColorHigh;

            v2f vert (appdata v) {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                //o.lightmapUV = v.vertex;  // Set the lightmap UV coordinates
                //o.screenPos = ComputeScreenPos(o.vertex);  // Set the screen position
                o.worldRefl = reflect(-o.worldViewDir, o.normal);
                //UNITY_TRANSFER_FOG(o,o.vertex);  // Transfer fog data
                TRANSFER_SHADOW(o)
                return o;
            }

            float4 fresnelSchlick(float cosTheta, float F0) {
                return F0 + (1 - F0) * pow(1 - cosTheta, 5);
            }

            float4 frag (v2f i) : SV_Target {

                fixed shadow = SHADOW_ATTENUATION(i);
                float3 normalDirection = normalize(i.normal);

                float4 skyDiffuse = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, normalDirection);
                float3 envDiffuse = DecodeHDR(skyDiffuse, unity_SpecCube0_HDR);
                float4 skySpecular = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, i.worldRefl);
                float3 envSpecular = DecodeHDR(skySpecular, unity_SpecCube0_HDR);

                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = max(0, dot(normalDirection, lightDirection));
                float ndotv = max(0, dot(normalDirection, i.worldViewDir));
                float F = fresnelSchlick(ndotv, 0.04);

                float4 color;
                if (i.worldPos.y < _HeightMid) {
                    color = lerp(_ColorLow, _ColorSurface, (i.worldPos.y - _HeightLow) / (_HeightMid - _HeightLow));
                } else {
                    color = lerp(_ColorLand, _ColorHigh, (i.worldPos.y - _HeightMid) / (_HeightHigh - _HeightMid));
                }
                
                //UNITY_APPLY_FOG(i.fogCoord, color);  // Apply fog to the color
                return color * ndotl * shadow * (1 - F) + float4(envDiffuse, 1) * (1 - F) + float4(envSpecular, 1) * F;
            }
            ENDCG
        }

        // shadow caster レンダリングパス。UnityCG.cginc のマクロを使って
        // 手動で実装されます。
        Pass
        {
            Tags {"LightMode"="ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { 
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
Shader "Custom/PBR"
{
    Properties
    {
        _Albedo ("Albedo (RGB)", 2D) = "white" {}
        _ARM ("AO Roughness Metalness (ARM)", 2D) = "white" {}
        _Normal ("Normal", 2D) = "white" {}
        _BRDF ("BRDF LUT", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass {
            Tags { "LightMode" = "ForwardBase" }  // Use ForwardBase light mode

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _Albedo;
            sampler2D _ARM;
            sampler2D _Normal;
            sampler2D _BRDF;
            samplerCUBE _Cube;

            #include "UnityCG.cginc"
            //#include "UnityLightingCommon.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "AutoLight.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
                float4 tangent : TANGENT;
            };

            // It turns out when you use TRANSFER_VERTEX_TO_FRAGMENT to create shadows 
            // if you have a custom struct and your semantics are not named exactly as they are here: 
            // https://docs.unity3d.com/Manual/SL-VertexProgramInputs.html
            struct v2f {
                float4 pos : SV_POSITION;
                float4 tangent : TANGENT;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldViewDir : TEXCOORD2;
                float2 texcoord : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                SHADOW_COORDS(5)
                //UNITY_FOG_COORDS(7)  // Add fog coordinates
            };

            v2f vert (appdata v) {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.screenPos = ComputeScreenPos(o.pos);
                o.tangent = v.tangent;
                o.texcoord = v.texcoord;
                TRANSFER_SHADOW(o)
                //UNITY_TRANSFER_FOG(o,o.vertex);  // Transfer fog data
                return o;
            }

            // GGX/Towbridge-Reitz normal distribution function.
            // Uses Disney's reparametrization of alpha = roughness^2.
            float NdfGGX(float cosLh, float roughness)
            {
                float alpha = roughness * roughness;
                float alphaSq = alpha * alpha;

                float denom = (cosLh * cosLh) * (alphaSq - 1.0) + 1.0;
                return alphaSq / (3.1415 * denom * denom);
            }

            // Single term for separable Schlick-GGX below.
            float GaSchlickG1(float cosTheta, float k)
            {
                return cosTheta / (cosTheta * (1.0 - k) + k);
            }

            // Schlick-GGX approximation of geometric attenuation function using Smith's method.
            float GaSchlickGGX(float cosLi, float cosLo, float roughness)
            {
                float r = roughness + 1.0;
                float k = (r * r) / 8.0; // Epic suggests using this roughness remapping for analytic lights.
                return GaSchlickG1(cosLi, k) * GaSchlickG1(cosLo, k);
            }

            float3 FresnelSchlick(float cosTheta, float3 F0, float FD90) 
            {
                return (F0 + (FD90 - F0) * pow(1.0 - cosTheta, 5.0f));
            }

            float3 FresnelSchlick(float cosTheta, float3 F0) {
                return F0 + (1 - F0) * pow(1 - cosTheta, 5);
            }

            float FresnelSchlick(float cosTheta, float F0) {
                return F0 + (1 - F0) * pow(1 - cosTheta, 5);
            }

            // Note: Disney diffuse must be multiply by diffuseAlbedo / PI. This is done outside of this function.
            half DisneyDiffuseRe(half NdotV, half NdotL, half LdotH, half perceptualRoughness)
            {
                half fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
                // Two schlick fresnel term
                half lightScatter   = (1 + (fd90 - 1) * Pow5(1 - NdotL));
                half viewScatter    = (1 + (fd90 - 1) * Pow5(1 - NdotV));

                return lightScatter * viewScatter;
            }

            float4 frag (v2f i) : SV_Target {

                fixed shadow = SHADOW_ATTENUATION(i);

	            float3 radiance = _LightColor0;
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float3 H = normalize(V + L);
                float3 N = normalize(i.normal);
	            float3 T = normalize(i.tangent);
                float3 B = cross(N, T);
                float3x3 TBN = float3x3(T, B, N);

                float3 albedo = tex2D(_Albedo, i.texcoord);
                float alpha = tex2D(_Albedo, i.texcoord).a;
                float ambientOcclusion = tex2D(_ARM, i.texcoord).r;
                float roughness = tex2D(_ARM, i.texcoord).g;
                float metalness = tex2D(_ARM, i.texcoord).b;
                float3 normalMap = UnpackNormal(tex2D(_Normal, i.texcoord));

                float3 normal = normalize(mul(normalMap, TBN));
                float3 R = reflect(-V, normal);

                float NdotL = max(dot(normal, L), 0.0f);
                float NdotV = max(dot(normal, V), 0.0f);
                float NdotH = max(dot(normal, H), 0.0f);
                float HdotV = max(dot(H, V), 0.0f);
                float LdotH = max(dot(L, H), 0.0f);
                float LdotV = max(dot(L, V), 0.0f);
                
                float3 F0 = lerp(0.04, albedo, metalness);
                float F = FresnelSchlick(NdotV, F0);

                float3 kd = lerp((float3)1 - F, (float3)0, metalness);
                float3 envDiffuse = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, normal, UNITY_SPECCUBE_LOD_STEPS), unity_SpecCube0_HDR);
                float3 envSpecular = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, R, roughness * UNITY_SPECCUBE_LOD_STEPS), unity_SpecCube0_HDR);

                float2 BRDFLUT = tex2D(_BRDF, float2(NdotV, roughness)).rg;

                float3 albedoLinear = pow(albedo, 2.2);

                // cook torrance
                float D = NdfGGX(NdotH, roughness);
                float G = GaSchlickGGX(NdotL, NdotV, roughness);

                // unreal engine BRDF
                float3 diffuseBRDF = albedoLinear * kd / 3.1415;
                float3 specularBRDF = F * G * D / max(0.001, 4.0 * NdotL * NdotV);

                float3 BRDF = (diffuseBRDF + specularBRDF) * NdotL * shadow;

                float3 diffuseIBL = kd * pow(envDiffuse, 2.2) * albedoLinear / 3.1415;
                float3 specularIBL = (F0 * BRDFLUT.x + BRDFLUT.y) * pow(envSpecular, 2.2);

                float3 IBL = diffuseIBL + specularIBL;

                float4 color = float4(radiance * BRDF + IBL * ambientOcclusion, alpha);
                
                return pow(color, 1.0 / 2.2);
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
    FallBack "Diffuse"
}

Shader "Custom/PBR"
{
    Properties
    {
        _Albedo ("Albedo (RGB)", 2D) = "white" {}
        _ARM ("AO Roughness MEtalness (ARM)", 2D) = "white" {}
        _Normal ("Normal", 2D) = "white" {}
        _BRDF ("BRDF LUT", 2D) = "white" {}
        _Cube("Reflection Map", CUBE) = "" {}
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
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
                float4 tangent : TANGENT;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldViewDir : TEXCOORD2;
                float2 texcoord : TEXCOORD3;
                float4 tangent : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                SHADOW_COORDS(6)
                //UNITY_FOG_COORDS(7)  // Add fog coordinates
            };

            v2f vert (appdata v) {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldViewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.screenPos = ComputeScreenPos(o.vertex);
                //o.worldRefl = reflect(-o.worldViewDir, o.normal);
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

            float4 frag (v2f i) : SV_Target {

                fixed shadow = SHADOW_ATTENUATION(i);

	            float3 radiance = _LightColor0;
                float L = normalize(_WorldSpaceLightPos0.xyz);
                float V = normalize(i.worldViewDir);
                float H = normalize(V + L);
                float N = normalize(i.normal);

                float NdotL = max(dot(N, L), 0.0f);
                float NdotV = max(dot(N, V), 0.0f);
                float NdotH = max(dot(N, H), 0.0f);
                float HdotV = max(dot(H, V), 0.0f);
                float LdotH = max(dot(L, H), 0.0f);

                float3 albedo = tex2D(_Albedo, i.texcoord);
                float ambientOcclusion = tex2D(_ARM, i.texcoord).r;
                float roughness = tex2D(_ARM, i.texcoord).g;
                float metalness = tex2D(_ARM, i.texcoord).b;

                float3 F0 = lerp(0.04, albedo, metalness);
                
                // Frostbite Diffuse BRDF (Normalized Disney model)
                float energyBias = lerp(0.0, 0.5, roughness);
                float energyFactor = lerp(1.0, 1.0 / 1.51, roughness);
                float Fd90 = energyBias + 2.0 * LdotH * LdotH * roughness;
                float3 FL = FresnelSchlick(NdotL, float3(1.0, 1.0, 1.0), Fd90);
                float3 FV = FresnelSchlick(NdotV, float3(1.0, 1.0, 1.0), Fd90);

	            // cook torrance
                float F = FresnelSchlick(NdotV, F0);
                float D = NdfGGX(NdotH, roughness);
                float G = GaSchlickGGX(NdotL, NdotV, roughness);

	            // unreal engine BRDF
	            float3 kd = lerp((float3)1 - F, (float3)0, metalness);

                float3 diffuseBRDF = albedo * kd  * FL * FV / 3.1415;
                float3 specularBRDF = F * G * D / (4 * NdotL * NdotV);

                float3 BRDF = (diffuseBRDF + specularBRDF) * radiance * NdotL * shadow;

                float R = reflect(-i.worldViewDir, i.normal);

                float4 envDiffuse = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, N, UNITY_SPECCUBE_LOD_STEPS);
                float4 envSpecular = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, R, roughness * UNITY_SPECCUBE_LOD_STEPS);

                float2 BRDFLUT = tex2D(_BRDF, float2(NdotV, roughness)).rg;

                float3 diffuseIBL = kd * envDiffuse * albedo / 3.1415;
                float3 specularIBL = (F0 * BRDFLUT.x + BRDFLUT.y) * envSpecular;

                float3 IBL = diffuseIBL + specularIBL;

                float4 color = float4(BRDF + IBL * ambientOcclusion, 1);
                
                //UNITY_APPLY_FOG(i.fogCoord, color);  // Apply fog to the color
                return color;
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

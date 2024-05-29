Shader "ReV3nus/OceanShader"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" {}
        _MainColor ("Color", Color) = (1,1,1,1)
        _Metallicness("Metallicness",Range(0,1)) = 0
        _Glossiness("Smoothness",Range(0,1)) = 1
        
        //_Amplitude("Amplitude", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityStandardBRDF.cginc"////dotclamped
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            struct VertexData {
                float4 position : POSITION;
                float4 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct FragmentData {
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 position : SV_POSITION;
            };

            
            // sampler2D _MainTex;
            // float4 _MainTex_ST;
            float4 _MainColor;
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            float _Glossiness;
            float _Metallicness;
            //float _Amplitude;


            float Pow2(float x)
            {
                return x*x;
            }
             float Pow5(float x)
            {
                return x*x*x*x*x;
            }
            float3 Schlick_F(half3 R, half cosA)
            {
                float3 F = R + (1-R) * Pow5(1 - cosA);
                return F;
            }
            float GGX_D(float roughness, float NdotH)
            {
                float D = Pow2(roughness) / (3.1415926535897932384626433832795 * (Pow2(1 + Pow2(NdotH) * (Pow2(roughness) - 1))));
                return D;
            }
            float CookTorrence_G (float NdotL, float NdotV, float VdotH, float NdotH){
                float G = 1;
                G = min(G, 2 * NdotH * NdotV / VdotH);
                G = min(G, 2 * NdotH * NdotL / VdotH);
                return G;
            }

            FragmentData vert (VertexData v)
            {
                FragmentData o;
                o.position = UnityObjectToClipPos(v.position);
                o.worldPos = mul(unity_ObjectToWorld, v.position).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv=v.uv;//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            UnityIndirect GetUnityIndirect(float3 lightColor, float3 lightDirection, float3 normalDirection,float3 viewDirection, float3 viewReflectDirection, float attenuation, float roughness, float3 worldPos){
                //// Set UnityLight
                UnityLight light;
                light.color = lightColor;
                light.dir = lightDirection;
                light.ndotl = saturate(dot( normalDirection, lightDirection));

                //// Set UnityGIInput
                UnityGIInput d;
                d.light = light;
                d.worldPos = worldPos;
                d.worldViewDir = viewDirection;
                d.atten = attenuation;
                d.ambient = 0.0h;
                d.boxMax[0] = unity_SpecCube0_BoxMax;
                d.boxMin[0] = unity_SpecCube0_BoxMin;
                d.probePosition[0] = unity_SpecCube0_ProbePosition;
                d.probeHDR[0] = unity_SpecCube0_HDR;
                d.boxMax[1] = unity_SpecCube1_BoxMax;
                d.boxMin[1] = unity_SpecCube1_BoxMin;
                d.probePosition[1] = unity_SpecCube1_ProbePosition;
                d.probeHDR[1] = unity_SpecCube1_HDR;

                //// Set EnvironmentData
                Unity_GlossyEnvironmentData ugls_en_data;
                ugls_en_data.roughness = roughness;
                ugls_en_data.reflUVW = viewReflectDirection;
                
                //// GetGI
                UnityGI gi = UnityGlobalIllumination(d, 1.0h, normalDirection, ugls_en_data );
                return gi.indirect;
            }

            fixed4 frag (FragmentData i) : SV_Target
            {
                //float4 mainTex = tex2D( _MainTex, i.uv );
                float4 mainTex = _MainColor;

                //// Vectors
                float3 L = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.worldPos.xyz,_WorldSpaceLightPos0.w));
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 H = Unity_SafeNormalize(L + V);
                float3 N = normalize(i.normal);
                
                float3 VR = Unity_SafeNormalize(reflect( -V, N ));

                //// Vector dot
                float NdotL = saturate( dot( N,L ));
                float NdotH = saturate( dot( N,H ));
                float NdotV = saturate( dot( N,V ));
                float VdotH = saturate( dot( V,H ));
                float LdotH = saturate( dot( L,H ));

                //// Light attenuation
                float attenuation = LIGHT_ATTENUATION(i);

                //// Indirect Global Illumination
                UnityIndirect gi =  GetUnityIndirect(_LightColor0.rgb, L, N, V, VR, attenuation, 1- _Glossiness, i.worldPos.xyz);

                //// Compute Roughness
                float perceptualRoughness = SmoothnessToPerceptualRoughness(_Glossiness);
                float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

                half oneMinusReflectivity;
                half3 specColor;
                float3 albedo = DiffuseAndSpecularFromMetallic (mainTex.rgb, _Metallicness, /*out*/ specColor, /*out*/ oneMinusReflectivity);

                //// BRDF
                //// 1. Diffuse term
                //// 1.1. Direct diffuse term
                float3 directDiffuse = albedo * DisneyDiffuse(NdotV, NdotL, LdotH, perceptualRoughness);
                //// 1.2. Indirect diffuse term
                float3 indirectDiffuse = gi.diffuse.rgb * albedo;
                
                //// 2. Specular term
                //// 2.1. Direct specular term
                float D = GGX_D(roughness, NdotH);
                float3 F = Schlick_F(specColor, LdotH);
                float G = CookTorrence_G(NdotL, NdotV, VdotH, NdotH);
                float3 directSpecular = (D * F * G) * UNITY_PI / (4 * (NdotL * NdotV));

                directSpecular = saturate(directSpecular);
                directDiffuse = saturate(directDiffuse);

                //// 2.2. Indirect specular term
                float grazingTerm = saturate(_Glossiness + (1-oneMinusReflectivity));
                float surfaceReduction = 1.0 / (roughness*roughness + 1.0);
                float3 indirectSpecular =  surfaceReduction * gi.specular * FresnelLerp (specColor, grazingTerm, NdotV);

                //// Sum up directLight and indirectLight
                float3 directLight = directDiffuse * NdotL + directSpecular * NdotL;
                float3 indirectLight =  indirectDiffuse + indirectSpecular;
                
                float4 color = float4(directLight * _LightColor0.rgb + indirectLight,1);
                color += float4( UNITY_LIGHTMODEL_AMBIENT.xyz * albedo,1);

                return color;
            }
            ENDCG
        }
    }
}

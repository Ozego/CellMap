Shader "Ozeg/Unlit/ParallaxStencil"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Depth ("Parallax Depth", Float ) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float2 pOffset: TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float1 _Depth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);

                float3x3 objectToTangent = float3x3
                (
                    normalize(v.tangent.xyz),
                    cross(v.normal, v.tangent.xyz) * v.tangent.w,
                    v.normal
                );
                o.pOffset = normalize(mul(objectToTangent,ObjSpaceViewDir(v.vertex))).xy;

                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 colOffset = tex2D(_MainTex, i.uv+i.pOffset*_Depth);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(colOffset.xyz,col.r);
                // return fixed4(i.tangentViewDirection,1.);
            }
            ENDCG
        }
    }
}

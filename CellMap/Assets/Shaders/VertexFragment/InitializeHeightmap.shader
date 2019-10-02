Shader "Ozeg/Blit/InitializeHeightmap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass
        { 
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require derivatives

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 inCol = tex2D(_MainTex, i.uv);
                fixed4 outCol = (0.).xxxx;
                outCol.r = ddx( inCol.r ) * 16.;
                outCol.g = ddy( inCol.r ) * 16.;
                outCol.b = length(outCol.rg) ;
                outCol.r = .5 - outCol.r;
                outCol.g = .5 - outCol.g;


                return outCol;
            }
            ENDCG
        }
    }
}

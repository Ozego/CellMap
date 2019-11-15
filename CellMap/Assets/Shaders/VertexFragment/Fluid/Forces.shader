Shader "Ozeg/Blit/Fluid/Forces"
{
    Properties
    {
        _MainTex  ("Texture", 2D) = "white" {}
        _ForceMap ("Texture", 2D) = "white" {}
        [HideInInspector] _Size ("Size", Vector) = (0.,0.,0.,0.)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D   _MainTex;
            sampler2D   _ForceMap;
            float4      _Size;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 f = tex2D(_ForceMap, i.uv/*+float2(0.,sin(_Time.y)*.25)*/)*2.-1.;
                return col+f*.01;
            }
            ENDCG
        }
    }
}

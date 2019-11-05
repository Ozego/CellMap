Shader "Ozeg/Blit/Fluid/PressureSubstraction"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            float4      _Size;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed n = tex2D(_MainTex, i.uv + float2( 0., _Size.y )).b;
                fixed e = tex2D(_MainTex, i.uv + float2( _Size.x, 0. )).b;
                fixed s = tex2D(_MainTex, i.uv - float2( 0., _Size.x )).b;
                fixed w = tex2D(_MainTex, i.uv - float2( _Size.x, 0. )).b;
                fixed4 col = tex2D(_MainTex, i.uv);
                // just invert the colors
                col.rg -= fixed2(e-w,n-s);
                return col;
            }
            ENDCG
        }
    }
}

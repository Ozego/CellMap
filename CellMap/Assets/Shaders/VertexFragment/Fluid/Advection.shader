Shader "Ozeg/Blit/Fluid/Advection"
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
                float dT = unity_DeltaTime.x;
                fixed2 U = tex2D(_MainTex, i.uv).xy*2.-1.;
                U /= 2.;
                fixed4 col = tex2D(_MainTex, i.uv-U*_Size.xy);
                return col;
            }
            ENDCG
        }
    }
}

Shader "Ozeg/Blit/Fluid/Pressure"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            float4      _Size;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 n = tex2D(_MainTex, i.uv + float2( 0., _Size.y ));
                fixed4 e = tex2D(_MainTex, i.uv + float2( _Size.x, 0. ));
                fixed4 s = tex2D(_MainTex, i.uv - float2( 0., _Size.x ));
                fixed4 w = tex2D(_MainTex, i.uv - float2( _Size.x, 0. ));
                fixed4 col = tex2D(_MainTex, i.uv);
                col.b = ((e.x - w.x) + (n.y - s.y))/2.;
                // col.x -= ddx(col.b);
                // col.y -= ddy(col.b);
                return col;
            }
            ENDCG
        }
    }
}

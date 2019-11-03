Shader "Ozeg/Unlit/SplitGradientTrigometric"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _p ("Sample Point", float) = 0.
        _A ("Vector A", Vector) = (1., 1., 1., 0.)
        _B ("Vector B", Vector) = (1., 1., 1., 0.)
        _C ("Vector C", Vector) = (1., 1., 1., 0.)
        _D ("Vector D", Vector) = (1., 1., 1., 0.)
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "TrigConst.cginc"
            #include "TrigFunction.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D   _MainTex;
            float4      _MainTex_ST;
            float       _p;
            fixed3      _A;
            fixed3      _B;
            fixed3      _C;
            fixed3      _D;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // float zoom = cos(max(0.,.5*_Time.y%4.-3.)*2.*PI+PI)+1.;
                // zoom *=.5;
                // i.uv -=.5;
                // i.uv*=1.+zoom;
                // i.uv +=.5;
                fixed4 col = tex2D(_MainTex, float2(i.uv.x, _p));
                if(i.uv.y < .5)
                {
                    col.rgb = TrigGrad
                    (
                        i.uv.x, 
                        _A, 
                        _B, 
                        _C, 
                        _D
                    );
                }
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

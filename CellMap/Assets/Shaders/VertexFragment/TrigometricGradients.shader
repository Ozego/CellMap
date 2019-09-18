Shader "Ozeg/Unlit/TrigometricGradients"
{
    Properties
    {
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

            fixed3 _A;
            fixed3 _B;
            fixed3 _C;
            fixed3 _D;
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            fixed3 trigGrad (float t, fixed3 a, fixed3 b, fixed3 c, fixed3 d)
            {
                return a + b * cos( TAU * ( c * t + d ) );
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0.,0.,0.,1.);
                col.rgb = trigGrad
                (
                    i.uv.y,
                    fixed3( 0.50 , 0.50 , 0.50 ),
                    fixed3( 0.50 , 0.50 , 0.50 ),
                    fixed3( 1.00 , 1.00 , 1.00 ),
                    fixed3( 0.00 , 0.33 , 0.67 )
                );
                if(i.uv.x > .025)
                {
                    col.rgb = trigGrad
                    (
                        i.uv.y, 
                        _A * i.uv.x , 
                        _B * i.uv.x * 1.5 , 
                        _C, 
                        _D
                    );
                }
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

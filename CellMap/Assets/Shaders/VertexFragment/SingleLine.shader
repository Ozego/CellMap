Shader "Ozeg/Unlit/SingleLine"
{
    Properties
    {
        _A ("Vector A", Vector) = (0., 0., 0., 0.)
        _B ("Vector B", Vector) = (1., 1., 1., 0.)
        _C ("Vector B", Vector) = (1., 1., 1., 0.)
        [PowerSlider(3.0)] _Thickness ("Thickness", Range (0.0001, .1)) = 0.1
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

            fixed2 _A;
            fixed2 _B;
            fixed4 _C;
            fixed _Thickness;

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

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed tLine = dot(i.uv+_C.zw,normalize(_C.xy));
                fixed2 tUV = tLine<0.?reflect(i.uv+_C.zw,normalize(_C.xy))-_C.zw:i.uv;
                fixed4 col = 
                fixed4
                ( 
                    ( tUV.x % 0.1 < _Thickness*.5 ) ? floor(tUV.x*10.)/10. : 0.,
                    ( tUV.y % 0.1 < _Thickness*.5 ) ? floor(tUV.y*10.)/10. : 0.,
                    ( tUV.x % 0.1 < _Thickness )&( tUV.y % 0.1 < _Thickness ) ? 1. : 0. ,
                    1.
                );
                fixed h = SDFLine(tUV, _A, _B); 
                col += h %.1<_Thickness?1.:0.;
                col.rg += .5+fixed2(ddx(h),ddy(h))*256. ;
                col.b += h;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
                // return fixed4(tLine,0.,0.,1.);
            }
            ENDCG
        }
    }
}

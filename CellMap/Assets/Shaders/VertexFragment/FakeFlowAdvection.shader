Shader "Ozeg/Unlit/FakeFlowAdvection"
{
    Properties
    {
        //expose parameters for animation
        [Toggle(USE_MASK)] _UseMask ("Use Texture Mask", Float) = 0
        _MainTex    ("Texture", 2D) = "white" {}
        _Noise      ("Texture", 2D) = "grey" {}
        _Scroll     ("Scroll", Float) = 0.
        _Speed      ("Speed", Float) = 0.
        _Flow       ("Flow", Float) = 0.
        _Displace   ("Displacement", Float) = 0.
        _AnimVec    ("Animation Vectors", Vector) = (0.,1.,1.,1.)

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
            #pragma shader_feature USE_MASK

            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Noise;
            float4 _Noise_ST;
            float4 _AnimVec;
            float _Scroll;
            float _Speed;
            float _Flow;
            float _Displace;

            float _Intensity;
            float _Contrast;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            //remove unused functions
            fixed mod(fixed t, fixed m) { t%=m; t+=m; t%=m; return t; }
            fixed cone(half2 U) { return 1.-length(U);}
            fixed cubicPulse( fixed x )
            {
                x = abs(x);
                return 1.-x*x*(3.-2.*x);
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0.,0.,0.,1.);
                half2 tOffset = _Scroll * _Noise_ST.zw * _Time.x;
                fixed4 advection = tex2D(_Noise,i.uv+tOffset*_Noise_ST.xy+_Noise_ST.zw) ;
                advection -= .5;
                advection *= .1 * _Flow;
                advection.rg *= _AnimVec.zw;
                half2 U = i.uv + tOffset * _Noise_ST.xy + _Noise_ST.zw;
                half t  = _Speed * _Time.x;
                t -= dot(i.uv, _AnimVec.xy);
                half t0 = (mod(t + 0.0, 2.)-1.);
                half t1 = (mod(t + 1.0, 2.)-1.);
                fixed4 d0 = tex2D( _Noise, U - advection.rg * t0);
                fixed4 d1 = tex2D( _Noise, U - advection.rg * t1);
                fixed4 displacement = lerp(d0,d1,cubicPulse(t1));
                displacement-=.5;
                displacement*=.5;
                fixed4 tex = tex2D(_MainTex,i.uv-displacement*_Displace);
                col.rgb += tex.rgb;
                // col.rg += displacement.ba;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

Shader "Ozeg/Unlit/FakeFlow"
{
    Properties
    {
        //expose parameters for animation
        [Toggle(USE_MASK)] _UseMask ("Use Texture Mask", Float) = 0
        _MainTex ("Texture", 2D) = "white" {}
        _Noise ("Texture", 2D) = "grey" {}
        _Scroll ("Scroll", Float) = 0.
        _Speed ("Speed", Float) = 0.
        _Flow ("Flow", Float) = 0.
        _Displacement ("Displacement", Float) = 0.
        _Size ("Size", Float) = 0.
        _Intensity ("Intensity", Float) = 0.
        _Contrast ("Contrast", Float) = 0.
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
            float _Scroll;
            float _Speed;
            float _Flow;
            float _Displacement;
            float _Size;
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
            fixed mod1(fixed t) { t%=1.; t+=1.; t%=1.; return t; }
            fixed2 mod1(fixed2 t) { return fixed2(mod1(t.x),mod1(t.y)); }
            fixed saw(fixed t, fixed i, fixed c) { return mod1(t+i/c)*2.-1.; }
            fixed transparency(fixed t ) { return smoothstep(0.,1.,min(1.,1.5*(1.-abs(t)))); }
            fixed checker(half2 U) { return .5 * abs(sign(mod1(U.x)-.5)+sign(mod1(U.y)-.5 ));}
            fixed cone(half2 U) { return 1.-length(U);}
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0.,0.,0.,1.);
                fixed2 tOffset = _Time.x*fixed2(_Scroll/13.,-_Scroll);

                fixed4 vNoise = tex2D(_Noise,i.uv+tOffset*_Noise_ST.xy+_Noise_ST.zw); vNoise-=.5; 
                vNoise.ga*=-1; 
               
                fixed3 t = fixed3
                (
                    saw(_Time.x*_Speed-i.uv.y,0.,3.), 
                    saw(_Time.x*_Speed-i.uv.y,1.,3.), 
                    saw(_Time.x*_Speed-i.uv.y,2.,3.)
                );
                _Flow*=1.-i.uv.y; //TODO: Mask _Flow with B channel
                fixed4 dNoise = 
                (
                    -.333333+tex2D(_Noise,i.uv*_Noise_ST.xy+_Noise_ST.zw+t.x*vNoise.rg*_Flow+tOffset)*transparency(t.x)+
                    -.333333+tex2D(_Noise,i.uv*_Noise_ST.xy+_Noise_ST.zw+t.y*vNoise.rg*_Flow+tOffset)*transparency(t.y)+
                    -.333333+tex2D(_Noise,i.uv*_Noise_ST.xy+_Noise_ST.zw+t.z*vNoise.rg*_Flow+tOffset)*transparency(t.z)
                ); 
                dNoise.ga*=-1;
                #if USE_MASK
                    _Displacement*=i.uv.y; //TODO: Mask _Displacement with B channel
                    fixed4 c = smoothstep(1.-_Intensity/2.-0.5/_Contrast,1.-_Intensity/2.+0.5/_Contrast,tex2D(_MainTex,i.uv/_Size+dNoise.rg*_Displacement));
                    col.rgb += c.rgb;   
                #else
                    i.uv-=.5; i.uv*=2.;
                    fixed c = smoothstep(1.-_Intensity/2.-0.5/_Contrast,1.-_Intensity/2.+0.5/_Contrast,cone(i.uv/_Size+dNoise.rg*_Displacement));
                    col.rgb += c;   
                #endif
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

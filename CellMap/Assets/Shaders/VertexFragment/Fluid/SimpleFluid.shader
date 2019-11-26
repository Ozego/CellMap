Shader "Ozeg/Blit/Fluid/SimpleFluid"
{
    Properties
    {
        _Blue      ( "BlueNoise",       2D )    = "grey" {}
        [HideInInspector] _Intensity            ( "", Float ) = 0
        [HideInInspector] _MainTex              ( "", 2D )    = "white" {}
        [HideInInspector] _Mask                 ( "", 2D )    = "white" {}
        [HideInInspector] _VecMap               ( "", 2D )    = "white" {}
        [HideInInspector] _FlowAdvection        ( "", Float ) = 0
        [HideInInspector] _FlowDiffusion        ( "", Float ) = 0
        [HideInInspector] _PressureDissipation  ( "", Float ) = 0
        [HideInInspector] _PressureVelocity     ( "", Float ) = 0
        [HideInInspector] _Frame                ( "", Float ) = 0
        [HideInInspector] _Size                 ( "", Vector ) = (0,0,0,0)
    }
    CGINCLUDE
        fixed  mod1(fixed t) { t%=1.; t+=1.; t%=1.; return t; }
        fixed2 mod1(fixed2 t) { return fixed2(mod1(t.x),mod1(t.y)); }
        fixed4 mod2D (sampler2D tex, half2 uv) { return tex2D(tex,mod1(uv)); }
        fixed2 rand2D(fixed t){ return (sin(fixed2(t * 12.9898, t * 78.233))* 43758.5453)%1.; }
    ENDCG
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass //Initialize flow
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            sampler2D _MainTex;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.b += .5;
                return col;
            }
            ENDCG
        }
        Pass //Advection
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float       _Frame;
            half        _Intensity;
            half        _FlowAdvection;
            half        _FlowDiffusion;
            half4       _Size;
            sampler2D   _Blue;
            sampler2D   _MainTex;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0.,0.,0.,1.);

                fixed2 C =  mod2D( _MainTex, i.uv ).xy;                                    C -= .5;  C *= 2.;
                fixed2 sE = mod2D( _MainTex, i.uv - _Size.xy * ( C + half2( 1., 0. ) ) ); sE -= .5; sE *= 2.;
                fixed2 sW = mod2D( _MainTex, i.uv - _Size.xy * ( C + half2(-1., 0. ) ) ); sW -= .5; sW *= 2.;
                fixed2 sN = mod2D( _MainTex, i.uv - _Size.xy * ( C + half2( 0., 1. ) ) ); sN -= .5; sN *= 2.;
                fixed2 sS = mod2D( _MainTex, i.uv - _Size.xy * ( C + half2( 0.,-1. ) ) ); sS -= .5; sS *= 2.;

                fixed2 vecField = (sN+sS+sE+sW)/4.;
                fixed4 n = mod2D(_Blue, (i.uv-rand2D(_Frame)*31./128.) * _Size.zw / half2(128.,128.)); n += _Frame*127./256.; n %=1.; 
                col = mod2D(_MainTex,i.uv - _Size.xy * vecField * _FlowAdvection * n.rg);
                vecField /= 2.; vecField += .5;
                // col.rg += vecField; col.rg /= 2.;
                col.rg = lerp(col.rg,vecField,_FlowDiffusion);
                return col;
            }
            ENDCG
        }
        Pass //Calculate Pressure
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            half4       _Size;
            half        _PressureDissipation;
            sampler2D   _MainTex;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = mod2D(_MainTex, i.uv);
                fixed2 E = mod2D(_MainTex, i.uv - _Size.xy * half2( 1., 0.) ); E -= .5;
                fixed2 W = mod2D(_MainTex, i.uv - _Size.xy * half2(-1., 0.) ); W -= .5;
                fixed2 N = mod2D(_MainTex, i.uv - _Size.xy * half2( 0., 1.) ); N -= .5;
                fixed2 S = mod2D(_MainTex, i.uv - _Size.xy * half2( 0.,-1.) ); S -= .5;
                col.b *= _PressureDissipation;
                col.b += (E.x-W.x+N.y-S.y);
                return col;
            }
            ENDCG
        }
        Pass //Substract Pressure
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float       _Frame;
            half4       _Size;
            half        _FlowAdvection;
            half        _PressureVelocity;
            sampler2D   _MainTex;
            sampler2D   _Blue;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = mod2D(_MainTex, i.uv);

                fixed E = mod2D(_MainTex, i.uv - _Size.xy * ( half2( 1., 0.))).b; E -= .5; 
                fixed W = mod2D(_MainTex, i.uv - _Size.xy * ( half2(-1., 0.))).b; W -= .5; 
                fixed N = mod2D(_MainTex, i.uv - _Size.xy * ( half2( 0., 1.))).b; N -= .5; 
                fixed S = mod2D(_MainTex, i.uv - _Size.xy * ( half2( 0.,-1.))).b; S -= .5; 
                fixed4 n = mod2D(_Blue, (i.uv-rand2D(_Frame)*31./128.) * _Size.zw / half2(128.,128.)); n += _Frame*127./256.; n %=1.; 
                col.x -= _PressureVelocity*(W-E)/4.*n.x;
                col.y -= _PressureVelocity*(S-N)/4.*n.y;
                col.b = (E+W+N+S)/4.+.5;
                return col;
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(i.uv,0.,1.);
                return col;
            }
            ENDCG
        }
        Pass //Advect UV map
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float     _Frame;
            half      _Intensity;
            half      _FlowAdvection;
            half      _FlowDiffusion;
            half4     _Size;
            sampler2D _MainTex;
            sampler2D _Blue;
            sampler2D _VecMap;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed rsqrt2 = 0.7071067811865475;
                half4 col = fixed4(0.,0.,0.,1.);

                half2 C = mod2D( _VecMap, i.uv ).xy;             C -= .5; C *= 2.;
                half2 S = mod2D( _VecMap, i.uv - _Size.xy * C ); S -= .5; S *= 2.;
                _Frame+=7.;fixed4 n = mod2D(_Blue, (i.uv-rand2D(_Frame)*31./128.) * _Size.zw / half2(128.,128.)); n += _Frame*127./256.; n %=1;
                half2 U = i.uv - _Size.xy * S * n.rg * _FlowAdvection;
                col += tex2D(_MainTex, U + _Size.xy * ( half2( 1., 1.)))*rsqrt2;
                col += tex2D(_MainTex, U + _Size.xy * ( half2(-1., 1.)))*rsqrt2;
                col += tex2D(_MainTex, U + _Size.xy * ( half2( 1.,-1.)))*rsqrt2;
                col += tex2D(_MainTex, U + _Size.xy * ( half2(-1.,-1.)))*rsqrt2;
                col += tex2D(_MainTex, U + _Size.xy * ( half2( 1., 0.)));
                col += tex2D(_MainTex, U + _Size.xy * ( half2(-1., 0.)));
                col += tex2D(_MainTex, U + _Size.xy * ( half2( 0., 1.)));
                col += tex2D(_MainTex, U + _Size.xy * ( half2( 0.,-1.)));
                col /= 6.8284271247461;
                col += tex2D(_MainTex, U);
                col /= 2.;
                return col;
            }
            ENDCG
        }
        Pass //Save
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            sampler2D _MainTex;
            sampler2D _VecMap;
            fixed4 frag (v2f i) : SV_Target
            {
                half4 col = fixed4(0.,0.,0.,1.);
                col.rg = tex2D( _VecMap,  i.uv ).xy;
                col.ba = ( tex2D( _MainTex, i.uv ).xy-i.uv ) * 2. + .5;
                return col;
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            float       _Frame;
            half4       _Size;
            sampler2D   _MainTex;
            sampler2D   _Blue;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 n = mod2D(_Blue, (i.uv-rand2D(_Frame)*31./128.) * _Size.zw / half2(128.,128.)); n += _Frame*127./256.; n %=1.;  n+=.5;
                return n;
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION;    float2 uv : TEXCOORD0; };
            struct v2f {     float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            sampler2D _MainTex;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = mod2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
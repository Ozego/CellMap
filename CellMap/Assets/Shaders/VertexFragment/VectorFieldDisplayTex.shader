Shader "Ozeg/Unlit/VectorFieldDisplayTex"
{
    Properties
    {
        [HideInInspector] _Size       ("", Vector ) = (0,0,0,0)
        [HideInInspector] _dTex       ("Display Texture", 2D) = "white" {}
        [HideInInspector] _VecMap   ("Vector Field", 2D)    = "white" {}
        [HideInInspector] _MainTex    ("Texture", 2D) = "white" {}
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
            half4 _Size;
            sampler2D _VecMap;
            sampler2D _dTex;
            sampler2D _MainTex;
            float4 _MainTex_ST;

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
                fixed2 vec = tex2D(_MainTex, i.uv).xy;
                fixed2 C = tex2D( _VecMap, i.uv ).xy;             C -= .5; C *= 2.;
                fixed2 S = tex2D( _VecMap, i.uv - _Size.xy * C ); S -= .5; S *= 2.;
                fixed4 col = fixed4(0.,0.,0.,1.);
                // col += tex2D(_dTex, vec - _Size.xy * (C+S+ half2( 2., 0.))); 
                // col += tex2D(_dTex, vec - _Size.xy * (C+S+ half2(-2., 0.))); 
                // col += tex2D(_dTex, vec - _Size.xy * (C+S+ half2( 0., 2.))); 
                // col += tex2D(_dTex, vec - _Size.xy * (C+S+ half2( 0.,-2.)));
                // col += tex2D(_dTex, vec - _Size.xy * (C+S));
                // col += tex2D(_dTex, vec - _Size.xy * C);
                col += tex2D(_dTex, vec);
                // col/=7.;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

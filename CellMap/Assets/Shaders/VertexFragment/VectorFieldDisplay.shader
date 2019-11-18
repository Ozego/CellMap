Shader "Ozeg/Unlit/VectorFieldDisplay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
                fixed2 t = fixed2(sin(0.25),cos(0.25));
                fixed3 A1 = fixed3( 0.4, 0.4, 0.4 );
                fixed3 B1 = fixed3( 0.1, 0.1, 0.1 );
                fixed3 C1 = fixed3( 1.0, 1.0, 1.0 );
                fixed3 D1 = fixed3( 1.0, 0.1, 0.2 );
 
                fixed3 A2 = fixed3( 0.2, 0.0,-0.2 );
                fixed3 B2 = fixed3( 0.6, 0.6, 0.6 );
                fixed3 C2 = fixed3( 1.0, 0.8, 0.8 );
                fixed3 D2 = fixed3( 0.6, 0.8, 0.9 ); 

                // sample the texture
                fixed2 ld1 = fixed2( t.x,-t.y );
                fixed2 ld2 = fixed2( t.x, t.y );
                fixed4 col = fixed4(0.,0.,0.,1.);
                fixed4 vec = tex2D(_MainTex, i.uv);
                vec.xy -= .5;
                col.rgb += TrigGrad(.5+.5*dot(vec.xy,ld1),A1,B1,C1,D1)* (1.-vec.b);
                col.rgb += TrigGrad(.5+.5*dot(vec.xy,ld2),A2,B2,C2,D2)* vec.b;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

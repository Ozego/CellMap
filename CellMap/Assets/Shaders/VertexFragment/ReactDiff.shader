Shader "Ozeg/Blit/ReactDiff"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _dXYFK ("diffuse XY, Kill, Feed", Vector) = (0.,0.,0.,0.)
        [HideInInspector] _Size ("Size", Vector) = (0.,0.,0.,0.)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
            float4 _Size;
            sampler2D _MainTex;
            fixed4 Convolve9x9(sampler2D map, float2 uv, fixed c, fixed n, fixed ne, fixed e, fixed se, fixed s, fixed sw, fixed w, fixed nw)
            {
                fixed4 o = c * tex2D( map, uv );
                o += nw * tex2D( map, uv + float2( -_Size.x,  _Size.y ) );
                o += n  * tex2D( map, uv + float2(  0.     ,  _Size.y ) );
                o += ne * tex2D( map, uv + float2(  _Size.x,  _Size.y ) );
                o += e  * tex2D( map, uv + float2(  _Size.x,  0.      ) );
                o += se * tex2D( map, uv + float2(  _Size.x, -_Size.y ) );
                o += s  * tex2D( map, uv + float2(  0.     , -_Size.y ) );
                o += sw * tex2D( map, uv + float2( -_Size.x, -_Size.y ) );
                o += w  * tex2D( map, uv + float2( -_Size.x,  0.      ) );
                return o;
            }
        ENDCG

        Pass
        {
            CGPROGRAM
                #include "UnityCG.cginc"
                #pragma vertex vert
                #pragma fragment frag


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

                fixed4 _dXYFK;

                fixed4 frag (v2f i) : SV_Target
                {
                    fixed4  map     = tex2D( _MainTex, i.uv );
                    fixed4  diff    = fixed4(_dXYFK.x,_dXYFK.y,0.0,0.0) * Convolve9x9(_MainTex,i.uv,-1.,.2,.04,.2,.06,.2,.06,.2,.04);
                    fixed   react   = map.r * map.g * map.g;
                    fixed   feed    = _dXYFK.z, 
                            kill    = _dXYFK.w;
                    map.r += 0.5*(diff.r - react + feed * ( 1. - map.r ));
                    map.g += 0.5*(diff.g + react - ( kill + feed ) * map.g);
                    map.b = 0.;
                    map.a = 0.;
                    return map;
                }
            ENDCG
        }
    }
}

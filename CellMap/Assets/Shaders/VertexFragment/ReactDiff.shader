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
            #define PI 3.14159265358979323846
            float4 _Size;
            sampler2D _MainTex;
            float cubicPulse( float w, float x )
            {
                x = abs(x);
                if( x>w ) return 0.0;
                x /= w;
                return 1.0 - x*x*(3.0-2.0*x);
            }
            fixed4 cubicBlur(sampler2D map, float2 uv, float radius)
            {
                fixed4 o = fixed4(0.,0.,0.,0.);
                fixed sum = 0.; // todo: analytical integral will be faster
                for( float x = -radius; x <= radius; x++ )
                {
                    for( float y = -radius; y <= radius; y++ )
                    {
                        float w = cubicPulse(radius,1.-length(float2(x,y)));
                        sum += w;
                        o += w * tex2D( map, uv + float2( x * _Size.x,  y * _Size.y ) );
                    }
                }
                // float vol = PI*.33333;
                o /= sum;
                return o;
            }
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
                    float U = -length(i.uv-.5);
                    fixed4  map     = tex2D( _MainTex, i.uv );
                    fixed4  diff    = fixed4(_dXYFK.x , _dXYFK.y ,0.0,0.0) * (cubicBlur(_MainTex, i.uv, 3.)*1.0472-map);//Convolve9x9(_MainTex,i.uv,-1.,.2,.05,.2,.05,.2,.05,.2,.05);
                    fixed   react   = map.r * map.g * map.g;
                    fixed   feed    = _dXYFK.z + U*.05, 
                            kill    = _dXYFK.w + (i.uv.y-.5)*.04;
                    map.r += .25 * ( diff.r - react + feed * ( 1. - map.r ));
                    map.g += (.4 - U *.5) * ( diff.g + react - ( kill + feed ) * map.g);
                    map.b = 0.;
                    map.a = 0.;

                    return map;
                }
            ENDCG
        }
    }
}

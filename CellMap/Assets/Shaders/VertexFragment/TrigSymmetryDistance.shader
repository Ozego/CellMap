// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Ozeg/Unlit/TrigSymmetryDistance"
{
    Properties
    {
        [Space] [Header(Animation)]
        [KeywordEnum(Static,Rotation)] _Keyword ("Set Animation", Float) = 0
        [Space] [Header(Lighting)]
        _LightDir ("Light Direction", Vector) = (1., 1., 1., 0.)
        [Space] [Header(Color Function Matrix)]
        _A ("Vector A", Vector) = (1., 1., 1., 0.)
        _B ("Vector B", Vector) = (1., 1., 1., 0.)
        _C ("Vector C", Vector) = (1., 1., 1., 0.)
        _D ("Vector D", Vector) = (1., 1., 1., 0.)
        _Offset ("Animation Offset", Float) = 0
        _Speed ("Animation Speed", Float) = 1
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

            #pragma shader_feature _KEYWORD_ROTATION

            #include "UnityCG.cginc"
            #include "TrigConst.cginc"
            #include "TrigFunction.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                half3 worldNormal : TEXCOORD1;
                half3 worldTangent : TEXCOORD2;
                float xCell : TEXCOORD3;
            };

            fixed3 _A;
            fixed3 _B;
            fixed3 _C;
            fixed3 _D;
            fixed3 _LightDir;
            float _Offset;
            float _Speed;
            
            v2f vert (appdata v)
            {
                _Time *= _Speed;
                float angleZ = radians(_Time.z*TAU);
                float c = cos(angleZ);
                float s = sin(angleZ);
                float4x4 rotateZMatrix = float4x4
                ( 
                     c, -s, 0., 0.,
                     s,  c, 0., 0.,
                    0., 0., 1., 0.,
                    0., 0., 0., 1.
                );
                v2f o;
                o.xCell = 0;
                #if defined(_KEYWORD_ROTATION)
                    o.xCell = 2*floor(0.5-v.vertex.x*.5);
                    v.vertex = mul(rotateZMatrix,v.vertex+float4(o.xCell,.0,0.,0.))-float4(o.xCell,.0,0.,0.);
                    v.normal = mul(rotateZMatrix,v.normal);
                    v.tangent.xyz = mul(rotateZMatrix,v.tangent.xyz);
                #endif
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = .5+abs(v.uv%1.-.5);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                // output the tangent space matrix
                return o;
                // o.uv = (v.uv*.978)%1; // why .978?
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                _Time *= _Speed;

                fixed lightAttenuation = dot(normalize(_LightDir),i.worldNormal);
                fixed freshnel = dot(fixed3(0.,1.,0.),i.worldNormal);
                freshnel = pow(1.25-freshnel,8.);
                fixed2 dField = PolarLogCoordinates(i.uv.yx);
                dField = saturate(dField);
                fixed4 col = fixed4(0.,0.,0.,1.);
                col.rgb = TrigGrad
                (
                    (abs(dField-.5).x+lightAttenuation)/2.,
                    _A, 
                    _B, 
                    _C+cos(_Time.w+_Offset+i.xCell*.1), 
                    _D+sin(_Time.z+_Offset+i.xCell*.1)
                );
                col += pow(lightAttenuation,16.)+freshnel;
                // col *= dField.y *5.
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
                // return fixed4(i.xCell/8.,0.,0.,1.);
            }
            ENDCG
        }
    }
}

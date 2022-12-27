Shader "Unlit/Border_Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Main Color", Color) = (1,0,0,1)
        _Thickness("Thickness", Float) = 5.0
        _Offset("Offset", Float) = 0
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

            struct appdata
            {
                half4 vertex : POSITION;
                
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half4 wpos : TEXCOORD0;
            };

            sampler2D _MainTex;
            half4 _Color;
            half _Thickness;
            half _Offset;
            half4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float halfT = _Thickness / 2;
                clip((((abs(i.wpos.x + i.wpos.y + _Time.y * halfT + _Offset)) % -_Thickness) < halfT) - 1);
                return _Color + (_SinTime.w * (_SinTime.w > 0));
            }
            ENDCG
        }
    }
}

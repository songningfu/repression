Shader "Hidden/SeeAPsychologist/DissociationGrayscale"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Amount ("Amount", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Amount;

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

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // luminance (Rec.601) for built-in pipeline friendly grayscale
                fixed gray = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, gray.xxx, saturate(_Amount));
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}


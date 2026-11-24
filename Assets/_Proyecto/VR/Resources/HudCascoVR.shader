// VR14: el panel curvo del HUD de casco. Muestra la textura donde la cámara del HUD dibujó los Canvas de PC (fondo con alfa 0).
// UI/Default (Unity 6) dibuja con alfa premultiplicado sobre el negro transparente: la textura queda premultiplicada y con el alfa
// correcto, así que aquí se mezcla premultiplicado (con UI/Default encima otra vez lo translúcido saldría más oscuro). Sin luz, sin
// profundidad, encima del mundo, y va bien en estéreo (multi-pass e instanced).
Shader "Hidden/MechaCyberpunk/HudCascoVR"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "black" {}
        _Opacidad ("Opacidad", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Opacidad;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Opacidad;
            }
            ENDCG
        }
    }
}

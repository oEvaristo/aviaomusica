Shader "TextMeshPro/Mobile/Distance Field"
{
    Properties
    {
        _MainTex ("Font atlas", 2D) = "white" {}
        _FaceColor ("Face", Color) = (1,1,1,1)
        _FaceDilate ("Dilate", Float) = 0
        _OutlineWidth ("Outline", Float) = 0
        _OutlineSoftness ("Outline softness", Float) = 0
        _OutlineColor ("Outline color", Color) = (0,0,0,1)
        _WeightNormal ("Normal weight", Float) = 0
        _WeightBold ("Bold weight", Float) = 0.5
        _GradientScale ("Gradient scale", Float) = 10
        _TextureWidth ("Width", Float) = 1024
        _TextureHeight ("Height", Float) = 1024
        _ScaleRatioA ("Ratio A", Float) = 1
        _ScaleRatioB ("Ratio B", Float) = 1
        _ScaleRatioC ("Ratio C", Float) = 1
        _ShaderFlags ("Flags", Float) = 0
        _StencilComp ("Stencil comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil operation", Float) = 0
        _StencilWriteMask ("Write mask", Float) = 255
        _StencilReadMask ("Read mask", Float) = 255
        _ColorMask ("Color mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float4 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 local : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float4 _FaceColor, _ClipRect;
            float _FaceDilate;
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.local = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv.xy;
                output.color = input.color * _FaceColor;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float distance = tex2D(_MainTex, input.uv).a;
                float softness = max(fwidth(distance), 0.002);
                float edge = .5 - _FaceDilate * .1;
                fixed4 color = input.color;
                color.a *= smoothstep(edge-softness, edge+softness, distance);
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}

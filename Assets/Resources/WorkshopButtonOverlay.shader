Shader "UI/Workshop Button Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                half4 tint : COLOR;
            };

            struct Interpolators
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 canvasPosition : TEXCOORD1;
                half4 tint : COLOR;
            };

            sampler2D _MainTex;
            half4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIVertexColorAlwaysGammaSpace;

            Interpolators Vertex(Attributes input)
            {
                Interpolators output;
                output.position = UnityObjectToClipPos(input.position);
                output.canvasPosition = input.position.xy;
                output.uv = input.uv;
                output.tint = input.tint;
                #ifndef UNITY_COLORSPACE_GAMMA
                if (_UIVertexColorAlwaysGammaSpace > 0.5)
                    output.tint.rgb = UIGammaToLinear(input.tint.rgb);
                #endif
                return output;
            }

            half4 Fragment(Interpolators input) : SV_Target
            {
                half alpha = (tex2D(_MainTex, input.uv).a + _TextureSampleAdd.a) * input.tint.a;
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.canvasPosition, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001h);
                #endif
                return half4(input.tint.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}

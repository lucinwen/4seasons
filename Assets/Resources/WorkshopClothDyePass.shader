Shader "FourSeasons/UI/ClothDye"
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 canvasPosition : TEXCOORD1;
                half4 tint : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            half4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIVertexColorAlwaysGammaSpace;

            Interpolators Vertex(Attributes input)
            {
                Interpolators output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
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
                half4 source = tex2D(_MainTex, input.uv) + _TextureSampleAdd;
                // Retain sewn edges and white needle marks. Neutralize only the blue fabric weave.
                half fabric = smoothstep(0.035h, 0.12h, source.b - source.r);
                half luminance = dot(source.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                #ifdef UNITY_COLORSPACE_GAMMA
                half neutral = saturate(0.27h + luminance);
                #else
                half neutral = saturate(0.27h + luminance * 1.45h);
                #endif
                half4 result = half4(lerp(source.rgb, neutral * input.tint.rgb, fabric), source.a * input.tint.a);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.canvasPosition, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001h);
                #endif
                result.rgb *= result.a;
                return result;
            }
            ENDCG
        }
    }
}

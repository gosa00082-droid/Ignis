Shader "Custom/UIBlurFullscreen"
{
    Properties
    {
        _BlurSize ("Blur Size", Float) = 2
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    float _BlurSize;

    struct BlurAttributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct BlurVaryings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    BlurVaryings Vert(BlurAttributes input)
    {
        BlurVaryings output;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);

        return output;
    }

    float4 FullScreenPass(BlurVaryings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 positionSS = input.positionCS.xy;
        positionSS.y = _ScreenSize.y - positionSS.y;

        float3 color = 0;

        color += CustomPassLoadCameraColor(positionSS + float2(-2, -2) * _BlurSize, 0).rgb * 0.025;
        color += CustomPassLoadCameraColor(positionSS + float2(-1, -2) * _BlurSize, 0).rgb * 0.05;
        color += CustomPassLoadCameraColor(positionSS + float2( 0, -2) * _BlurSize, 0).rgb * 0.075;
        color += CustomPassLoadCameraColor(positionSS + float2( 1, -2) * _BlurSize, 0).rgb * 0.05;
        color += CustomPassLoadCameraColor(positionSS + float2( 2, -2) * _BlurSize, 0).rgb * 0.025;

        color += CustomPassLoadCameraColor(positionSS + float2(-2, -1) * _BlurSize, 0).rgb * 0.05;
        color += CustomPassLoadCameraColor(positionSS + float2(-1, -1) * _BlurSize, 0).rgb * 0.075;
        color += CustomPassLoadCameraColor(positionSS + float2( 0, -1) * _BlurSize, 0).rgb * 0.1;
        color += CustomPassLoadCameraColor(positionSS + float2( 1, -1) * _BlurSize, 0).rgb * 0.075;
        color += CustomPassLoadCameraColor(positionSS + float2( 2, -1) * _BlurSize, 0).rgb * 0.05;

        color += CustomPassLoadCameraColor(positionSS + float2(-2,  0) * _BlurSize, 0).rgb * 0.075;
        color += CustomPassLoadCameraColor(positionSS + float2(-1,  0) * _BlurSize, 0).rgb * 0.1;
        color += CustomPassLoadCameraColor(positionSS + float2( 0,  0) * _BlurSize, 0).rgb * 0.125;
        color += CustomPassLoadCameraColor(positionSS + float2( 1,  0) * _BlurSize, 0).rgb * 0.1;
        color += CustomPassLoadCameraColor(positionSS + float2( 2,  0) * _BlurSize, 0).rgb * 0.075;

        color += CustomPassLoadCameraColor(positionSS + float2(-2,  1) * _BlurSize, 0).rgb * 0.05;
        color += CustomPassLoadCameraColor(positionSS + float2(-1,  1) * _BlurSize, 0).rgb * 0.075;
        color += CustomPassLoadCameraColor(positionSS + float2( 0,  1) * _BlurSize, 0).rgb * 0.1;
        color += CustomPassLoadCameraColor(positionSS + float2( 1,  1) * _BlurSize, 0).rgb * 0.075;
        color += CustomPassLoadCameraColor(positionSS + float2( 2,  1) * _BlurSize, 0).rgb * 0.05;

        color += CustomPassLoadCameraColor(positionSS + float2(-2,  2) * _BlurSize, 0).rgb * 0.025;
        color += CustomPassLoadCameraColor(positionSS + float2(-1,  2) * _BlurSize, 0).rgb * 0.05;
        color += CustomPassLoadCameraColor(positionSS + float2( 0,  2) * _BlurSize, 0).rgb * 0.075;
        color += CustomPassLoadCameraColor(positionSS + float2( 1,  2) * _BlurSize, 0).rgb * 0.05;
        color += CustomPassLoadCameraColor(positionSS + float2( 2,  2) * _BlurSize, 0).rgb * 0.025;

        return float4(color, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
        }

        Pass
        {
            Name "FullScreenPass"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FullScreenPass
            ENDHLSL
        }
    }

    Fallback Off
}

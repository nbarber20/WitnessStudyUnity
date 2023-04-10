Shader "WitnessStudy/GridShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Tile("Tiling", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        float4 _Color;
        float _Tile;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 Node_UV = IN.worldPos * _Tile;
            float3 Node_Blend = abs(IN.worldNormal);
            Node_Blend /= dot(Node_Blend, 1.0);
            float4 Node_X = tex2D(_MainTex, Node_UV.zy);
            float4 Node_Y = tex2D(_MainTex, Node_UV.xz);
            float4 Node_Z = tex2D(_MainTex, Node_UV.xy);

            o.Albedo.rgb = Node_X * Node_Blend.x + Node_Y * Node_Blend.y + Node_Z * Node_Blend.z;
            o.Albedo.rgb *= _Color;
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

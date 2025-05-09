Shader "Custom/TerrainHeightColor"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float3 worldPos; // World-space position
        };

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float y = IN.worldPos.y;
            fixed3 color;

            if (y < 20)
                color = fixed3(0.76, 0.70, 0.50); // Sand (tan)
            else if (y < 50)
                color = fixed3(0.13, 0.55, 0.13); // Grass (green)
            else if (y < 80)
                color = fixed3(0.5, 0.5, 0.5);     // Rock (grey)
            else
                color = fixed3(1.0, 1.0, 1.0);     // Snow (white)

            o.Albedo = color;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

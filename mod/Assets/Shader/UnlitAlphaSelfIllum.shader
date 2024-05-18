Shader "Unlit/AlphaSelfIllum" {
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)   
        _MainTex ("SelfIllum Color (RGB) Alpha (A)", 2D) = "white"
    }
 
    Category
    {
        Lighting On
        ZWrite On
        Cull back
        Blend SrcAlpha OneMinusSrcAlpha
        Tags {Queue=Transparent}
 
        SubShader
        {
            Tags {Queue=Transparent}
            Blend SrcAlpha OneMinusSrcAlpha
            Color  [_Color]
            Material
            {
                Emission [_Color]
                Ambient [_Color]
                Diffuse [_Color]
            }
 
            Pass
            {
                SetTexture [_MainTex]
                {
                    constantColor [_Color]
                    Combine texture * primary, texture * constant
                }
            }
        }
    }
}
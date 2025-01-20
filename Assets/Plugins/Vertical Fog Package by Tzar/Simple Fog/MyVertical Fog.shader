Shader "Tzar/MyVerticalFog"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
       _Color("Main Color", Color) = (1, 1, 1, .5)
       _Intensity("Intensity", float) = 1
    }
        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"  }

        Pass
        {
           Blend SrcAlpha OneMinusSrcAlpha
           ZWrite Off
           CGPROGRAM
           #pragma vertex vert
           #pragma fragment frag
           #pragma multi_compile_fog
           #include "UnityCG.cginc"
           
           struct appdata
           {
               float4 vertex : POSITION;
               UNITY_VERTEX_INPUT_INSTANCE_ID
           };

           struct v2f
           {
               float2 uv : TEXCOORD01;
               float4 scrPos : TEXCOORD0;
               UNITY_FOG_COORDS(1)
               float4 vertex : SV_POSITION;
               UNITY_VERTEX_OUTPUT_STEREO
           };



           UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
           sampler2D _MainTex;
           float4 _MainTex_ST;
           float4 _Color;
           float _Intensity;

           v2f vert(appdata v)
           {
               v2f o;

               UNITY_SETUP_INSTANCE_ID(v);
               UNITY_INITIALIZE_OUTPUT(v2f, o);
               UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

               
               o.vertex = UnityObjectToClipPos(v.vertex);
               o.scrPos = ComputeScreenPos(o.vertex);
               UNITY_TRANSFER_FOG(o,o.vertex);
               return o;
           }


            half4 frag(v2f i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

               float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)));
               float diff = saturate(_Intensity * (depth - i.scrPos.w));

               fixed4 col = lerp(fixed4(_Color.rgb, 0.0), _Color, diff * diff * diff * (diff * (6 * diff - 15) + 10));
               // fixed4 MainTex = tex2D(_MainTex, i.uv);
               //  col.a = MainTex.a;

                UNITY_APPLY_FOG(i.fogCoord, col);
               return col;
            }

           // void surf(Input IN, inout SurfaceOutputStandard o) 
           //  {
           //     fixed4 col = tex2D(_MainTex, IN.uv_MainTex);
           //   o.Alpha = col.a;
           //  }
            ENDCG
        }
    }
}
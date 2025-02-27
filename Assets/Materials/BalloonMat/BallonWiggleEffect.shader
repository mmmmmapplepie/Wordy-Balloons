Shader "Custom/BallonWiggleEffect"
{   
	 Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
				_Color ("Color", Color) = (1,1,1,1)
        _Bumps("Wobble Bumps", int) = 2
        _MaxDistortion("Distortion", Range(-1,1)) = 0.5
				_Rotation("Rot", Range(0,360)) = 0
    }
    SubShader
    {

			Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}

			ZTest LEqual

			Blend SrcAlpha OneMinusSrcAlpha
			// ZWrite Off
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            int _Bumps;
            float _MaxDistortion;
						fixed4 _Color;
						float _Rotation;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
								o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
							float minimumAlpha = 0.01;
							if (_Bumps < 2) {
								fixed4 c = tex2D(_MainTex, i.uv);
								c *= _Color ;
								if (c.a < minimumAlpha) {
									discard;
								}
								return c;
							}
							float2 centeredUV = i.uv - float2(0.5, 0.5);
							float r = length(centeredUV);
							float angle = atan2(centeredUV.y, centeredUV.x);
							float distortionR = r * (_MaxDistortion/(3*pow(_Bumps,0.9))) * cos(_Bumps * (angle+ radians(-_Rotation))) ;
							float2 distortionDir = float2(distortionR * cos(angle), distortionR * sin(angle));
							float2 targetUV = i.uv + distortionDir;
							targetUV = clamp(targetUV, 0, 1);

							fixed4 c = tex2D(_MainTex, targetUV);
							c *= _Color ;
							if (c.a < minimumAlpha) {
								discard;
							}
							return c;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
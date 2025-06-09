Shader "Custom/PinholeExtra"
{
    Properties
    {
				_MainTex ("Albedo (RGB)", 2D) = "white" {}

        _MaskColor ("MaskColor", Color) = (1,1,1,1)
        _BackgroundColor ("BackgroundColor", Color) = (1,1,1,1)
    }
		SubShader
    {

			Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}

			ZWrite Off
			ZTest Always

			Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
						
						#define MAX_SHAPES 8;
            sampler2D _MainTex;
						fixed4 _MaskColor;
						fixed4 _BackgroundColor;
						float4 _Shapes[MAX_SHAPES * 2];
						float4 _MainTex_TexelSize;
						int _ShapeCount;

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
								fixed4 c tex2D(_MainTex, i.uv) * _BackgroundColor;

								float finalMask = 0;

								for (int shapeIndex = 0; shapeIndex < _ShapeCount; shapeIndex++)
								{
										float4 data1 = _Shapes[shapeIndex * 2];     // x, y, width, height -- all in UV coordinates
										float4 data2 = _Shapes[shapeIndex * 2 + 1]; // rotation, fade(0 - no fade, 1 - maximum fade with fade distance = minimum among width/height), shapeType (0 is ellipse, 1 is rectangle), weightMask (clamped to 0, 1)
						
										float2 center = data1.xy;
										float2 dim = data1.zw;
						
										float rotation = data2.x;
										float fade = saturate(data2.y);
										int shapeType = (int)data2.z;
										float maskWeight = data2.w;

										if (maskWeight == 0) {continue;}
										if (dim.x == 0 || dim.y == 0) {continue;}
										if (shapeType > 1 || shapeType < 0) {continue;}

										float aspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
										center = UVScaledToAspect(center, aspect);
										float2 uv = i.uv;
										uv = UVScaledToAspect(uv, aspect);
										dim = float2(dim.x*aspect, dim.y);

										float maskAmount = GetMaskValue(shapeType, uv, center, dim, rotation, fade);
										finalMask += maskAmount * maskWeight;
								}

								finalMask /= _ShapeCount;
								//lerp between mask and background color depending on the mask weight.
								// float4 finalC = 






								return c;
            }

						float GetMaskValue(int shape,float2 uv, float2 center, float2 dim, float rot, float fade) {
							if (shape == 0) {
								return GetEllipseMask(uv, center, dim, rot, fade);
							} else if (shape == 1) {
								return GetRectangleMask(uv, center, dim, rot, fade);
							}
							return 0;
						}
						float GetEllipseMask(float2 uv, float2 center, float2 dim, float rot, float fade) {
							uv = RotateAround(uv, center, -rot);

							float2 diff = (uv - center) / dim;
							float dist = length(diff); // Ellipse: distance from center in normalized space
					
							float fadeDist = min(dim.x, dim.y) * fade; // in UV space
					
							float inner = 1.0 - (fadeDist / min(dim.x, dim.y)); // normalized inner radius
							float mask = 1.0 - smoothstep(inner, 1.0, dist);
					
							return mask;
						}
						float GetRectangleMask(float2 uv, float2 center, float2 dim, float rot, float fade) {
							uv = RotateAround(uv, center, -rot);

							float2 diff = abs(uv - center);
							float2 halfDim = dim * 0.5;
					
							float fadeDist = min(dim.x, dim.y); // UV-space fade distance
							float actualFade = fade * fadeDist;
					
							float2 inner = halfDim - actualFade;
					
							float2 mask = 1.0 - smoothstep(inner, halfDim, diff);
							return mask.x * mask.y;
						}

						float2 RotateAround(float2 point, float2 pivot, float angleDeg) {
							float angleRad = radians(angleDeg);
							float s = sin(angleRad);
							float c = cos(angleRad);
					
							float2 p = point - pivot;
							float2 rotated = float2(p.x * c - p.y * s, p.x * s + p.y * c);
							return rotated + pivot;
						}
						//making it so that it scaled with center at 0.5,0.5;
						float2 UVScaledToAspect(float2 uv, float aspect)
						{
								return float2((uv.x - 0.5) * aspect + 0.5, uv.y);
						}



            ENDCG
        }
    }
    FallBack "Diffuse"
}

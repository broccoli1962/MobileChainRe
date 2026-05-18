Shader "Custom/OutlineShader"
{
    Properties
    {
        _MainTex      ("Sprite Texture", 2D)         = "white" {}
        _Color        ("Tint",           Color)       = (1,1,1,1)
        _OutlineColor ("Outline Color",  Color)       = (1,1,0,1)
        // 외곽선 두께를 텍셀(픽셀) 단위로 지정 — 픽셀아트에서 정수 값 권장
        _OutlineWidth ("Outline Width (Pixels)", Range(0.5, 16)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;   // (1/w, 1/h, w, h)
            fixed4    _Color;
            fixed4    _OutlineColor;
            float     _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.color  = v.color * _Color;
                return o;
            }

            // 반지름 r(텍셀 단위)의 원 위에 sampleCount개 방향을 샘플해
            // 인접한 불투명 픽셀이 있으면 1을 반환
            float sampleRing(float2 uv, float2 texelSize, float r, int sampleCount)
            {
                float result = 0;
                float step = 6.28318530718 / (float)sampleCount;
                for (int i = 0; i < sampleCount; i++)
                {
                    float  angle  = step * i;
                    float2 offset = float2(cos(angle), sin(angle)) * r * texelSize;
                    result = max(result, tex2D(_MainTex, uv + offset).a);
                }
                return result;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv        = i.uv;
                float2 texelSize = _MainTex_TexelSize.xy;

                fixed4 sprite = tex2D(_MainTex, uv) * i.color;
                sprite.rgb   *= sprite.a;  // premultiply

                float alpha = sprite.a;

                // 완전 불투명 픽셀은 외곽선 처리 불필요
                if (alpha >= 0.99) return sprite;

                // -------------------------------------------------------
                // 원형 다중 링 샘플링
                //
                // 두꺼울수록 원을 더 잘게 나눠 다각형이 되지 않도록 함.
                //   - OutlineWidth <= 2  → 16 directions
                //   - OutlineWidth <= 4  → 24 directions
                //   - OutlineWidth <= 8  → 32 directions
                //   - OutlineWidth > 8   → 48 directions
                //
                // 추가로 바깥 링(R) + 안쪽 링(R×0.5)을 모두 샘플해
                // 픽셀아트처럼 오목하거나 얇은 영역도 빈틈 없이 커버.
                // -------------------------------------------------------
                int outerSamples;
                if      (_OutlineWidth <= 2.0) outerSamples = 16;
                else if (_OutlineWidth <= 4.0) outerSamples = 24;
                else if (_OutlineWidth <= 8.0) outerSamples = 32;
                else                           outerSamples = 48;

                float neighborAlpha = sampleRing(uv, texelSize, _OutlineWidth, outerSamples);

                // 두께 > 1이면 중간 링도 추가 (오목부/얇은 부분 누락 방지)
                if (_OutlineWidth > 1.0)
                    neighborAlpha = max(neighborAlpha,
                        sampleRing(uv, texelSize, _OutlineWidth * 0.5, outerSamples / 2));

                float outlineMask = step(0.01, neighborAlpha) * (1.0 - step(0.01, alpha));

                fixed4 outlineCol  = _OutlineColor;
                outlineCol.rgb    *= outlineCol.a;  // premultiply
                outlineCol        *= outlineMask;

                // 스프라이트 뒤에 외곽선 합성
                return sprite + outlineCol * (1.0 - alpha);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}

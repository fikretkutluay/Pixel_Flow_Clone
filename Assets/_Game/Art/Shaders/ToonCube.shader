// Referans oyunun gövde gölgelendirmesi, ekran görüntüsünden ölçülerek
// çıkarıldı (bkz. HANDOFF.md — "Ölç, göz kararı geçme"). Üç davranış:
//
//   1. Gölge grileşmiyor, DOYGUNLAŞIYOR. Turuncu gövde (244,188,120) gölgede
//      (158,99,58) oluyor — mavi kanal en çok kısılan. Nötr bir çarpım bunu
//      veremez, o yüzden doygunluk ayrı bir çarpan.
//   2. Işıklı tepe beyaza değil SARIYA kayıyor: (255,255,174). Kanal farkları
//      toplamsal bir sıcak katkıyla birebir tutuyor (+0.28 · (1,1,0.68)).
//   3. Kenarlar parlamıyor, KOYULAŞIYOR. Beyaz gövdenin kenarı 135/255 = 0.53.
//      Eski ToonCube_SG kenarlara sıcak fresnel EKLİYORDU — yıkanmış görüntünün
//      sebebi buydu.
//
// Ayrıca siluetin çevresinde saf siyah 1-2 px kontur var. Ters-kabuk (inverted
// hull) tekniğiyle ayrı bir pass olarak çiziliyor; Shader Graph tek graph'tan
// iki pass üretemediği için bu shader elle yazıldı.
Shader "PixelFlow/ToonCube"
{
    Properties
    {
        [MainColor] _BaseColor("Taban Rengi", Color) = (1, 1, 1, 1)

        [Header(Toon Rampasi)][Space(4)]
        _RampCenter("Rampa Merkezi", Range(-1, 1)) = 0.0
        _RampSmooth("Rampa Yumusakligi", Range(0.001, 1)) = 0.35
        _ShadowValue("Golge Parlakligi", Range(0, 1)) = 0.55
        _ShadowSat("Golge Doygunlugu", Range(1, 2)) = 1.3
        _ShadowTint("Golge Ortam Tonu", Color) = (0.35, 0.37, 0.60, 1)
        _ShadowTintAmount("Golge Ortam Miktari", Range(0, 1)) = 0.18

        [Header(Isik Bandi)][Space(4)]
        _HighlightColor("Isik Rengi", Color) = (1, 1, 0.68, 1)
        _HighlightAmount("Isik Miktari", Range(0, 1)) = 0.28
        _HighlightStart("Isik Baslangici", Range(-1, 1)) = 0.45
        _HighlightSmooth("Isik Yumusakligi", Range(0.001, 1)) = 0.25

        [Header(Yuzey Gradyani)][Space(4)]
        // Duz yuzlu mesh'lerde (kup) N.L tum yuze ayni dusuyor ve yuz TEK bir
        // renge cakiliyor. Referansta hicbir kup duz degil: yuzey tepeden asagi
        // ~%12 aciliyor (kahverengi kup 139->156, mor kup 77->87). Yan yana
        // dizilen duz saf renk alanlari "cirtlak" okunmasinin ana sebebi.
        _FaceGradient("Yuzey Gradyani", Range(0, 0.6)) = 0
        _FaceExtent("Mesh Yarim Yuksekligi (object space)", Range(0.01, 4)) = 0.5

        [Header(Kenar)][Space(4)]
        _EdgeDarken("Kenar Karartma", Range(0, 1)) = 0.47
        _EdgePower("Kenar Keskinligi", Range(0.5, 8)) = 2.5

        [Header(Kontur)][Space(4)]
        [Toggle(_OUTLINE_ON)] _OutlineEnabled("Kontur Acik", Float) = 0
        _OutlineColor("Kontur Rengi", Color) = (0, 0, 0, 1)
        _OutlineWidth("Kontur Kalinligi (piksel)", Range(0, 12)) = 2.5
        // Benek kaliyorsa BUYUT. Cok buyurse kontur yakin yuzeylerin arkasina
        // kacar ve siluetin bir kismi kaybolur.
        _OutlineDepthBias("Kontur Derinlik Itmesi", Range(0, 0.05)) = 0.004
        // Sert normalli mesh'lerde (küp) normal boyunca sisirme kosede yirtilir.
        // Merkeze gore konum yonu konveks govdelerde surekli bir kabuk verir.
        [Toggle(_OUTLINE_FROM_POSITION)] _OutlineFromPosition("Konturu Konumdan Sis", Float) = 0

        [Header(Soru Isareti Deseni)][Space(4)]
        [Toggle(_PATTERN_ON)] _PatternEnabled("Desen Acik", Float) = 0
        [NoScaleOffset] _PatternMap("Desen (alfa)", 2D) = "black" {}
        _PatternTiling("Desen Sikligi", Range(0.05, 8)) = 1.4
        _PatternStrength("Desen Gucu", Range(0, 1)) = 1.0
        // Toplamsal kaldirma, carpimsal kazanctan once gelmez: siyah bir govdede
        // carpim tek basina hicbir sey uretmez, ileride koyu yesil/siyah aticilar
        // gelecek. Referansta olculen fark (+0.24,+0.25,+0.15) bu ikiliyle tutuyor.
        _PatternGain("Desen Kazanci", Range(1, 2)) = 1.15
        _PatternLift("Desen Kaldirma", Range(0, 1)) = 0.16
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float  _RampCenter;
            float  _RampSmooth;
            float  _ShadowValue;
            float  _ShadowSat;
            float4 _ShadowTint;
            float  _ShadowTintAmount;
            float4 _HighlightColor;
            float  _HighlightAmount;
            float  _HighlightStart;
            float  _HighlightSmooth;
            float  _FaceGradient;
            float  _FaceExtent;
            float  _EdgeDarken;
            float  _EdgePower;
            float4 _OutlineColor;
            float  _OutlineWidth;
            float  _OutlineDepthBias;
            float  _PatternTiling;
            float  _PatternStrength;
            float  _PatternGain;
            float  _PatternLift;
        CBUFFER_END
        ENDHLSL

        // --------------------------------------------------------------------
        // Kontur. URP forward renderer'i SRPDefaultUnlit etiketli pass'i de
        // cizer, boylece tek materyalden iki gecis cikar ve prefablarda ikinci
        // materyal yuvasi acmaya gerek kalmaz.
        // --------------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma shader_feature_local_vertex _OUTLINE_ON
            #pragma shader_feature_local_vertex _OUTLINE_FROM_POSITION

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;

            #ifndef _OUTLINE_ON
                // w = 0 -> ucgen dejenere olur ve rasterize edilmez. Pass'i
                // materyal basina kapatmanin en ucuz yolu.
                OUT.positionCS = float4(0, 0, 0, 0);
                return OUT;
            #else
                float3 dirOS = IN.normalOS;
            #ifdef _OUTLINE_FROM_POSITION
                dirOS = normalize(IN.positionOS.xyz + 1e-6);
            #endif

                float3 dirWS  = TransformObjectToWorldNormal(dirOS);
                float4 posCS  = TransformObjectToHClip(IN.positionOS.xyz);
                float3 dirCS  = mul((float3x3)UNITY_MATRIX_VP, dirWS);

                // Yonu once PIKSEL uzayina cevir, sonra normalize et. Dogrudan
                // NDC'de normalize etmek genis ekranlarda konturu yatayda
                // inceltirdi (NDC'nin iki ekseni ayni piksel sayisina denk gelmez).
                float2 dirPix = normalize(dirCS.xy * _ScreenParams.xy + 1e-6);
                float2 ndcPerPixel = 2.0 / _ScreenParams.xy;

                // posCS.w ile carpim, perspektif bolmeden sonra kalinligi sabit
                // tutar. Ortografik kamerada w = 1, yani bu terim zararsiz.
                posCS.xy += dirPix * _OutlineWidth * ndcPerPixel * posCS.w;

                // Kabuk sadece XY'de sisiyor, derinligi govdeyle AYNI kaliyor —
                // ters yuzeyler govdeyle z-fight edip yuzeye siyah benek serpiyor.
                // Render state'teki "Offset" isaretini derleyiciye birakiyordu ve
                // ters-Z'de yon garantili degil; burada UNITY_REVERSED_Z'ye bakip
                // konturu her zaman kameradan UZAGA itiyoruz. Siluet disinda
                // rakip olmadigi icin kontur yine tam gorunur.
            #if UNITY_REVERSED_Z
                posCS.z -= _OutlineDepthBias * posCS.w;
            #else
                posCS.z += _OutlineDepthBias * posCS.w;
            #endif

                OUT.positionCS = posCS;
                return OUT;
            #endif
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }

        // --------------------------------------------------------------------
        // Govde.
        // --------------------------------------------------------------------
        Pass
        {
            Name "Body"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex BodyVert
            #pragma fragment BodyFrag
            // Her iki asamada da tanimli olmali: Varyings struct'i bu keyword'e
            // bagli, _fragment varyanti vertex tarafinda tanimsiz kalirdi.
            #pragma shader_feature_local _PATTERN_ON

            TEXTURE2D(_PatternMap);
            SAMPLER(sampler_PatternMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float3 positionOS : TEXCOORD2;   // yuzey gradyani + desen kullanir
            #ifdef _PATTERN_ON
                float3 normalOS   : TEXCOORD3;
            #endif
            };

            Varyings BodyVert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS  = GetWorldSpaceViewDir(positionWS);
                OUT.positionOS = IN.positionOS.xyz;
            #ifdef _PATTERN_ON
                OUT.normalOS   = IN.normalOS;
            #endif
                return OUT;
            }

        #ifdef _PATTERN_ON
            // Cubic_Dog.fbx ikili bir dosya ve UV'lerini disaridan dogrulamak
            // mumkun degil; triplanar projeksiyon UV istemez. Object space'te
            // yapiliyor, boylece atici ates yonune donerken desen govdede sabit
            // kalir (world space olsaydi govdenin uzerinden kayardi).
            float3 ApplyPattern(float3 base, float3 positionOS, float3 normalOS)
            {
                float3 blend = abs(normalize(normalOS));
                blend /= max(blend.x + blend.y + blend.z, 1e-4);

                float2 uvX = positionOS.zy * _PatternTiling;
                float2 uvY = positionOS.xz * _PatternTiling;
                float2 uvZ = positionOS.xy * _PatternTiling;

                half a = SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, uvX).a * blend.x
                       + SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, uvY).a * blend.y
                       + SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, uvZ).a * blend.z;

                float3 patternCol = saturate(base * _PatternGain + _PatternLift);
                return lerp(base, patternCol, saturate(a) * _PatternStrength);
            }
        #endif

            half4 BodyFrag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                float3 L = normalize(_MainLightPosition.xyz);

                float3 base = _BaseColor.rgb;
            #ifdef _PATTERN_ON
                base = ApplyPattern(base, IN.positionOS, IN.normalOS);
            #endif

                // (1) Golge: luminansa dogru degil, luminanstan UZAGA lerp —
                // _ShadowSat > 1 oldugu icin renk doyguluk kazanir.
                float  lum       = dot(base, float3(0.299, 0.587, 0.114));
                float3 shadowCol = saturate(lerp(lum.xxx, base, _ShadowSat)) * _ShadowValue;
                shadowCol = lerp(shadowCol, _ShadowTint.rgb * lum, _ShadowTintAmount);

                float ndl = dot(N, L);
                float lit = smoothstep(_RampCenter - _RampSmooth,
                                       _RampCenter + _RampSmooth, ndl);
                float3 col = lerp(shadowCol, base, lit);

                // (2) Sicak isik bandi — toplamsal.
                float hi = smoothstep(_HighlightStart - _HighlightSmooth,
                                      _HighlightStart + _HighlightSmooth, ndl);
                col += _HighlightColor.rgb * (_HighlightAmount * hi);

                // (3) Yuzey gradyani. Duz yuzlu mesh'te N.L sabit oldugu icin yuz
                // tek renge cakiliyordu; referansta yuzey asagi dogru aciliyor.
                // 0'da hicbir sey yapmaz, yani egri govdeler etkilenmez.
                float faceT = saturate(IN.positionOS.y / _FaceExtent * 0.5 + 0.5);
                col *= 1.0 + _FaceGradient * (0.5 - faceT);

                // (4) Kenar karartma — fresnel EKLEMIYOR, KISIYOR.
                float fres = pow(saturate(1.0 - saturate(dot(N, V))), _EdgePower);
                col *= lerp(1.0, 1.0 - _EdgeDarken, fres);

                return half4(saturate(col), _BaseColor.a);
            }
            ENDHLSL
        }

        // --------------------------------------------------------------------
        // Depth prepass / depth texture bunu bekler. Eksik olursa derinlige
        // dayanan her sey (ornegin ileride bir soft particle) bu nesneleri
        // gormez.
        // --------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack Off
}

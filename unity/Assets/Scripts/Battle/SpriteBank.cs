using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameMaker.Battle
{
    /// <summary>
    /// 유닛 스프라이트 로더.
    /// 레거시의 setViewResource(name + action) 규칙을 그대로 따른다:
    ///   "{name}{action}_{frame}" (예: ourbasicmove_0) 프레임들을 찾고,
    ///   없으면 move 프레임 → 단일 스프라이트 → 컬러 플레이스홀더 순으로 폴백.
    /// </summary>
    public static class SpriteBank
    {
        static Dictionary<string, Sprite> all;
        static Sprite placeholder;

        static void EnsureLoaded()
        {
            if (all != null) return;
            all = Resources.LoadAll<Sprite>("Sprites/units")
                .GroupBy(s => s.name)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public static Sprite[] GetFrames(string name, string action)
        {
            EnsureLoaded();

            var frames = Collect(name + action);
            if (frames.Length > 0) return frames;

            frames = Collect(name + "move");
            if (frames.Length > 0) return frames;

            if (all.TryGetValue(name, out var single)) return new[] { single };

            return new[] { Placeholder() };
        }

        static Sprite[] Collect(string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 8; i++)
            {
                if (all.TryGetValue(prefix + "_" + i, out var s)) list.Add(s);
                else break;
            }
            return list.ToArray();
        }

        public static Sprite GetEnv(string name) => Resources.Load<Sprite>("Sprites/env/" + name);

        static Sprite rounded;

        /// <summary>모서리가 둥근 9-slice 스프라이트 — 체력바/패널을 부드럽게.
        /// SpriteRenderer.drawMode = Sliced 와 함께 사용.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int S = 24;
                const float R = 8f;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < S; y++)
                {
                    for (int x = 0; x < S; x++)
                    {
                        // 둥근 사각형 내부 거리 계산 (모서리 반경 R, 1px 안티앨리어싱)
                        float dx = Mathf.Max(R - x, x - (S - 1 - R), 0);
                        float dy = Mathf.Max(R - y, y - (S - 1 - R), 0);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(R - d + 0.5f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
                }
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 1f, 0,
                    SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
                return rounded;
            }
        }

        static Sprite circle;

        /// <summary>안티앨리어싱된 원형 스프라이트 — 원형 아이콘 버튼 배경용.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int S = 64;
                float r = S * 0.5f - 1f;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < S; y++)
                {
                    for (int x = 0; x < S; x++)
                    {
                        float d = Mathf.Sqrt((x - S / 2f + 0.5f) * (x - S / 2f + 0.5f)
                                           + (y - S / 2f + 0.5f) * (y - S / 2f + 0.5f));
                        tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
                    }
                }
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 1f);
                return circle;
            }
        }

        static Sprite fan;

        /// <summary>1/4 원(부채꼴) 스프라이트 — 좌하단 모서리가 원 중심. 코너 버튼용(냥코 스타일).</summary>
        public static Sprite Fan
        {
            get
            {
                if (fan != null) return fan;
                const int S = 256;
                float r = S - 2f;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < S; y++)
                {
                    for (int x = 0; x < S; x++)
                    {
                        float d = Mathf.Sqrt(x * x + y * y);
                        tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
                    }
                }
                tex.Apply();
                fan = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0f, 0f), 1f);
                return fan;
            }
        }

        static Sprite cloud;

        /// <summary>몽글몽글한 카툰 구름 스프라이트 (원 겹침으로 생성).</summary>
        public static Sprite Cloud
        {
            get
            {
                if (cloud != null) return cloud;
                const int W = 240, H = 130;
                // (cx, cy, r) — 겹치는 원들로 구름 모양
                float[][] discs = {
                    new[] { 62f, 52f, 42f }, new[] { 112f, 66f, 52f },
                    new[] { 168f, 54f, 40f }, new[] { 208f, 46f, 28f }, new[] { 32f, 44f, 26f }
                };
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        float a = 0f;
                        foreach (var d in discs)
                        {
                            float dist = Mathf.Sqrt((x - d[0]) * (x - d[0]) + (y - d[1]) * (y - d[1]));
                            a = Mathf.Max(a, Mathf.Clamp01(d[2] - dist + 0.5f));
                        }
                        if (y < 34) a *= Mathf.Clamp01((y - 20f) / 14f); // 아랫면 평평하게
                        tex.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
                }
                tex.Apply();
                cloud = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 1f);
                return cloud;
            }
        }

        static Sprite arrow;

        /// <summary>원거리 유닛 발사체용 작은 화살 스프라이트 (오른쪽 방향).</summary>
        public static Sprite Arrow
        {
            get
            {
                if (arrow != null) return arrow;
                const int W = 26, H = 7;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                var clear = new Color(0, 0, 0, 0);
                var shaft = new Color(0.55f, 0.38f, 0.2f);
                var head  = new Color(0.85f, 0.85f, 0.9f);
                var feather = new Color(0.9f, 0.3f, 0.25f);
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        tex.SetPixel(x, y, clear);
                for (int x = 2; x < 20; x++) tex.SetPixel(x, 3, shaft);
                // 화살촉 (삼각형)
                for (int i = 0; i < 5; i++)
                    for (int y = 3 - i / 2; y <= 3 + i / 2; y++)
                        tex.SetPixel(25 - i, y, head);
                // 깃털
                tex.SetPixel(2, 2, feather); tex.SetPixel(2, 4, feather);
                tex.SetPixel(3, 2, feather); tex.SetPixel(3, 4, feather);
                tex.SetPixel(2, 1, feather); tex.SetPixel(2, 5, feather);
                tex.Apply();
                arrow = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 1f);
                return arrow;
            }
        }

        static Sprite white;

        /// <summary>체력바 등에 쓰는 1x1 흰색 스프라이트.</summary>
        public static Sprite White
        {
            get
            {
                if (white != null) return white;
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                return white;
            }
        }

        static Sprite Placeholder()
        {
            if (placeholder != null) return placeholder;
            var tex = new Texture2D(64, 64);
            var px = new Color[64 * 64];
            for (int i = 0; i < px.Length; i++) px[i] = Color.magenta;
            tex.SetPixels(px);
            tex.Apply();
            placeholder = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 1f);
            return placeholder;
        }
    }

    /// <summary>2~n 프레임을 순환 재생하는 초간단 애니메이터 (레거시 GIF 재생 대응).</summary>
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        public float fps = 10f;
        SpriteRenderer sr;
        Sprite[] frames;
        float t;
        int idx;
        bool loop = true;
        System.Action onDone;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        public void Play(Sprite[] newFrames, bool loopPlay = true, System.Action onComplete = null)
        {
            frames = newFrames;
            loop = loopPlay;
            onDone = onComplete;
            idx = 0;
            t = 0;
            if (sr != null && frames.Length > 0) sr.sprite = frames[0];
        }

        void Update()
        {
            if (frames == null) return;
            if (frames.Length <= 1 && loop) return; // 단일 프레임 루프 = 정지 이미지
            t += Time.deltaTime;
            if (t < 1f / fps) return;
            t = 0;
            idx++;
            if (idx >= frames.Length)
            {
                if (loop) idx = 0;
                else { frames = null; onDone?.Invoke(); return; } // 1회 재생 완료 통지
            }
            sr.sprite = frames[idx];
        }
    }
}

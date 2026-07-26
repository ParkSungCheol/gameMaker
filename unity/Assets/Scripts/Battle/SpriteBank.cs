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

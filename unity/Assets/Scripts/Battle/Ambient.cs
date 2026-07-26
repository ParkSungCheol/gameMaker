using System.Collections.Generic;
using UnityEngine;

namespace GameMaker.Battle
{
    /// <summary>
    /// 스테이지 분위기 파티클 — 코드로만 생성하는 초경량 앰비언트 이펙트.
    /// 모드: snow(눈), leaves(낙엽), petals(꽃잎), sand(모래바람),
    ///       fireflies(반딧불), sparkle(수정 반짝임), abyss(심연 부유 입자), sea(물보라)
    /// </summary>
    public class Ambient : MonoBehaviour
    {
        class P
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Vector2 vel;
            public float phase, size, baseAlpha, spin;
            public Vector3 home;
        }

        string mode;
        float worldW;
        readonly List<P> ps = new List<P>();

        public void Init(string ambientMode, float worldWidth)
        {
            mode = ambientMode;
            worldW = worldWidth;

            int count;
            switch (mode)
            {
                case "snow": count = 40; break;
                case "leaves": count = 14; break;
                case "petals": count = 16; break;
                case "sand": count = 40; break;
                case "sprinkles": count = 18; break;
                case "rain": count = 55; break;
                case "fireflies": count = 12; break;
                case "sparkle": count = 20; break;
                case "abyss": count = 22; break;
                case "sea": count = 18; break;
                default: return;
            }

            bool glow = mode == "fireflies" || mode == "sparkle" || mode == "abyss";
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("p" + i);
                go.transform.SetParent(transform, false);
                var p = new P { tr = go.transform, sr = go.AddComponent<SpriteRenderer>() };
                p.sr.sprite = SpriteBank.Circle;

                if (glow) // 은은한 겉광 한 겹 — 조잡한 점이 아니라 빛나는 입자로
                {
                    var halo = new GameObject("halo");
                    halo.transform.SetParent(go.transform, false);
                    var hsr = halo.AddComponent<SpriteRenderer>();
                    hsr.sprite = SpriteBank.Circle;
                    hsr.sortingOrder = -1;
                    halo.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
                    hsr.color = new Color(1f, 1f, 1f, 0.22f); // 색은 Setup 에서 본색 따라감
                }
                Setup(p, true);
                ps.Add(p);
            }
        }

        void SetHalo(P p, Color core)
        {
            var halo = p.tr.Find("halo");
            if (halo != null)
                halo.GetComponent<SpriteRenderer>().color = new Color(core.r, core.g, core.b, 0.2f);
        }

        void Setup(P p, bool fresh)
        {
            float x = Random.Range(-200f, worldW + 300f);
            p.phase = Random.Range(0f, 6.28f);

            switch (mode)
            {
                case "snow":
                    p.size = Random.Range(7f, 14f);
                    p.sr.color = new Color(1f, 1f, 1f, Random.Range(0.55f, 0.9f));
                    p.sr.sortingOrder = 12;
                    p.vel = new Vector2(Random.Range(-25f, -60f), Random.Range(-70f, -140f));
                    p.tr.position = new Vector3(x, fresh ? Random.Range(50f, 1500f) : 1500f, 0);
                    p.tr.localScale = new Vector3(p.size / 64f, p.size / 64f, 1f);
                    break;

                case "leaves":
                {
                    // 타원형 잎 (납작한 원 + 회전) — 초록 계열 3톤
                    p.size = Random.Range(14f, 22f);
                    float g = Random.Range(0f, 1f);
                    p.sr.color = g < 0.33f ? new Color(0.36f, 0.62f, 0.22f, 0.95f)
                              : g < 0.66f ? new Color(0.5f, 0.72f, 0.28f, 0.95f)
                                          : new Color(0.28f, 0.5f, 0.2f, 0.95f);
                    p.sr.sortingOrder = 12;
                    p.spin = Random.Range(-160f, 160f);
                    p.vel = new Vector2(Random.Range(-50f, -100f), Random.Range(-40f, -80f));
                    p.tr.position = new Vector3(x, fresh ? Random.Range(100f, 1400f) : 1400f, 0);
                    p.tr.localScale = new Vector3(p.size / 64f * 1.25f, p.size / 64f * 0.55f, 1f);
                    break;
                }
                case "petals":
                {
                    // 분홍/흰 꽃잎 — 하늘하늘 낙하
                    p.size = Random.Range(10f, 16f);
                    p.sr.color = Random.Range(0f, 1f) < 0.6f
                        ? new Color(1f, 0.72f, 0.82f, 0.95f)
                        : new Color(1f, 0.92f, 0.95f, 0.95f);
                    p.sr.sortingOrder = 12;
                    p.spin = Random.Range(-120f, 120f);
                    p.vel = new Vector2(Random.Range(-35f, -75f), Random.Range(-30f, -60f));
                    p.tr.position = new Vector3(x, fresh ? Random.Range(100f, 1400f) : 1400f, 0);
                    p.tr.localScale = new Vector3(p.size / 64f * 1.15f, p.size / 64f * 0.6f, 1f);
                    break;
                }
                case "sprinkles":
                {
                    // 알록달록 캔디 스프링클 낙하 (과자섬)
                    p.size = Random.Range(8f, 13f);
                    Color[] candy = {
                        new Color(1f, 0.5f, 0.65f), new Color(0.55f, 0.8f, 1f),
                        new Color(1f, 0.85f, 0.35f), new Color(0.65f, 0.9f, 0.5f),
                        new Color(0.85f, 0.6f, 1f)
                    };
                    p.sr.color = candy[Random.Range(0, candy.Length)];
                    p.sr.sortingOrder = 12;
                    p.spin = Random.Range(-140f, 140f);
                    p.vel = new Vector2(Random.Range(-30f, -70f), Random.Range(-50f, -95f));
                    p.tr.position = new Vector3(x, fresh ? Random.Range(100f, 1400f) : 1400f, 0);
                    p.tr.localScale = new Vector3(p.size / 64f * 1.4f, p.size / 64f * 0.5f, 1f);
                    break;
                }
                case "rain":
                    // 가늘고 긴 빗줄기 — 빠르게 사선으로 낙하
                    p.size = Random.Range(26f, 46f);
                    p.sr.color = new Color(0.75f, 0.85f, 0.95f, Random.Range(0.35f, 0.6f));
                    p.sr.sortingOrder = 12;
                    p.vel = new Vector2(-140f, Random.Range(-900f, -1300f));
                    p.tr.position = new Vector3(x, fresh ? Random.Range(100f, 1500f) : 1550f, 0);
                    p.tr.localScale = new Vector3(3.5f / 64f, p.size / 64f, 1f);
                    p.tr.localRotation = Quaternion.Euler(0, 0, 7f); // 바람 사선
                    break;

                case "sand":
                    // 길쭉한 모래 줄기 — 빠르고 또렷하게
                    p.size = Random.Range(9f, 16f);
                    p.sr.color = new Color(0.95f, 0.82f, 0.5f, Random.Range(0.6f, 0.95f));
                    p.sr.sortingOrder = 12;
                    p.vel = new Vector2(Random.Range(-300f, -520f), 0f);
                    p.tr.position = new Vector3(fresh ? x : worldW + 300f, Random.Range(25f, 380f), 0);
                    p.tr.localScale = new Vector3(p.size / 64f * 3.6f, p.size / 64f * 0.5f, 1f);
                    break;

                case "fireflies":
                    p.size = Random.Range(9f, 14f);
                    p.baseAlpha = Random.Range(0.6f, 1f);
                    p.sr.color = new Color(1f, 0.93f, 0.4f, p.baseAlpha);
                    p.sr.sortingOrder = 12;
                    p.home = new Vector3(x, Random.Range(150f, 900f), 0);
                    p.tr.position = p.home;
                    p.tr.localScale = new Vector3(p.size / 64f, p.size / 64f, 1f);
                    SetHalo(p, p.sr.color);
                    break;

                case "sparkle":
                    p.size = Random.Range(7f, 12f);
                    p.baseAlpha = Random.Range(0.7f, 1f);
                    p.sr.color = new Color(0.65f, 0.95f, 1f, 0f);
                    p.sr.sortingOrder = 12;
                    p.home = new Vector3(x, Random.Range(100f, 1300f), 0);
                    p.tr.position = p.home;
                    SetHalo(p, p.sr.color);
                    break;

                case "abyss":
                    p.size = Random.Range(7f, 13f);
                    p.baseAlpha = Random.Range(0.45f, 0.8f);
                    p.sr.color = new Color(0.85f, 0.55f, 1f, p.baseAlpha);
                    p.sr.sortingOrder = 12;
                    p.vel = new Vector2(0f, Random.Range(35f, 90f));
                    p.tr.position = new Vector3(x, fresh ? Random.Range(0f, 1200f) : Random.Range(-30f, 20f), 0);
                    p.tr.localScale = new Vector3(p.size / 64f, p.size / 64f, 1f);
                    SetHalo(p, p.sr.color);
                    break;

                case "sea":
                    p.size = Random.Range(6f, 12f);
                    p.baseAlpha = Random.Range(0.5f, 0.9f);
                    p.sr.color = new Color(1f, 1f, 1f, p.baseAlpha);
                    p.sr.sortingOrder = -8;
                    p.vel = new Vector2(Random.Range(-60f, -130f), 0f);
                    p.tr.position = new Vector3(fresh ? x : worldW + 300f, Random.Range(0f, 55f), 0);
                    p.tr.localScale = new Vector3(p.size / 64f, p.size / 64f, 1f);
                    break;
            }
        }

        void Update()
        {
            float t = Time.time;
            foreach (var p in ps)
            {
                switch (mode)
                {
                    case "rain":
                    {
                        var pos = p.tr.position;
                        pos += (Vector3)(p.vel * Time.deltaTime);
                        p.tr.position = pos;
                        if (pos.y < 10f) Setup(p, false);
                        break;
                    }
                    case "snow":
                    case "leaves":
                    case "petals":
                    case "sprinkles":
                    {
                        var pos = p.tr.position;
                        pos += (Vector3)(p.vel * Time.deltaTime);
                        pos.x += Mathf.Sin(t * 2.2f + p.phase) * 44f * Time.deltaTime;
                        if (p.spin != 0f)
                            p.tr.Rotate(0, 0, p.spin * Time.deltaTime);
                        p.tr.position = pos;
                        if (pos.y < 15f || pos.x < -250f) Setup(p, false);
                        break;
                    }
                    case "sand":
                    case "sea":
                    {
                        var pos = p.tr.position;
                        pos += (Vector3)(p.vel * Time.deltaTime);
                        pos.y += Mathf.Sin(t * 5f + p.phase) * 20f * Time.deltaTime;
                        p.tr.position = pos;
                        if (pos.x < -250f) Setup(p, false);
                        break;
                    }
                    case "fireflies":
                    {
                        p.tr.position = p.home + new Vector3(
                            Mathf.Sin(t * 0.7f + p.phase) * 90f,
                            Mathf.Sin(t * 0.9f + p.phase * 1.7f) * 60f, 0);
                        var c = p.sr.color;
                        c.a = p.baseAlpha * (0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 1.6f + p.phase)));
                        p.sr.color = c;
                        break;
                    }
                    case "sparkle":
                    {
                        float k = Mathf.Sin(t * 2.4f + p.phase);
                        var c = p.sr.color;
                        c.a = k > 0.86f ? p.baseAlpha * ((k - 0.86f) / 0.14f) : 0f;
                        p.sr.color = c;
                        float s = (p.size / 64f) * (1f + (k > 0.86f ? (k - 0.86f) * 3f : 0f));
                        p.tr.localScale = new Vector3(s, s, 1f);
                        break;
                    }
                    case "abyss":
                    {
                        var pos = p.tr.position;
                        pos += (Vector3)(p.vel * Time.deltaTime);
                        pos.x += Mathf.Sin(t * 1.4f + p.phase) * 30f * Time.deltaTime;
                        p.tr.position = pos;
                        var c = p.sr.color;
                        c.a = p.baseAlpha * Mathf.Clamp01((1250f - pos.y) / 500f);
                        p.sr.color = c;
                        if (pos.y > 1250f) Setup(p, false);
                        break;
                    }
                }
            }
        }
    }
}

using GameMaker.Battle;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMaker.Screens
{
    /// <summary>
    /// 메뉴 화면 공용 야외 배경 — 전투 스테이지와 같은 화풍(하늘/해/구름/풀 지면)을
    /// 타이틀·메인·업그레이드 화면이 공유해서 게임 전체 톤을 통일한다.
    /// </summary>
    public static class MenuBackdrop
    {
        /// <summary>게임 타이틀 (한글).</summary>
        public const string GameTitle = "보물 원정대";

        /// <summary>지면 윗선 — 화면 하단 기준(anchor 0.5,0) y 좌표.</summary>
        public const float GroundTop = 150f;

        /// <summary>하늘 + 해 + 떠다니는 구름 + 풀 지면 + 소품. dim>0 이면 어두운 오버레이 추가.</summary>
        public static void Build(MonoBehaviour host, Canvas canvas, float dim = 0f, bool withGround = true)
        {
            var root = canvas.transform;

            var sky = Ui.Image(root, SpriteBank.GetEnv("stage1bg"), "Sky");
            Ui.Stretch((RectTransform)sky.transform);

            var sun = Ui.Image(root, SpriteBank.GetEnv("sun"), "Sun");
            Ui.Place((RectTransform)sun.transform, new Vector2(0f, 1f), new Vector2(120, -60), new Vector2(190, 190));
            sun.preserveAspect = true;

            // 떠다니는 구름 4개 (상단, 좌→우 순환)
            for (int i = 0; i < 4; i++)
            {
                var cloud = Ui.Image(root, SpriteBank.Cloud, "Cloud" + i);
                float w = 210f + i * 45f;
                Ui.Place((RectTransform)cloud.transform, new Vector2(0.5f, 1f),
                    new Vector2(-800f + i * 520f, -90f - (i % 2) * 120f), new Vector2(w, w * 0.55f));
                cloud.color = new Color(1f, 1f, 1f, 0.88f);
                cloud.raycastTarget = false;
                host.StartCoroutine(DriftCloud((RectTransform)cloud.transform, 18f + i * 9f));
            }

            if (withGround)
            {
                // 풀 지면: 잔디 기둥 타일 + 잔디 윗단(lip)
                var ground = Ui.Image(root, SpriteBank.GetEnv("col_grass"), "Ground");
                var gRt = (RectTransform)ground.transform;
                gRt.anchorMin = new Vector2(0f, 0f);
                gRt.anchorMax = new Vector2(1f, 0f);
                gRt.pivot = new Vector2(0.5f, 0f);
                gRt.anchoredPosition = Vector2.zero;
                gRt.sizeDelta = new Vector2(0, GroundTop);
                ground.type = Image.Type.Tiled;
                ground.raycastTarget = false;

                var lip = Ui.Image(root, SpriteBank.GetEnv("lip_grass"), "GroundLip");
                var lRt = (RectTransform)lip.transform;
                lRt.anchorMin = new Vector2(0f, 0f);
                lRt.anchorMax = new Vector2(1f, 0f);
                lRt.pivot = new Vector2(0.5f, 0f);
                lRt.anchoredPosition = new Vector2(0, GroundTop - 26f);
                lRt.sizeDelta = new Vector2(0, 44f);
                lip.type = Image.Type.Tiled;
                lip.raycastTarget = false;

                // 소품: 소나무 / 풀포기 / 돌멩이 (소나무는 화면 우측 끝에 걸치게 — 업그레이드 유닛과 안 겹침)
                Prop(root, "pine", new Vector2(890, GroundTop - 14), new Vector2(200, 260));
                Prop(root, "tuft_a", new Vector2(-560, GroundTop - 8), new Vector2(90, 60));
                Prop(root, "tuft_b", new Vector2(280, GroundTop - 8), new Vector2(80, 55));
                Prop(root, "rock_small", new Vector2(560, GroundTop - 10), new Vector2(90, 60));
            }

            if (dim > 0f)
            {
                var overlay = Ui.Panel(root, new Color(0f, 0f, 0f, dim), "DimOverlay");
                Ui.Stretch(overlay);
                overlay.GetComponent<Image>().raycastTarget = false;
            }
        }

        static void Prop(Transform root, string sprite, Vector2 pos, Vector2 size)
        {
            var img = Ui.Image(root, SpriteBank.GetEnv(sprite), "Prop_" + sprite);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        /// <summary>좌측에 아군 성, 지면 위로 원정대 유닛들이 걸어가는 타이틀 연출.</summary>
        public static void BuildParade(MonoBehaviour host, Canvas canvas)
        {
            var root = canvas.transform;

            var castle = Ui.Image(root, SpriteBank.GetFrames("ourcastle", "move")[0], "Castle");
            var cRt = (RectTransform)castle.transform;
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0f);
            cRt.pivot = new Vector2(0.5f, 0f);
            cRt.anchoredPosition = new Vector2(-770, GroundTop - 24f);
            cRt.sizeDelta = new Vector2(320, 320);
            castle.preserveAspect = true;
            castle.raycastTarget = false;

            // 원정대 행진: 성에서 출발해 오른쪽(보물)으로
            string[] parade = { "ourtank", "ourbasic", "ourbattle", "ourmass" };
            for (int i = 0; i < parade.Length; i++)
            {
                var frames = SpriteBank.GetFrames(parade[i], "move");
                var img = Ui.Image(root, frames[0], "Parade_" + parade[i]);
                var rt = (RectTransform)img.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(-380f + i * 170f, GroundTop - 18f);
                rt.sizeDelta = new Vector2(165, 165);
                img.preserveAspect = true;
                img.raycastTarget = false;
                if (frames.Length > 1) host.StartCoroutine(CycleFrames(img, frames, 0.16f));
                host.StartCoroutine(March(rt, 52f));
            }
        }

        /// <summary>게임 타이틀 라벨 — 금색 + 두꺼운 외곽선.</summary>
        public static Text TitleLabel(Canvas canvas, int size, Vector2 anchor, Vector2 pos)
        {
            var title = Ui.OutlinedLabel(canvas.transform, GameTitle, size, new Color(1f, 0.88f, 0.3f), "GameTitle");
            var o2 = title.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0.25f, 0.14f, 0.02f, 0.95f);
            o2.effectDistance = new Vector2(-3.5f, 3.5f);
            var sh = title.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sh.effectDistance = new Vector2(5f, -6f);
            Ui.Place((RectTransform)title.transform, anchor, pos);
            return title;
        }

        static System.Collections.IEnumerator DriftCloud(RectTransform rt, float speed)
        {
            while (rt != null)
            {
                var p = rt.anchoredPosition;
                p.x += speed * Time.deltaTime;
                if (p.x > 1150f) p.x = -1150f;
                rt.anchoredPosition = p;
                yield return null;
            }
        }

        static System.Collections.IEnumerator March(RectTransform rt, float speed)
        {
            while (rt != null)
            {
                var p = rt.anchoredPosition;
                p.x += speed * Time.deltaTime;
                if (p.x > 1120f) p.x = -1120f;
                rt.anchoredPosition = p;
                yield return null;
            }
        }

        /// <summary>걷기 프레임 순환. 프레임마다 원본 크기가 달라도(발 벌림 등) 배율을 하나로 고정해
        /// 몸 전체가 커졌다 작아졌다 하는 '꿀렁임'을 막는다 — 박스는 가장 큰 프레임 기준으로 맞춘다.</summary>
        public static System.Collections.IEnumerator CycleFrames(Image img, Sprite[] frames, float interval)
        {
            var rt = img.rectTransform;
            var box = rt.sizeDelta;
            float maxW = 1f, maxH = 1f;
            foreach (var fr in frames) { maxW = Mathf.Max(maxW, fr.rect.width); maxH = Mathf.Max(maxH, fr.rect.height); }
            float scale = Mathf.Min(box.x / maxW, box.y / maxH);
            int i = 0;
            while (img != null)
            {
                var fr = frames[i % frames.Length];
                img.sprite = fr;
                rt.sizeDelta = new Vector2(fr.rect.width * scale, fr.rect.height * scale);
                i++;
                yield return new WaitForSeconds(interval);
            }
        }
    }
}

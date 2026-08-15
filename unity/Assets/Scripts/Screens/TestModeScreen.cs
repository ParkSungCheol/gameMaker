using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;
using GameMaker.Battle;
using GameMaker.Data;
using GameMaker.Core;
using System.Collections.Generic;

namespace GameMaker.Screens
{
    /// <summary>
    /// Test mode UI: 리스트로 모든 유닛(아군/적군) 미리보기, Walk/Attack/Die 버튼으로 모션을 재생해볼 수 있다.
    /// 빠른 단위 테스트/시각화용으로만 사용.
    /// </summary>
    public class TestModeScreen : MonoBehaviour
    {
        Canvas canvas;
        RectTransform listRoot;
        Dictionary<string, UnitPreview> previews = new Dictionary<string, UnitPreview>();

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "TestModeCanvas");
            MenuBackdrop.Build(this, canvas);
            var title = Ui.OutlinedLabel(canvas.transform, "테스트 모드", 52, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -55));

            // 뒤로가기
            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 92,
                () => ScreenRouter.I.Show(ScreenId.Main), "BackButton");
            Ui.Place((RectTransform)back.transform, new Vector2(1f, 0f), new Vector2(-58, 50));

            // 자동 재생 토글
            var autoBtn = Ui.TextButton(canvas.transform, "Auto Play", 28, new Vector2(220, 64), ToggleAutoPlay, Color.gray, "AutoPlayBtn");
            Ui.Place((RectTransform)autoBtn.transform, new Vector2(0f, 1f), new Vector2(120, -60));

            // 리스트 컨테이너
            listRoot = Ui.Panel(canvas.transform, new Color(0,0,0,0), "ListRoot");
            Ui.Place(listRoot, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1500, 700));

            BuildList();
        }

        void BuildList()
        {
            // clear
            foreach (Transform c in listRoot) Destroy(c.gameObject);
            previews.Clear();

            // 가져오기
            var monsters = DataHub.I.GetMonsters();
            float x = -700; float y = 300; float gapX = 460; float gapY = -220;
            int col = 0;
            foreach (var m in monsters)
            {
                var slot = new GameObject("Preview_" + m.name);
                slot.transform.SetParent(listRoot, false);
                var rt = slot.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(420, 200);
                Ui.Place(rt, new Vector2(0f, 1f), new Vector2(x + col * gapX, y));

                var preview = slot.AddComponent<UnitPreview>();
                preview.Init(m, slot.transform);
                previews[m.name] = preview;

                col++;
                if (col >= 3) { col = 0; y += gapY; }
            }
        }

        bool autoPlay = false;
        float autoTimer = 0f;
        void ToggleAutoPlay()
        {
            autoPlay = !autoPlay;
        }

        void Update()
        {
            if (!autoPlay) return;
            autoTimer += Time.deltaTime;
            if (autoTimer < 1f) return;
            autoTimer = 0f;
            // 매 틱마다 모든 유닛 걷기/공격/죽기 순환
            foreach (var kv in previews)
            {
                kv.Value.PlayWalk();
            }
            // 1초 후 공격
            StartCoroutine(DoDelayed(0.5f, () => { foreach (var kv in previews) kv.Value.PlayAttack(); }));
            // 1.0초 후 죽기(리셋 포함)
            StartCoroutine(DoDelayed(1.0f, () => { foreach (var kv in previews) kv.Value.PlayDie(); }));
            StartCoroutine(DoDelayed(2.0f, () => { foreach (var kv in previews) kv.Value.ResetToIdle(); }));
        }

        System.Collections.IEnumerator DoDelayed(float t, System.Action a)
        {
            yield return new WaitForSeconds(t);
            a?.Invoke();
        }
    }

    // 컴포넌트: 단일 유닛 미리보기 + 버튼
    public class UnitPreview : MonoBehaviour
    {
        MonsterData data;
        Transform root;
        GameObject go;
        Unit fakeUnit;
        Text nameTxt;
        Image hpBarFill;

        public void Init(MonsterData m, Transform parent)
        {
            data = m;
            root = parent;

            // 배경 패널
            var panel = Ui.Image(root, SpriteBank.GetEnv("panel_parchment"), "Panel");
            ((RectTransform)panel.transform).sizeDelta = new Vector2(420, 200);

            // 이름
            nameTxt = Ui.OutlinedLabel(panel.transform, m.name, 28, Color.white, "Name");
            Ui.Place((RectTransform)nameTxt.transform, new Vector2(0.5f, 1f), new Vector2(0, -12));

            // 미리보기 스프라이트 객체
            go = new GameObject("Visual");
            go.transform.SetParent(panel.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();

            // Try common action frames first, then fall back to default frames
            var frames = SpriteBank.GetFrames(m.SpriteName, "move");
            if ((frames == null || frames.Length == 0)) frames = SpriteBank.GetFrames(m.SpriteName, "walk");
            if ((frames == null || frames.Length == 0)) frames = SpriteBank.GetFrames(m.SpriteName, "move");

            if (frames != null && frames.Length > 0)
            {
                sr.sprite = frames[0];
                var anim = go.AddComponent<SimpleSpriteAnimator>();
                anim.fps = 12f;
                anim.Play(frames, true);
            }

            var goRect = go.AddComponent<RectTransform>();
            goRect.sizeDelta = new Vector2(120, 120);
            Ui.Place(goRect, new Vector2(0f, 0.5f), new Vector2(50, 0));

            // HP 바
            var hpPanel = Ui.Image(panel.transform, SpriteBank.GetEnv("panel_parchment"), "HpBg");
            Ui.Place((RectTransform)hpPanel.transform, new Vector2(1f, 0.5f), new Vector2(-18, -18), new Vector2(160, 24));
            hpBarFill = Ui.Image(hpPanel.transform, SpriteBank.White, "HpFill");
            ((RectTransform)hpBarFill.transform).anchorMin = new Vector2(0f, 0f);
            ((RectTransform)hpBarFill.transform).anchorMax = new Vector2(1f, 1f);
            ((RectTransform)hpBarFill.transform).anchoredPosition = Vector2.zero;
            hpBarFill.type = Image.Type.Filled;
            hpBarFill.fillMethod = Image.FillMethod.Horizontal;
            hpBarFill.fillAmount = 1f;

            // 버튼: Walk / Attack / Die
            var walkBtn = Ui.TextButton(panel.transform, "Walk", 20, new Vector2(110, 44), () => PlayWalk(), Color.white, "Walk");
            Ui.Place((RectTransform)walkBtn.transform, new Vector2(0f, 0f), new Vector2(20, 20));
            var atkBtn = Ui.TextButton(panel.transform, "Attack", 20, new Vector2(110, 44), () => PlayAttack(), Color.white, "Attack");
            Ui.Place((RectTransform)atkBtn.transform, new Vector2(0.5f, 0f), new Vector2(0, 20));
            var dieBtn = Ui.TextButton(panel.transform, "Die", 20, new Vector2(110, 44), () => PlayDie(), Color.white, "Die");
            Ui.Place((RectTransform)dieBtn.transform, new Vector2(1f, 0f), new Vector2(-20, 20));
        }

        public void PlayWalk()
        {
            var frames = SpriteBank.GetFrames(data.SpriteName, "move");
            if ((frames == null || frames.Length == 0)) frames = SpriteBank.GetFrames(data.SpriteName, "walk");
            if (frames != null && frames.Length > 0)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                var anim = go.GetComponent<SimpleSpriteAnimator>();
                if (anim == null) anim = go.AddComponent<SimpleSpriteAnimator>();
                anim.fps = 12f;
                anim.Play(frames, true);
            }
        }

        public void PlayAttack()
        {
            var frames = SpriteBank.GetFrames(data.SpriteName, "attack");
            if (frames != null && frames.Length > 0)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                var anim = go.GetComponent<SimpleSpriteAnimator>();
                if (anim == null) anim = go.AddComponent<SimpleSpriteAnimator>();
                anim.fps = 12f;
                anim.Play(frames, false, () => { /* onComplete */ });
            }
        }

        public void PlayDie()
        {
            var frames = SpriteBank.GetFrames(data.SpriteName, "defeat");
            if ((frames == null || frames.Length == 0)) frames = SpriteBank.GetFrames(data.SpriteName, "death");
            if (frames != null && frames.Length > 0)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                var anim = go.GetComponent<SimpleSpriteAnimator>();
                if (anim == null) anim = go.AddComponent<SimpleSpriteAnimator>();
                anim.fps = 12f;
                anim.Play(frames, false, () => { hpBarFill.fillAmount = 0f; });
            }
            else
            {
                hpBarFill.fillAmount = 0f;
            }
        }

        public void ResetToIdle()
        {
            var frames = SpriteBank.GetFrames(data.SpriteName, "move");
            if ((frames == null || frames.Length == 0)) frames = SpriteBank.GetFrames(data.SpriteName);
            if (frames != null && frames.Length > 0)
            {
                var anim = go.GetComponent<SimpleSpriteAnimator>();
                anim.Play(frames, true);
                hpBarFill.fillAmount = 1f;
            }
        }
    }
}

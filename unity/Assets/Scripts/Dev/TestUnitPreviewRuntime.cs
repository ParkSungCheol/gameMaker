using GameMaker.Data;
using System;
using UnityEngine;
using UnityEngine.UI;
using GameMaker.Battle;

public class TestUnitPreviewRuntime : MonoBehaviour
{
    MonsterData data;
    Sprite[] walkFrames;
    Sprite[] attackFrames;
    Sprite[] defeatFrames;

    GameObject spriteHolder;
    SpriteRenderer sr;
    SimpleSpriteAnimator anim;

    Text nameText;
    Text teamText;

    Button walkBtn;
    Button attackBtn;
    Button dieBtn;

    Image highlight;

    public void Setup(MonsterData m, Sprite[] walk, Sprite[] attack, Sprite[] defeat)
    {
        data = m;
        walkFrames = walk ?? new Sprite[0];
        attackFrames = attack ?? new Sprite[0];
        defeatFrames = defeat ?? new Sprite[0];

        // background panel
        var bg = new GameObject("BG");
        bg.transform.SetParent(transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.11f, 0.09f, 0.6f);
        var bgRt = (RectTransform)bg.transform;
        bgRt.sizeDelta = new Vector2(320, 560);

        // highlight border
        var hi = new GameObject("Highlight");
        hi.transform.SetParent(bg.transform, false);
        highlight = hi.AddComponent<Image>();
        highlight.color = new Color(1f, 1f, 1f, 0f);
        var hiRt = (RectTransform)hi.transform;
        hiRt.anchorMin = new Vector2(0, 0);
        hiRt.anchorMax = new Vector2(1, 1);
        hiRt.offsetMin = new Vector2(-4, -4);
        hiRt.offsetMax = new Vector2(4, 4);

        // sprite area
        spriteHolder = new GameObject("Sprite");
        spriteHolder.transform.SetParent(bg.transform, false);
        var shRt = (RectTransform)spriteHolder.transform;
        shRt.anchoredPosition = new Vector2(0, 80);
        shRt.sizeDelta = new Vector2(280, 280);

        var srGo = new GameObject("SR");
        srGo.transform.SetParent(spriteHolder.transform, false);
        sr = srGo.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        anim = srGo.AddComponent<SimpleSpriteAnimator>();

        // name and team (UI texts)
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(bg.transform, false);
        nameText = nameGo.AddComponent<Text>();
        nameText.font = GameMaker.UI.Ui.DefaultFont;
        nameText.text = data.name;
        nameText.fontSize = 28;
        nameText.alignment = TextAnchor.MiddleCenter;
        var ntRt = (RectTransform)nameGo.transform;
        ntRt.anchoredPosition = new Vector2(0, -90);
        ntRt.sizeDelta = new Vector2(300, 40);

        var teamGo = new GameObject("Team");
        teamGo.transform.SetParent(bg.transform, false);
        teamText = teamGo.AddComponent<Text>();
        teamText.font = GameMaker.UI.Ui.DefaultFont;
        teamText.text = data.IsOur ? "Ally" : "Enemy";
        teamText.fontSize = 20;
        teamText.alignment = TextAnchor.MiddleCenter;
        var ttRt = (RectTransform)teamGo.transform;
        ttRt.anchoredPosition = new Vector2(0, -120);
        ttRt.sizeDelta = new Vector2(300, 28);

        // buttons
        var btnY = -200f;
        walkBtn = CreateBtn(bg.transform, "Walk", new Vector2(-80, btnY), () => PlayWalk());
        attackBtn = CreateBtn(bg.transform, "Attack", new Vector2(0, btnY), () => PlayAttack());
        dieBtn = CreateBtn(bg.transform, "Die", new Vector2(80, btnY), () => PlayDie());

        walkBtn.interactable = walkFrames.Length > 0;
        attackBtn.interactable = attackFrames.Length > 0;
        dieBtn.interactable = defeatFrames.Length > 0;

        // initially show idle/walk if available
        if (walkFrames.Length > 0) anim.Play(walkFrames, true);
        else if (attackFrames.Length > 0) anim.Play(new[] { attackFrames[0] }, true);
    }

    Button CreateBtn(Transform parent, string label, Vector2 pos, Action cb)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.18f, 0.16f, 1f);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(140, 50);
        rt.anchoredPosition = pos;

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => { cb(); Highlight(); });

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.font = GameMaker.UI.Ui.DefaultFont;
        txt.text = label;
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        var tRt = (RectTransform)txtGo.transform;
        tRt.sizeDelta = rt.sizeDelta;

        return btn;
    }

    void PlayWalk()
    {
        if (walkFrames.Length == 0) return;
        anim.Play(walkFrames, true);
    }
    void PlayAttack()
    {
        if (attackFrames.Length == 0) return;
        anim.Play(attackFrames, false, () => { if (walkFrames.Length > 0) anim.Play(walkFrames, true); });
    }
    void PlayDie()
    {
        if (defeatFrames.Length == 0) return;
        anim.Play(defeatFrames, false, () => { /* keep dead frame */ });
    }

    void Highlight()
    {
        // flash border
        StopAllCoroutines();
        StartCoroutine(DoHighlight());
    }

    System.Collections.IEnumerator DoHighlight()
    {
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            var c = highlight.color;
            c.a = Mathf.PingPong(t * 3f, 0.6f);
            highlight.color = c;
            yield return null;
        }
        var cc = highlight.color; cc.a = 0f; highlight.color = cc;
    }
}

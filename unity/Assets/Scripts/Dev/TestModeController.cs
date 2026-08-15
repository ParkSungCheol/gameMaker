using System.Collections.Generic;
using GameMaker.Data;
using GameMaker.UI;
using UnityEngine;
using UnityEngine.UI;
using GameMaker.Battle;

namespace GameMaker.Dev
{
    public class TestModeController : MonoBehaviour
    {
        Canvas canvas;
        RectTransform contentRoot;

        void Start()
        {
            canvas = Ui.CreateCanvas(transform, "TestModeCanvas");

            // Title
            var title = Ui.OutlinedLabel(canvas.transform, "Test Mode - Unit Preview", 48, Color.white, "Title");
            Ui.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0, -40));

            // Back button
            var back = Ui.CircleIconButton(canvas.transform, "icon_return", 80, () => UnityEngine.SceneManagement.SceneManager.LoadScene("Boot"), "Back");
            Ui.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(44, -40));

            // Filter buttons (All / Ally / Enemy)
            var filterAll = Ui.TextButton(canvas.transform, "All", 20, new Vector2(90, 46), () => ApplyFilter(Filter.All), null, "FilterAll");
            var filterAlly = Ui.TextButton(canvas.transform, "Ally", 20, new Vector2(90, 46), () => ApplyFilter(Filter.Ally), null, "FilterAlly");
            var filterEnemy = Ui.TextButton(canvas.transform, "Enemy", 20, new Vector2(90, 46), () => ApplyFilter(Filter.Enemy), null, "FilterEnemy");
            Ui.Place((RectTransform)filterEnemy.transform, new Vector2(1f, 1f), new Vector2(-24, -40));
            Ui.Place((RectTransform)filterAlly.transform, new Vector2(1f, 1f), new Vector2(-124, -40));
            Ui.Place((RectTransform)filterAll.transform, new Vector2(1f, 1f), new Vector2(-224, -40));

            // Scroll view area
            var panel = Ui.Panel(canvas.transform, new Color(0.06f, 0.05f, 0.04f, 0.6f), "ScrollPanel");
            Ui.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1700, 620));

            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(panel, false);
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            var image = scrollGO.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0);
            var rt = (RectTransform)scrollGO.transform;
            rt.sizeDelta = new Vector2(1700, 620);

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpImage = viewport.AddComponent<Image>();
            vpImage.color = new Color(0, 0, 0, 0);
            var vpRt = (RectTransform)viewport.transform;
            vpRt.anchorMin = new Vector2(0, 0);
            vpRt.anchorMax = new Vector2(1, 1);
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            contentRoot = (RectTransform)content.transform;
            contentRoot.pivot = new Vector2(0, 0.5f);
            contentRoot.anchorMin = new Vector2(0, 0.5f);
            contentRoot.anchorMax = new Vector2(0, 0.5f);
            contentRoot.anchoredPosition = new Vector2(10, 0);
            contentRoot.sizeDelta = new Vector2(2000, 600);

            scrollRect.content = contentRoot;
            scrollRect.viewport = vpRt;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            // Load monsters and populate
            Populate(contentRoot);
        }

        enum Filter { All, Ally, Enemy }
        Filter currentFilter = Filter.All;

        void ApplyFilter(Filter f)
        {
            currentFilter = f;
            // Rebuild view
            foreach (Transform t in contentRoot) Destroy(t.gameObject);
            Populate(contentRoot);
        }

        void Populate(RectTransform root)
        {
            var monsters = DataHub.I.GetMonsters();
            float x = 0f;
            foreach (var m in monsters)
            {
                bool isOur = m.IsOur;
                if (currentFilter == Filter.Ally && !isOur) continue;
                if (currentFilter == Filter.Enemy && isOur) continue;

                // check if has at least one motion frame
                var walk = SpriteBank.GetFrames(m.SpriteName, "move");
                var attack = SpriteBank.GetFrames(m.SpriteName, "attack");
                var defeat = SpriteBank.GetFrames(m.SpriteName, "defeat");
                if ((walk == null || walk.Length == 0) && (attack == null || attack.Length == 0) && (defeat == null || defeat.Length == 0))
                {
                    Debug.Log("[TestMode] skipping (no frames) : " + m.name);
                    continue;
                }

                // instantiate preview prefab (created at runtime)
                var go = new GameObject("UnitPreview_" + m.name);
                go.transform.SetParent(root, false);
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(320, 560);
                rt.anchoredPosition = new Vector2(x, 0);

                var preview = go.AddComponent<TestUnitPreviewRuntime>();
                preview.Setup(m, walk, attack, defeat);

                x += 340f; // gap
            }
            // adjust content width
            root.sizeDelta = new Vector2(Mathf.Max(1200, x + 20), root.sizeDelta.y);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using Upstream.Data;
using Upstream.River;

namespace Upstream.UI
{
    /// <summary>
    /// A fish drawn with UI Images (body, tail, fins, glow) so the shop can show any skin.
    /// Lays itself out inside its RectTransform; optionally wags its tail.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FishPreview : MonoBehaviour
    {
        private Image glow;
        private Image tail;
        private Image leftFin;
        private Image rightFin;
        private Image body;
        private FishDef fish;
        private bool animate;
        private float clock;

        public static FishPreview Create(Transform parent, Vector2 anchoredPos, float height, bool animated)
        {
            var go = new GameObject("FishPreview", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(height * 0.75f, height);

            var preview = go.AddComponent<FishPreview>();
            preview.animate = animated;
            preview.glow = preview.AddPart("Glow", WatercolorSpriteFactory.GetCircleGlowSprite());
            preview.tail = preview.AddPart("Tail", null);
            preview.leftFin = preview.AddPart("LeftFin", null);
            preview.rightFin = preview.AddPart("RightFin", null);
            preview.body = preview.AddPart("Body", null);
            return preview;
        }

        private Image AddPart(string partName, Sprite sprite)
        {
            var go = new GameObject(partName, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        public void SetFish(FishDef def)
        {
            fish = def;
            body.sprite = FishSkinFactory.GetBody(def);
            tail.sprite = FishSkinFactory.GetTail(def);
            leftFin.sprite = FishSkinFactory.GetFin(def);
            rightFin.sprite = FishSkinFactory.GetFin(def);
            glow.color = def.glow.a > 0f ? new Color(def.glow.r, def.glow.g, def.glow.b, Mathf.Min(1f, def.glow.a * 1.8f)) : Color.clear;
            Layout();
        }

        private void Layout()
        {
            // The in-game fish is 96 units tall (body sprite height); scale everything to this rect.
            float h = ((RectTransform)transform).rect.height;
            if (h <= 0f) h = ((RectTransform)transform).sizeDelta.y;
            float k = h / 150f; // leaves room for the tail below the body

            Place(body.rectTransform, new Vector2(0f, 14f * k), new Vector2(64f * fish.width, 96f * fish.length) * k * 0.8f, new Vector2(0.5f, 0.5f));
            Place(tail.rectTransform, new Vector2(0f, 14f * k - 26f * fish.length * k), new Vector2(48f, 40f) * k * 0.8f * fish.tailSize, new Vector2(0.5f, 0.95f));
            Place(leftFin.rectTransform, new Vector2(-20f * fish.width * k, 18f * k), new Vector2(32f, 20f) * k * 0.8f, new Vector2(0.5f, 0.5f));
            Place(rightFin.rectTransform, new Vector2(20f * fish.width * k, 18f * k), new Vector2(32f, 20f) * k * 0.8f, new Vector2(0.5f, 0.5f));
            leftFin.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            rightFin.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            Place(glow.rectTransform, new Vector2(0f, 6f * k), new Vector2(150f, 170f) * k, new Vector2(0.5f, 0.5f));
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private void Update()
        {
            if (!animate || fish == null) return;
            clock += Time.unscaledDeltaTime * 6f;
            float wag = Mathf.Sin(clock) * 18f;
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, wag);
            leftFin.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 32f + wag * 0.3f);
            rightFin.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -32f - wag * 0.3f);

            if (fish.pattern == FishPattern.Rainbow)
            {
                Color hue = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.25f, 1f), 0.55f, 1f);
                hue.a = 0.7f;
                glow.color = hue;
            }
        }
    }
}

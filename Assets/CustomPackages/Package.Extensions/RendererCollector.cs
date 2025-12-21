using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CustomPackages.Package.Extensions
{
    [Serializable]
    public class RendererCollector
    {
        public readonly Dictionary<SpriteRenderer, float> SpriteRenderers = new();
        public readonly Dictionary<TextMeshPro, float> Texts = new();

        public RendererCollector(GameObject root)
        {
            CollectFrom(root);
        }

        public void ResetColors()
        {
            foreach (var pair in SpriteRenderers)
            {
                var color = pair.Key.color;
                color.a = pair.Value;
                pair.Key.color = color;
            }

            foreach (var pair in Texts)
            {
                var color = pair.Key.color;
                color.a = pair.Value;
                pair.Key.color = color;
            }
        }

        private void CollectFrom(GameObject root)
        {
            var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var spriteRenderer in spriteRenderers)
            {
                SpriteRenderers.Add(spriteRenderer, spriteRenderer.color.a);
            }

            var texts = root.GetComponentsInChildren<TextMeshPro>(true);
            foreach (var text in texts)
            {
                Texts.Add(text, text.color.a);
            }
        }

        public void UpdateCollect(GameObject root)
        {
            SpriteRenderers.Clear();
            Texts.Clear();
            
            var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var spriteRenderer in spriteRenderers)
            {
                if (SpriteRenderers.ContainsKey(spriteRenderer) == false)
                    SpriteRenderers.Add(spriteRenderer, spriteRenderer.color.a);
            }
            
            var texts = root.GetComponentsInChildren<TextMeshPro>(true);
            foreach (var text in texts)
            {
                if (Texts.ContainsKey(text) == false)
                    Texts.Add(text, text.color.a);
            }
        }
    }
}
using UnityEngine;
using UnityEngine.Rendering;

namespace Features.Bosses.Scripts
{
    [DisallowMultipleComponent]
    public sealed class MushroomSplitFlash : MonoBehaviour
    {
        private const float Duration = 0.16f;
        private const int TextureSize = 128;

        private SpriteRenderer _renderer;
        private Sprite _sprite;
        private Texture2D _texture;
        private Camera _camera;
        private float _radius;
        private float _age;

        public static void Play(Vector3 position, float radius)
        {
            var flashObject = new GameObject("Mushroom Split Flash");
            flashObject.transform.position = position;
            flashObject.AddComponent<MushroomSplitFlash>().Initialize(radius);
        }

        private void Initialize(float radius)
        {
            _radius = Mathf.Max(0.05f, radius);
            _camera = Camera.main;
            _texture = CreateBurstTexture();
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), TextureSize, 0, SpriteMeshType.FullRect);
            _sprite.name = "Mushroom Split Flash";
            _sprite.hideFlags = HideFlags.DontSave;

            // SpriteRenderer supplies its default unlit sprite material, avoiding
            // a runtime Shader.Find dependency or a prefab/material hookup.
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _sprite;
            _renderer.color = Color.white;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            UpdateVisual();
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            if (_age >= Duration)
            {
                Destroy(gameObject);
                return;
            }

            UpdateVisual();
        }

        private void UpdateVisual()
        {
            float progress = Mathf.Clamp01(_age / Duration);
            float expansion = 1f - Mathf.Pow(1f - progress, 3f);
            transform.localScale = Vector3.one * (_radius * 2f * Mathf.Lerp(0.65f, 1f, expansion));
            transform.rotation = _camera != null
                ? _camera.transform.rotation
                : Quaternion.Euler(90f, 0f, 0f);

            // Begin at full brightness and fall away quickly after the split.
            float alpha = (1f - progress) * (1f - progress);
            _renderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private static Texture2D CreateBurstTexture()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "Mushroom Split Flash Burst",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (x + 0.5f) / TextureSize * 2f - 1f;
                    float v = (y + 0.5f) / TextureSize * 2f - 1f;
                    float distance = Mathf.Sqrt(u * u + v * v);
                    float angle = Mathf.Atan2(v, u);
                    float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 4f)), 16f);
                    float rayLength = Mathf.Lerp(0.24f, 0.96f, ray);
                    float rays = 1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(rayLength * 0.72f, rayLength, distance));
                    float core = 1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.1f, 0.43f, distance));
                    float halo = 0.2f * Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                    float alpha = Mathf.Max(core, Mathf.Max(rays, halo));
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (_sprite != null)
                Destroy(_sprite);
            if (_texture != null)
                Destroy(_texture);
        }
    }
}

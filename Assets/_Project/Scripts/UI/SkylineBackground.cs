using UnityEngine;
using UnityEngine.UI;

namespace FrontierDraw.UI
{
    /// <summary>
    /// Generates a simple two-layer skyline silhouette (distant jagged mountains + a nearer
    /// row of blocky rooftops) as a procedural texture at runtime, same "wiring first, no art
    /// asset required" approach as DuelController's generated placeholder SFX tones. Pure
    /// decoration behind the MainMenu panels - no logic, no interaction.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SkylineBackground : MonoBehaviour
    {
        [Header("Placeholder skyline (Phase 5 real background art not built yet)")]
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 256;
        [SerializeField] private Color skyColor = new Color(0.65f, 0.75f, 0.85f, 1f);
        [SerializeField] private Color farMountainColor = new Color(0.35f, 0.4f, 0.48f, 1f);
        [SerializeField] private Color nearRooftopColor = new Color(0.18f, 0.2f, 0.25f, 1f);
        [Tooltip("Roughly how many rooftop blocks span the width.")]
        [SerializeField] private int rooftopCount = 14;
        [SerializeField] private int randomSeed = 12345;

        private void Awake()
        {
            var image = GetComponent<Image>();
            image.sprite = GenerateSkylineSprite();
        }

        private Sprite GenerateSkylineSprite()
        {
            var random = new System.Random(randomSeed);
            var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

            // Distant mountain silhouette: a jagged line whose height wanders smoothly
            // per-column, mountains occupy roughly the lower 55% of the texture.
            int[] mountainHeights = new int[textureWidth];
            float mountainWalk = textureHeight * 0.35f;
            for (int x = 0; x < textureWidth; x++)
            {
                mountainWalk += (float)(random.NextDouble() - 0.5) * 8f;
                mountainWalk = Mathf.Clamp(mountainWalk, textureHeight * 0.2f, textureHeight * 0.55f);
                mountainHeights[x] = Mathf.RoundToInt(mountainWalk);
            }

            // Nearer rooftop silhouette: flat-topped blocks of varying height along the
            // bottom ~30% of the texture, in front of the mountains.
            int[] rooftopHeights = new int[textureWidth];
            int blockWidth = Mathf.Max(1, textureWidth / rooftopCount);
            for (int blockStart = 0; blockStart < textureWidth; blockStart += blockWidth)
            {
                int blockHeight = Mathf.RoundToInt((float)random.NextDouble() * textureHeight * 0.25f + textureHeight * 0.08f);
                for (int x = blockStart; x < Mathf.Min(blockStart + blockWidth, textureWidth); x++)
                {
                    rooftopHeights[x] = blockHeight;
                }
            }

            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    Color pixel;
                    if (y < rooftopHeights[x])
                    {
                        pixel = nearRooftopColor;
                    }
                    else if (y < mountainHeights[x])
                    {
                        pixel = farMountainColor;
                    }
                    else
                    {
                        pixel = skyColor;
                    }
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;

            return Sprite.Create(texture, new Rect(0, 0, textureWidth, textureHeight), new Vector2(0.5f, 0f));
        }
    }
}

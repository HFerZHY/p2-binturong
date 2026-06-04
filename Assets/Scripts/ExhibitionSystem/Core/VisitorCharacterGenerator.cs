using UnityEngine;

namespace ExhibitionSystem.Core
{
    /// <summary>
    /// Generates random visitor characters by rendering CharacterBase assets
    /// to a RenderTexture with randomized colors.
    /// </summary>
    public class VisitorCharacterGenerator : MonoBehaviour
    {
        // ── Serialized Fields ───────────────────────────────────────────────────

        [Header("Character Templates")]
        [SerializeField] private CharacterBase[] _characterBases;

        [Header("Render Target")]
        [SerializeField] private RenderTexture _renderTarget;

        private const byte VISIBLE_ALPHA_THRESHOLD = 8;
        private static readonly Color32 SKIN_MASK_COLOR = new(61, 61, 196, 255);
        private static readonly Color32 HAIR_MASK_COLOR = new(231, 191, 41, 255);
        private static readonly Color32 CLOTHES_MASK_COLOR = new(212, 58, 58, 255);
        private static readonly Color32 TRANSPARENT = new(0, 0, 0, 0);
        private static readonly Color32 DEFAULT_LINE_COLOR = new(18, 18, 24, 255);

        // ── Public Properties ───────────────────────────────────────────────────

        /// <summary>
        /// The RenderTexture where characters are rendered.
        /// </summary>
        public RenderTexture RenderTarget => _renderTarget;

        /// <summary>
        /// Whether the generator is properly configured with character bases.
        /// </summary>
        public bool IsConfigured => _characterBases != null && _characterBases.Length > 0;

        // ── Public Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Generates a random visitor character and renders it to the RenderTexture.
        /// </summary>
        public void GenerateRandomVisitor()
        {
            if (_characterBases == null || _characterBases.Length == 0)
            {
                Debug.LogWarning("[VisitorCharacterGenerator] No character bases configured.");
                return;
            }

            if (_renderTarget == null)
            {
                Debug.LogWarning("[VisitorCharacterGenerator] No render target configured.");
                return;
            }

            // 1. Randomly select a character base
            var charBase = _characterBases[Random.Range(0, _characterBases.Length)];
            if (charBase == null || charBase.colorTexture == null)
            {
                Debug.LogWarning("[VisitorCharacterGenerator] Selected character base is invalid.");
                return;
            }

            Color skinColor = RandomColor(charBase.skinColors);
            Color hairColor = RandomColor(charBase.hairColors);
            Color clothesColor = RandomColor(charBase.clothesColors);

            RenderCpuRecoloredFallback(charBase, skinColor, hairColor, clothesColor);
        }

        /// <summary>
        /// Clears the RenderTexture to transparent.
        /// </summary>
        public void ClearRenderTarget()
        {
            if (_renderTarget == null) return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _renderTarget;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
        }

        // ── Private Methods ─────────────────────────────────────────────────────

        private Color RandomColor(ColorPalette palette)
        {
            if (palette == null || palette.colors == null || palette.colors.Length == 0)
                return Color.white;

            return palette.colors[Random.Range(0, palette.colors.Length)];
        }

        private void RenderCpuRecoloredFallback(CharacterBase charBase, Color skinColor, Color hairColor, Color clothesColor)
        {
            Texture2D colorMask = ReadTexture(charBase.colorTexture);
            Texture2D lineMask = charBase.lineArtTexture != null ? ReadTexture(charBase.lineArtTexture) : null;
            Texture2D output = new Texture2D(_renderTarget.width, _renderTarget.height, TextureFormat.RGBA32, false);

            try
            {
                Color32[] colorPixels = colorMask.GetPixels32();
                Color32[] linePixels = lineMask != null ? lineMask.GetPixels32() : null;
                Color32[] outputPixels = new Color32[colorPixels.Length];

                Color32 skin = skinColor;
                Color32 hair = hairColor;
                Color32 clothes = clothesColor;
                Color32 line = ResolveLineColor(charBase.lineColor);

                for (int i = 0; i < colorPixels.Length; i++)
                {
                    Color32 source = colorPixels[i];
                    if (source.a <= VISIBLE_ALPHA_THRESHOLD)
                    {
                        outputPixels[i] = TRANSPARENT;
                        continue;
                    }

                    outputPixels[i] = RecolorMaskPixel(source, skin, hair, clothes);

                    if (linePixels == null) continue;

                    Color32 lineSource = linePixels[i];
                    if (lineSource.a <= VISIBLE_ALPHA_THRESHOLD) continue;

                    outputPixels[i] = CompositeLinePixel(outputPixels[i], line, lineSource.a);
                }

                output.SetPixels32(outputPixels);
                output.Apply(false);
                Graphics.Blit(output, _renderTarget);
            }
            finally
            {
                DestroyGeneratedObject(colorMask);
                DestroyGeneratedObject(lineMask);
                DestroyGeneratedObject(output);
            }
        }

        private Texture2D ReadTexture(Texture source)
        {
            RenderTexture temp = RenderTexture.GetTemporary(
                _renderTarget.width,
                _renderTarget.height,
                0,
                RenderTextureFormat.ARGB32);
            Texture2D readableTexture = new Texture2D(_renderTarget.width, _renderTarget.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, temp);
                RenderTexture.active = temp;
                readableTexture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
                readableTexture.Apply(false);
                return readableTexture;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temp);
            }
        }

        private Color32 RecolorMaskPixel(Color32 source, Color32 skin, Color32 hair, Color32 clothes)
        {
            int skinDistance = ColorDistanceSquared(source, SKIN_MASK_COLOR);
            int hairDistance = ColorDistanceSquared(source, HAIR_MASK_COLOR);
            int clothesDistance = ColorDistanceSquared(source, CLOTHES_MASK_COLOR);

            Color32 targetColor = skinDistance <= hairDistance && skinDistance <= clothesDistance
                ? skin
                : hairDistance <= clothesDistance
                    ? hair
                    : clothes;

            targetColor.a = source.a;
            return targetColor;
        }

        private int ColorDistanceSquared(Color32 a, Color32 b)
        {
            int dr = a.r - b.r;
            int dg = a.g - b.g;
            int db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        private Color32 CompositeLinePixel(Color32 basePixel, Color32 lineColor, byte lineAlpha)
        {
            if (basePixel.a <= VISIBLE_ALPHA_THRESHOLD)
                return new Color32(lineColor.r, lineColor.g, lineColor.b, lineAlpha);

            float t = lineAlpha / 255f;
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(basePixel.r, lineColor.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(basePixel.g, lineColor.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(basePixel.b, lineColor.b, t)),
                basePixel.a > lineAlpha ? basePixel.a : lineAlpha);
        }

        private Color32 ResolveLineColor(Color lineColor)
        {
            if (lineColor.a <= 0.01f)
                return DEFAULT_LINE_COLOR;

            return lineColor;
        }

        private void DestroyGeneratedObject(Object generatedObject)
        {
            if (generatedObject == null) return;

            if (Application.isPlaying)
                Destroy(generatedObject);
            else
                DestroyImmediate(generatedObject);
        }
    }
}

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
        private static readonly Color32 TRANSPARENT = new(0, 0, 0, 0);
        private static readonly Color32 DEFAULT_LINE_COLOR = new(18, 18, 24, 255);

        // ── Runtime State ───────────────────────────────────────────────────────

        private Material _instanceMaterial;
        private bool _hasLoggedTransparentMaterialWarning;

        // ── Public Properties ───────────────────────────────────────────────────

        /// <summary>
        /// The RenderTexture where characters are rendered.
        /// </summary>
        public RenderTexture RenderTarget => _renderTarget;

        /// <summary>
        /// Whether the generator is properly configured with character bases.
        /// </summary>
        public bool IsConfigured => _characterBases != null && _characterBases.Length > 0;

        // ── Unity Lifecycle ─────────────────────────────────────────────────────

        private void OnDestroy()
        {
            // Clean up the material instance
            if (_instanceMaterial != null)
            {
                Destroy(_instanceMaterial);
                _instanceMaterial = null;
            }
        }

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

            if (charBase.baseMaterial == null)
            {
                Debug.LogWarning("[VisitorCharacterGenerator] Character base has no recolor material. Falling back to base texture.");
                RenderCpuRecoloredFallback(charBase, RandomColor(charBase.skinColors),
                    RandomColor(charBase.hairColors), RandomColor(charBase.clothesColors));
                return;
            }

            // 2. Create or reuse material instance
            if (_instanceMaterial == null)
            {
                _instanceMaterial = new Material(charBase.baseMaterial);
            }
            else
            {
                _instanceMaterial.CopyPropertiesFromMaterial(charBase.baseMaterial);
            }

            // 3. Set textures
            if (charBase.colorTexture != null)
                _instanceMaterial.SetTexture("_ColorTexture", charBase.colorTexture);

            if (charBase.lineArtTexture != null)
                _instanceMaterial.SetTexture("_LineTexture", charBase.lineArtTexture);

            // 4. Apply random colors from palettes
            Color skinColor = RandomColor(charBase.skinColors);
            Color hairColor = RandomColor(charBase.hairColors);
            Color clothesColor = RandomColor(charBase.clothesColors);

            _instanceMaterial.SetColor("_SkinColor", skinColor);
            _instanceMaterial.SetColor("_HairColor", hairColor);
            _instanceMaterial.SetColor("_ClothesColor", clothesColor);

            _instanceMaterial.SetColor("_LineColor", charBase.lineColor);

            // 5. Blit to RenderTexture
            Graphics.Blit(charBase.colorTexture, _renderTarget, _instanceMaterial);

            if (!RenderTargetHasVisiblePixels())
            {
                if (!_hasLoggedTransparentMaterialWarning)
                {
                    Debug.LogWarning("[VisitorCharacterGenerator] Recolor material produced a fully transparent visitor. Falling back to CPU recolor.");
                    _hasLoggedTransparentMaterialWarning = true;
                }

                RenderCpuRecoloredFallback(charBase, skinColor, hairColor, clothesColor);
            }
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

        private bool RenderTargetHasVisiblePixels()
        {
            if (_renderTarget == null) return false;

            RenderTexture previous = RenderTexture.active;
            Texture2D readableTexture = new Texture2D(
                _renderTarget.width,
                _renderTarget.height,
                TextureFormat.RGBA32,
                false);

            try
            {
                RenderTexture.active = _renderTarget;
                readableTexture.ReadPixels(new Rect(0, 0, _renderTarget.width, _renderTarget.height), 0, 0);
                readableTexture.Apply(false);

                Color32[] pixels = readableTexture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a > VISIBLE_ALPHA_THRESHOLD)
                        return true;
                }

                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                DestroyGeneratedObject(readableTexture);
            }
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

                    outputPixels[i] = new Color32(line.r, line.g, line.b, lineSource.a);
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
            Color32 targetColor;
            if (source.b > source.r && source.b > source.g)
                targetColor = skin;
            else if (source.r > source.b && source.g > source.b)
                targetColor = hair;
            else
                targetColor = clothes;

            targetColor.a = source.a;
            return targetColor;
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

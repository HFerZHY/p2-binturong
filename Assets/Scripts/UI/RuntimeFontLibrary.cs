using TMPro;
using UnityEngine;

namespace Otowa.UI
{
    public static class RuntimeFontLibrary
    {
        private const string BreeSerifRegularResource = "Fonts/BreeSerif-Regular";

        private static TMP_FontAsset _breeSerifRegular;

        public static TMP_FontAsset BreeSerifRegular
        {
            get
            {
                if (_breeSerifRegular != null)
                    return _breeSerifRegular;

                var font = Resources.Load<Font>(BreeSerifRegularResource);
                if (font == null)
                {
                    Debug.LogWarning($"[RuntimeFontLibrary] Could not load BreeSerif at Resources/{BreeSerifRegularResource}.");
                    return null;
                }

                _breeSerifRegular = TMP_FontAsset.CreateFontAsset(font);
                _breeSerifRegular.name = "Bree Serif Runtime SDF";
                return _breeSerifRegular;
            }
        }

        public static TMP_FontAsset BreeSerifRegularOr(TMP_FontAsset fallback)
        {
            return BreeSerifRegular != null ? BreeSerifRegular : fallback;
        }

        public static void ApplyBreeSerif(TMP_Text text, TMP_FontAsset fallback = null)
        {
            if (text == null)
                return;

            var font = BreeSerifRegularOr(fallback);
            if (font != null)
                text.font = font;
        }
    }
}

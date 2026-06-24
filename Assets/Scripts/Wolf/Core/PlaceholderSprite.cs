using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>1×1 white sprite for placeholder visuals (no character rig).</summary>
    public static class PlaceholderSprite
    {
        static Sprite _white;

        public static Sprite White
        {
            get
            {
                if (_white != null) return _white;
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                var cols = new Color[16];
                for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
                tex.SetPixels(cols);
                tex.Apply();
                _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                return _white;
            }
        }
    }
}

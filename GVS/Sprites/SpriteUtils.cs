using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.Sprites
{
    public static class SpriteUtils
    {
        public static void Draw(this SpriteBatch spr, Sprite s, Vector2 pos, Color color, float depth)
        {
            Draw(spr, s, new Rectangle((int)(pos.X - s.Pivot.X), (int)(pos.Y - s.Pivot.Y), s.Region.Width * s.DrawScale, s.Region.Height * s.DrawScale), color, depth);
        }

        public static void Draw(this SpriteBatch spr, Sprite s, Rectangle destination, Color color, float depth)
        {
            bool useTex = s.Texture != null && !s.Texture.IsDisposed;
            spr.Draw(useTex ? s.Texture : Main.MissingTexture, destination, useTex ? s.Region : new Rectangle(0, 0, 32, 32), color, 0f, Vector2.Zero, SpriteEffects.None, depth);
        }
    }
}

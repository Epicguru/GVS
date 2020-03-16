using Microsoft.Xna.Framework.Graphics;

namespace GVS.World.Tiles
{
    public class GrassTile : Tile
    {
        public override void Draw(SpriteBatch spr)
        {
            BaseSprite = Main.GrassTile;

            base.Draw(spr);
        }
    }
}
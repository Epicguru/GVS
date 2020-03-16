using GVS.Sprites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.World.Tiles.Components
{
    public class Trees : TileComponent
    {
        public override void Draw(SpriteBatch spr)
        {
            Vector2 pos = Map.GetTileDrawPosition(Position + new Point3D(0, 0, 1)).ToVector2();
            spr.Draw(Main.TreeTile, pos, Color.White, GetDrawDepth());
        }
    }
}

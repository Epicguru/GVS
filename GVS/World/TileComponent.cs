using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.World
{
    public class TileComponent
    {
        public Tile Tile { get; internal set; }
        public int Index { get; internal set; } = -1;
        public IsoMap Map
        {
            get
            {
                return Tile.Map;
            }
        }
        public Point3D Position
        {
            get
            {
                return Tile.Position;
            }
        }

        public virtual float GetDrawDepth()
        {
            // Get the draw depth of the tile above. This is where the base (index = 0) component would draw.
            float aboveDepth = Map.GetTileDrawDepth(Position + new Point3D(0, 0, 1));

            // Get a 'nudge' based on our index. Higher indexes draw at higher levels.
            // The nudge value is limited to half of a tile depth, allowing for entities to draw on top of components.
            float indexNudge = Map.SingleTileDepth * 0.5f * ((float) (Index + 1) / (Tile.MaxComponentCount + 1));

            return MathHelper.Clamp(aboveDepth + indexNudge, 0f, 1f);
        }

        protected internal virtual void UponAdded(Tile addedTo)
        {

        }

        protected internal virtual void UponRemoved(Tile removedFrom)
        {

        }

        public virtual void Update()
        {

        }

        public virtual void Draw(SpriteBatch spr)
        {

        }
    }
}

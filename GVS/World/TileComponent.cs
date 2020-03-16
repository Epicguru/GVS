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
            return Map.GetTileDrawDepth(Position + new Point3D(0, 0, 1)) + Map.SingleTileDepth * ((float)Index / Tile.MaxComponentCount);
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

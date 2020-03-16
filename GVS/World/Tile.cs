using GVS.Sprites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.World
{
    public abstract class Tile
    {
        public const bool DEBUG_MODE = true; // TODO remove-me for global mode, or use conditional compilation.

        /// <summary>
        /// The sprite that is rendered as the 'base': as the ground. If null, nothing is drawn apart from
        ///  the components.
        /// </summary>
        public Sprite BaseSprite { get; protected internal set; }
        /// <summary>
        /// The 'permanent' tint that the ground is drawn with. This ONLY affects the ground sprite, not the components.
        /// </summary>
        public Color BaseSpriteTint { get; set; } = Color.White;
        /// <summary>
        /// The 'temporary' tint that the ground is drawn with. When this tile is drawn,
        /// this value is multiplied by the <see cref="BaseSpriteTint"/> tint and then the tile is drawn with the resulting color.
        /// Once the tile is drawn, this tint is reset to white. This allows for easy manipulation of ground color.
        /// </summary>
        public Color TemporarySpriteTint { get; set; } = Color.White;
        public Point3D Position { get; internal set; }
        public IsoMap Map { get; internal set; }
        public int MaxComponentCount
        {
            get
            {
                return components.Length;
            }
        }

        private readonly TileComponent[] components = new TileComponent[8];

        public float GetDrawDepth()
        {
            return Map.GetTileDrawDepth(Position);
        }

        protected internal virtual void UponPlaced(IsoMap map)
        {

        }

        protected internal virtual void UponRemoved(IsoMap map)
        {
            // Should this also call the relevant UponRemoved method on all the components?
        }

        public virtual void Update()
        {
            // Update all components.
            foreach (var comp in components)
            {
                // TODO catch or log exceptions.
                comp?.Update();
            }
        }

        public virtual void Draw(SpriteBatch spr)
        {
            // Draw sprite into world.
            if(BaseSprite != null)
            {
                Point drawPos = Map.GetTileDrawPosition(Position);
                Color c = BaseSpriteTint.Multiply(TemporarySpriteTint);
                
                spr.Draw(BaseSprite, drawPos.ToVector2(), c, GetDrawDepth());

                TemporarySpriteTint = Color.White;
            }

            // Draw all components.
            foreach (var comp in components)
            {
                // TODO catch or log exceptions.
                comp?.Draw(spr);
            }
        }

        public bool CanAddComponent(TileComponent tc, int index)
        {
            return CanAddComponent(tc, index, out int _);
        }

        public bool CanAddComponent(TileComponent tc, int index, out int errorCode)
        {
            if (index < 0 || index >= components.Length)
            {
                // Index out of bounds!
                errorCode = 0;
                return false;
            }
            if (tc == null)
            {
                // Null component!
                errorCode = 1;
                return false;
            }
            if (tc.Tile != null)
            {
                // Component already has parent!
                errorCode = 2;
                return false;
            }
            if (components[index] != null)
            {
                // Component slot is not empty! (slot is already occupied)
                errorCode = 3;
                return false;
            }

            errorCode = -1;
            return true;
        }

        public bool AddComponent(TileComponent tc, int index, bool sendMessage = true)
        {
            bool canAdd = CanAddComponent(tc, index);
            if (!canAdd)
            {
                Debug.Error($"Cannot add component {tc} to tile {this} at index {index}!");
                if (DEBUG_MODE)
                {
                    CanAddComponent(tc, index, out int code);
                    Debug.Error($"Error code: {code}");
                }
                return false;
            }

            // Tell the component that we are it's parent (also 'assigns' position).
            tc.Tile = this;
            tc.Index = index;

            // Write it to the array.
            components[index] = tc;

            // Tell the component that it was just added.
            if (sendMessage)
            {
                tc.UponAdded(this);
            }

            return true;
        }

        public bool RemoveComponent(int index, bool sendMessage = true)
        {
            if (index < 0 || index >= components.Length)
            {
                Debug.Error($"Index {index} is out of bounds for removal of a component. Min: 0, Max: {components.Length - 1} inclusive.");
                return false;
            }

            // Tell the component that is is about to be removed, if it is not null.
            var current = components[index];
            if(current != null)
            {
                current.Index = -1;
                if (sendMessage)
                    current?.UponRemoved(this);
            }
            

            // Clear from the array.
            components[index] = null;

            return true;
        }
    }
}

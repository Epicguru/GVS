using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.Entities
{
    public abstract class Entity
    {
        #region Static

        private static readonly List<Entity> activeEntities = new List<Entity>();
        private static readonly List<Entity> pendingEntities = new List<Entity>();

        private static void Register(Entity e)
        {
            if (e == null)
            {
                Debug.Error("Cannot register null entity!");
                return;
            }
            if (e.internalState != 0)
            {
                Debug.Error($"Entity {e} is not in the correct state (expected 0 got {e.internalState}), cannot register again.");
                return;
            }
            if (e.IsDestroyed)
            {
                Debug.Error("Entity is already destroyed, it cannot be registered now!");
                return;
            }

            // Update state and add to the pending list. The state variable allow the skipping of checking the array.
            e.internalState = 1;
            pendingEntities.Add(e);
        }

        internal static void UpdateAll()
        {
            // Add pending entities.
            foreach (var entity in pendingEntities)
            {
                if(entity != null && !entity.IsDestroyed && entity.internalState == 1)
                {
                    entity.internalState = 2;
                    activeEntities.Add(entity);
                }
                else if(entity != null)
                {
                    entity.IsDestroyed = true;
                    entity.internalState = 3;
                }
            }
            pendingEntities.Clear();

            for (int i = 0; i < activeEntities.Count; i++)
            {
                var entity = activeEntities[i];

                if (entity.IsDestroyed)
                {
                    activeEntities.RemoveAt(i);
                    i--;
                    continue;
                }

                // TODO catch exceptions and handle.
                entity.Update();
            }
        }

        internal static void DrawAll(SpriteBatch spr)
        {
            foreach (var entity in activeEntities)
            {
                if (entity.IsDestroyed)
                    continue;

                // TODO catch exceptions and handle.
                entity.Draw(spr);
            }
        }

        internal static void DrawAllUI(SpriteBatch spr)
        {
            foreach (var entity in activeEntities)
            {
                if (entity.IsDestroyed)
                    continue;

                // TODO catch exceptions and handle.
                entity.DrawUI(spr);
            }
        }

        #endregion

        public string Name { get; protected set; } = "No-name";
        public Vector2 Position;
        public bool IsDestroyed { get; private set; } = false;

        /// <summary>
        /// The internal state of the entity, related to how it is registered, updated and removed from the world.
        /// <para>0: None. Entity has been instantiated but nothing else. </para>
        /// <para>1: Entity has been registered and is now pending entry to world. </para>
        /// <para>2: Entity has now moved out of the pending list and is now being updated. </para>
        /// <para>3: Entity has been removed from the world. Note that the entity can still be 'destroyed' even if the state is not 3: check the <see cref="IsDestroyed"/> flag. </para>
        /// </summary>
        private byte internalState = 0;

        /// <summary>
        /// Causes this entity to be spawned into the world. If your entity isn't showing up or rendering,
        /// make sure this has been called.
        ///<para></para>
        /// Repeated calls, or calls when entity is in invalid state (such as destroyed)
        /// will have no effect and will not log an error.
        /// </summary>
        public void Activate()
        {
            if (IsDestroyed)
                return;

            if (internalState != 0)
                return;

            Register(this);
        }
        
        /// <summary>
        /// Called once per frame to update the entity's logic.
        /// </summary>
        protected virtual void Update()
        {
            
        }

        /// <summary>
        /// Called once per frame to draw the entity into the world. Avoid 'logic' code in here, such as Input
        /// or movement.
        /// </summary>
        /// <param name="spr">The SpriteBatch to draw the entity with. Positions and sizes will be in world-space.</param>
        protected virtual void Draw(SpriteBatch spr)
        {

        }

        /// <summary>
        /// Called once per frame to draw UI. This can be used to draw any kind of UI, but in-world UI is preferred
        /// since the draw order of this is basically random.
        /// </summary>
        /// <param name="spr">The SpriteBatch to draw with. Positions and sizes will be in screen-space.</param>
        protected virtual void DrawUI(SpriteBatch spr)
        {

        }

        public override string ToString()
        {
            return Name ?? "null-name";
        }
    }
}

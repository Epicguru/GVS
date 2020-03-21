using Microsoft.Xna.Framework.Graphics;

namespace GVS.Screens
{
    public abstract class GameScreen
    {
        public string Name { get; protected set; }

        protected GameScreen(string name)
        {
            this.Name = name;
        }

        public virtual void Load() { }
        public virtual void Unload() { }

        public virtual void UponShow() { }
        public virtual void UponHide() { }

        public virtual void Update() { }
        public virtual void Draw(SpriteBatch sb) { }
        public virtual void DrawUI(SpriteBatch sb) { }
    }
}

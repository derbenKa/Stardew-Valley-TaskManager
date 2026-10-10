using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TaskManager.UI
{
    /// <summary>One tab of the TaskManager menu.</summary>
    internal abstract class Page
    {
        public Rectangle Bounds { get; private set; }

        /// <summary>Tooltip to show this frame, set while drawing.</summary>
        public string? HoverText { get; protected set; }

        public virtual bool IsTyping => false;

        public virtual void SetBounds(Rectangle bounds) => Bounds = bounds;

        public virtual void OnActivate() { }

        public virtual void OnDeactivate() => StopTyping();

        public virtual void StopTyping() { }

        public abstract void Draw(SpriteBatch b);

        public virtual void LeftClick(int x, int y) { }

        public virtual void Scroll(int direction, int mouseX, int mouseY) { }
    }
}

namespace Flusher.Uwp.Vision
{
    public sealed class BoundingBox
    {
        public BoundingBox(float left, float top, float width, float height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }

        public float Left { get; private set; }
        public float Top { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
    }
}
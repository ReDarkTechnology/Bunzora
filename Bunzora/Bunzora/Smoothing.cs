using System.Numerics;

namespace Bunzora
{
    public interface ISmoothing
    {
        public Vector2 Stroke(Vector2 position, int intensity, int maxHistory);
        public void EndStroke();
    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Bunzora
{
    public class Stylus
    {
        public static Func<Stylus> CreateStylus;

        public Action<Vector2, float> OnStylusMove;
        public Action<Vector2, float> OnStylusDown;
        public Action OnStylusUp;

        /// <summary>
        /// Initializes a new instance of the <see cref="Stylus"/> class.
        /// </summary>
        /// <param name="hwnd">The result of <see cref="Raylib.GetWindowHandle"/></param>
        public virtual void Hook(IntPtr hwnd)
        {

        }
    }
}

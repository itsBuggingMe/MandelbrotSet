using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace MandelbrotSet
{
    internal class QuadRenderer
    {
        private readonly VertexPositionColor[] _vertBuffer = new VertexPositionColor[4];
        private static readonly short[] _indexBuffer = new short[] { 0, 1, 2, 1, 3, 2 };

        public QuadRenderer(Vector2 topLeft, Vector2 bottomRight)
        {
            SetSize(topLeft, bottomRight);
        }

        public QuadRenderer() : this(-Vector2.One, Vector2.One) { }

        public void Draw(GraphicsDevice device)
        {
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertBuffer, 0, 4, _indexBuffer, 0, 2);
        }


        //top left, top right, bottom left, bottom right
        public void SetSize(Vector2 tl, Vector2 br)
        {
            Set(0, tl.X, tl.Y);
            Set(1, br.X, tl.Y);
            Set(2, tl.X, br.Y);
            Set(3, br.X, br.Y);
        }

        void Set(int i, float x, float y) => _vertBuffer[i] = new VertexPositionColor(new Vector3(x, y, 1), Color.White);
    }
}

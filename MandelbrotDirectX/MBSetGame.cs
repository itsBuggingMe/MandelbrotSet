using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MandelbrotSet
{
    public class MBSetGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private QuadRenderer _quadDrawer;
        private SpriteBatch _spriteBatch;
        private RenderTarget2D _tempTarget;

        private Effect _mandelbrotShaderOther;
        private Effect _mandelbrotShaderActive;

        private (EffectParameter, EffectParameter) _worldPositionX;
        private (EffectParameter, EffectParameter) _worldPositionY;
        private (EffectParameter, EffectParameter) _worldPositionZ;
        private double _camX = -1225.5021;
        private double _camY = -510.8379;
        private double _camZ = 0.0021845647f;

        private EffectParameter _colorMapExp;
        private int _colorMapExpValue = 2;

        private SpriteFont _font;

        public MBSetGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        }

        protected override void Initialize()
        {
            _quadDrawer = new();
            _spriteBatch = new(GraphicsDevice);
            _mandelbrotShaderActive = Content.Load<Effect>("mbShader");
            _mandelbrotShaderOther = Content.Load<Effect>("mbShaderRAINBOW");

            UpdateParameters();

            _font = Content.Load<SpriteFont>("font");

            _tempTarget = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            base.Initialize();
            ToggleBorderless();
        }

        private void UpdateParameters()
        {
            _worldPositionX = (_mandelbrotShaderActive.Parameters["worldPositionXa"], _mandelbrotShaderActive.Parameters["worldPositionXb"]);
            _worldPositionY = (_mandelbrotShaderActive.Parameters["worldPositionYa"], _mandelbrotShaderActive.Parameters["worldPositionYb"]);
            _worldPositionZ = (_mandelbrotShaderActive.Parameters["worldPositionZa"], _mandelbrotShaderActive.Parameters["worldPositionZb"]);
            _colorMapExp = _mandelbrotShaderActive.Parameters["colorMapExp"];
        }

        int prevScrollValue = 0;
        Point prevMouseLoc;
        KeyboardState prevKb;

        bool needsRefresh = true;
        int refreshes = 0;


        protected override void Update(GameTime gameTime)
        {
            //input
            KeyboardState kb = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            int oldQEval = _colorMapExpValue;
            (double, double, double) oldCamPos = (_camX, _camY, _camZ);

            float moveMulti = MathF.Sqrt((float)_camZ);

            if (!prevKb.IsKeyDown(Keys.F11) && kb.IsKeyDown(Keys.F11))
                ToggleBorderless();

            if (!prevKb.IsKeyDown(Keys.Q) && kb.IsKeyDown(Keys.Q))
                _colorMapExpValue++;
            if (!prevKb.IsKeyDown(Keys.E) && kb.IsKeyDown(Keys.E))
                _colorMapExpValue--;

            if (kb.IsKeyDown(Keys.Escape))
                Exit();

            if (!prevKb.IsKeyDown(Keys.W) && kb.IsKeyDown(Keys.W))
            {
                (_mandelbrotShaderActive, _mandelbrotShaderOther) = (_mandelbrotShaderOther, _mandelbrotShaderActive);
                UpdateParameters();
                needsRefresh = true;
            }

            if (ms.LeftButton == ButtonState.Pressed)
            {
                Point off = (prevMouseLoc - ms.Position);
                _camX += off.X;
                _camY += off.Y;
            }

            double oldZoom = _camZ;
            if (kb.IsKeyDown(Keys.I) || ms.ScrollWheelValue - prevScrollValue < 0)
                _camZ *= 1.15f;
            if (kb.IsKeyDown(Keys.J) || ms.ScrollWheelValue - prevScrollValue > 0)
                _camZ *= 0.85f;

            if(_camZ < 0)
                _camZ = 0.0001f;
            if(oldZoom != _camZ)
            {
                ZoomChanged(oldZoom, _camZ, ms.Position.ToVector2());
            }
            
            if((_camX, _camY, _camZ) != oldCamPos || _colorMapExpValue != oldQEval)
            {
                needsRefresh = true;
            }

            //Debug.WriteLine(_camLoc);
            //prep next frame
            prevScrollValue = ms.ScrollWheelValue;
            prevMouseLoc = ms.Position;
            prevKb = kb;
            base.Update(gameTime);
        }

        private void ZoomChanged(double old, double @new, Vector2 target)
        {
            // @new * (target + world + offset) == old * (target + world)
            _camX += ((old - @new) * (target.X + _camX)) / @new;
            _camY += ((old - @new) * (target.Y + _camY)) / @new;
        }

        protected override void Draw(GameTime gameTime)
        {
            if(!needsRefresh)
            {
                ApplyRenderTarget();
                return;
            }
            needsRefresh = false;
            refreshes++;

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.BlendState = BlendState.Additive;

            GraphicsDevice.SetRenderTarget(_tempTarget);
            //drawing
            //SSAA never worked well
            DrawMBQuad(0,0);

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, $"Color Exp (Q & E): {_colorMapExpValue}\nCamera Position (Click + Drag): {_camX}, {_camY}\nZoom (Scroll): {1 / _camZ}\nRefreshes: {refreshes}\nToggle Mode(W)", Vector2.One * 16, Color.White);
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            ApplyRenderTarget();
            void ApplyRenderTarget()
            {
                GraphicsDevice.SetRenderTarget(null);
                _spriteBatch.Begin();
                _spriteBatch.Draw(_tempTarget, Vector2.Zero, Color.White);
                _spriteBatch.End();
            }

            void DrawMBQuad(double offsetX, double offsetY)
            {
                _colorMapExp.SetValue(_colorMapExpValue);

                _worldPositionX.Item1.SetValue(new DoubleToInt(offsetY + _camX).IntegerA);
                _worldPositionX.Item2.SetValue(new DoubleToInt(offsetY + _camX).IntegerB);
                _worldPositionY.Item1.SetValue(new DoubleToInt(offsetX + _camY).IntegerA);
                _worldPositionY.Item2.SetValue(new DoubleToInt(offsetX + _camY).IntegerB);
                _worldPositionZ.Item1.SetValue(new DoubleToInt(_camZ).IntegerA);
                _worldPositionZ.Item2.SetValue(new DoubleToInt(_camZ).IntegerB);
                
                _mandelbrotShaderActive.CurrentTechnique.Passes[0].Apply();
                _quadDrawer.Draw(GraphicsDevice);
            }

            base.Draw(gameTime);
        }
        


        private void ToggleBorderless()
        {
            if (Window.IsBorderless)
            {
                Window.IsBorderless = false;

                Window.Position = (new Point(_graphics.GraphicsDevice.DisplayMode.Width, _graphics.GraphicsDevice.DisplayMode.Height).ToVector2() * 0.5f - new Vector2(400, 240)).ToPoint();

                _graphics.PreferredBackBufferWidth = 800;
                _graphics.PreferredBackBufferHeight = 480;
            }
            else
            {
                Window.IsBorderless = true;
                _graphics.PreferredBackBufferWidth = _graphics.GraphicsDevice.DisplayMode.Width;
                _graphics.PreferredBackBufferHeight = _graphics.GraphicsDevice.DisplayMode.Height;
                Window.Position = Point.Zero;
            }

            _graphics.ApplyChanges();
            Window_ClientSizeChanged(this, EventArgs.Empty);
        }

        public void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            _graphics.ApplyChanges();

            _tempTarget = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            needsRefresh = true;
        }
        [StructLayout(LayoutKind.Explicit)]
        private readonly struct DoubleToInt
        {
            [FieldOffset(0)]
            public readonly double Double;
            [FieldOffset(0)]
            public readonly int IntegerA;
            [FieldOffset(4)]
            public readonly int IntegerB;
            public DoubleToInt(double d)
            {
                IntegerA = default;
                IntegerB = default;
                Double = d;
            }
        }
    }

    internal static class Vector3Exenstions
    {
        public static Vector2 XY(this Vector3 vector3)
        {
            return new Vector2(vector3.X, vector3.Y);
        }
    }
}

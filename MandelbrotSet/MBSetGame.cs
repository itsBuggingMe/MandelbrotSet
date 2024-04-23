using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;

namespace MandelbrotSet
{
    public class MBSetGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private QuadRenderer _quadDrawer;
        private SpriteBatch _spriteBatch;

        private Effect _mandelbrotShader;

        private EffectParameter _transform;
        private Vector3 _camLoc = new Vector3(-1225.5021f, -510.8379f, 0.0021845647f);

        private EffectParameter _colorMapExp;
        private int _colorMapExpValue = 4;

        private SpriteFont _font;

        public MBSetGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
        }

        protected override void Initialize()
        {
            _quadDrawer = new();
            _spriteBatch = new(GraphicsDevice);
            _mandelbrotShader = Content.Load<Effect>("mbShader");
            _font = Content.Load<SpriteFont>("font");
            _transform = _mandelbrotShader.Parameters["worldPosition"];
            _colorMapExp = _mandelbrotShader.Parameters["colorMapExp"];
            base.Initialize();
            ToggleBorderless();
        }

        int prevScrollValue = 0;
        Point prevMouseLoc;

        KeyboardState prevKb;

        protected override void Update(GameTime gameTime)
        {
            //input
            KeyboardState kb = Keyboard.GetState();
            MouseState ms = Mouse.GetState();

            const float moveSpeed = -1f;

            float moveMulti = MathF.Sqrt(_camLoc.Z);

            if (!prevKb.IsKeyDown(Keys.F11) && kb.IsKeyDown(Keys.F11))
                ToggleBorderless();

            if (!prevKb.IsKeyDown(Keys.Q) && kb.IsKeyDown(Keys.Q))
                _colorMapExpValue++;
            if (!prevKb.IsKeyDown(Keys.E) && kb.IsKeyDown(Keys.E))
                _colorMapExpValue--;

            if (kb.IsKeyDown(Keys.Escape))
                Exit();
            if (kb.IsKeyDown(Keys.W))
                _camLoc.Y -= moveSpeed * moveMulti;
            if (kb.IsKeyDown(Keys.S))
                _camLoc.Y += moveSpeed * moveMulti;
            if (kb.IsKeyDown(Keys.A))
                _camLoc.X += moveSpeed * moveMulti;
            if (kb.IsKeyDown(Keys.D))
                _camLoc.X -= moveSpeed * moveMulti;
            if (ms.LeftButton == ButtonState.Pressed)
                _camLoc += new Vector3((ms.Position - prevMouseLoc).ToVector2() * new Vector2(-1, 1),0);



            float oldZoom = _camLoc.Z;
            if (kb.IsKeyDown(Keys.I) || ms.ScrollWheelValue - prevScrollValue < 0)
                _camLoc.Z *= 1.1f;
            if (kb.IsKeyDown(Keys.J) || ms.ScrollWheelValue - prevScrollValue > 0)
                _camLoc.Z *= 0.9f;

            if(_camLoc.Z < 0)
                _camLoc.Z = 0.0001f;
            if(oldZoom != _camLoc.Z)
            {
                ZoomChanged(oldZoom, _camLoc.Z, ms.Position.ToVector2());
            }

            _transform.SetValue(_camLoc);
            _colorMapExp.SetValue(_colorMapExpValue);
            //Debug.WriteLine(_camLoc);
            //prep next frame
            prevScrollValue = ms.ScrollWheelValue;
            prevMouseLoc = ms.Position;
            prevKb = kb;
            base.Update(gameTime);
        }

        private void ZoomChanged(float old, float @new, Vector2 target)
        {
            // @new * (target + world + offset) == old * (target + world)

            Vector2 offset = ((old - @new) * (target + _camLoc.XY())) / @new;
            _camLoc = new Vector3(new Vector2(_camLoc.X, _camLoc.Y) + offset, _camLoc.Z);//still broken but close enuff to working

        }

        protected override void Draw(GameTime gameTime)
        {
            //drawing
            _mandelbrotShader.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            _quadDrawer.SetSize(-Vector2.One, Vector2.One);
            _quadDrawer.Draw(GraphicsDevice);

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, $"Color Exp (Q): {_colorMapExpValue}\nCamera Position (WASD / Click + Drag): {_camLoc.X}, {_camLoc.Y}\nZoom (Scroll): {1 / _camLoc.Z}", Vector2.One * 16, Color.White);
            _spriteBatch.End();

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

            Window_ClientSizeChanged(this, null);
        }

        public void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            _graphics.ApplyChanges();
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

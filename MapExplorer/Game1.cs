using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MapExplorer
{
    public class Game1 : Game
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _textureMap;
        private Texture2D _rtMap;
        private Texture2D _currentMap;

        // The zoom of the map
        private float _zoom = 1f;
        // The offset into the map without scaling applied
        private Vector2 _offset = Vector2.Zero;

        // a 1x1 white texture for drawing colored rectangles
        private Texture2D _blankTexture;

        private KeyboardState _current;
        private KeyboardState _prev;
        private bool _miniMap = true;

        private Rectangle SourceRect => new Rectangle(
            (int) (_offset.X),
            (int) (_offset.Y),
            (int) (GraphicsDevice.PresentationParameters.BackBufferWidth / _zoom),
            (int) (GraphicsDevice.PresentationParameters.BackBufferHeight / _zoom));

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _blankTexture = new Texture2D(GraphicsDevice, 1, 1);
            _blankTexture.SetData(new[] { Color.White.PackedValue });

            _textureMap = Content.Load<Texture2D>("map");
            _rtMap = CreateMap();
            _currentMap = _rtMap;

            _prev = Keyboard.GetState();
            _current = Keyboard.GetState();
        }

        private Texture2D CreateMap()
        {
            const int size = 4096;
            var map = new RenderTarget2D(GraphicsDevice, size, size);

            GraphicsDevice.SetRenderTarget(map);

            _spriteBatch.Begin();

            var random = new Random(420);
            const int tileSize = 32;
            for (var x = 0; x < map.Width / tileSize; x++)
            {
                for (var y = 0; y < map.Height / tileSize; y++)
                {
                    var rect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    var color = new Color(random.Next(256), random.Next(256), random.Next(256));
                    _spriteBatch.Draw(_blankTexture, rect, color);
                }
            }

            _spriteBatch.End();

            // unset the map render target
            GraphicsDevice.SetRenderTarget(null);

            return map;
        }

        protected override void Update(GameTime gameTime)
        {
            HandleInput();
        }

        private void HandleInput()
        {
            _current = Keyboard.GetState();

            if (IsDown(Keys.Escape))
                Exit();

            // zooming
            const float zoomFactor = 2f;
            if (IsPressed(Keys.X))
                _zoom /= zoomFactor;
            if (IsPressed(Keys.Z))
                _zoom *= zoomFactor;

            _zoom = MathHelper.Clamp(_zoom, 1f / 4f, 4f);

            // scrolling
            var speed = 10 / _zoom;
            if (IsDown(Keys.Right))
                _offset.X += speed;
            if (IsDown(Keys.Left))
                _offset.X -= speed;
            if (IsDown(Keys.Down))
                _offset.Y += speed;
            if (IsDown(Keys.Up))
                _offset.Y -= speed;

            _offset.X = MathHelper.Clamp(_offset.X, 0f, _currentMap.Width - GraphicsDevice.PresentationParameters.BackBufferWidth / _zoom);
            _offset.Y = MathHelper.Clamp(_offset.Y, 0f, _currentMap.Height - GraphicsDevice.PresentationParameters.BackBufferHeight / _zoom);

            // toggle the minimap
            if (IsPressed(Keys.H))
                _miniMap = !_miniMap;

            // switch the map
            if (IsPressed(Keys.S))
                _currentMap = _currentMap == _rtMap ? _textureMap : _rtMap;

            _prev = _current;
        }

        private bool IsDown(Keys key)
        {
            return _current.IsKeyDown(key);
        }

        private bool IsPressed(Keys key)
        {
            return _current.IsKeyDown(key) && _prev.IsKeyUp(key);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            // SourceRect gives us the source rectangle of the texture we need to render to the screen
            // We render that to the whole viewport
            var rect = SourceRect;
            _spriteBatch.Draw(_currentMap, GraphicsDevice.Viewport.Bounds, rect, Color.White);

            if (_miniMap)
            {
                var backBufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
                var backBufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
                var mapAspect = (float) _currentMap.Height / _currentMap.Width;

                // pick a width for the minimap and compute the height (to match aspect ratio)
                const int miniMapWidth = 120;
                var miniMapHeight = (int) (miniMapWidth * mapAspect);
                var margin = 20;
                var area = new Rectangle(backBufferWidth - miniMapWidth - margin, backBufferHeight - miniMapHeight - margin, miniMapWidth, miniMapHeight);

                // Draw a nice border
                var expandedArea = new Rectangle(area.Location, area.Size);
                expandedArea.Inflate(2, 2);
                _spriteBatch.Draw(_blankTexture, expandedArea, Color.Black);

                // Draw the minimap
                _spriteBatch.Draw(_currentMap, area, Color.White);

                // draw the rectangle with the current location
                var offsetScale = (float) miniMapWidth / _currentMap.Width; // scale offset [0, Map width] -> [0, minimap width]
                var scaledScreenWidth = backBufferWidth / _zoom; 
                var scaledScreenHeight = backBufferHeight / _zoom; 
                var sizeXScale = scaledScreenWidth / _currentMap.Width;
                var sizeYScale = scaledScreenHeight / _currentMap.Height;
                var miniArea = new Rectangle(
                    area.Location + (_offset * offsetScale).ToPoint(),
                    new Point((int) (miniMapWidth * sizeXScale), (int) (miniMapHeight * sizeYScale) ));

                DrawRect(miniArea, Color.Black);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawRect(Rectangle rect, Color color)
        {
            // draw the 4 edges of the rectangle with a 1 pixel thickness
            _spriteBatch.Draw(_blankTexture, new Rectangle(rect.Left, rect.Top, rect.Width, 1), color);
            _spriteBatch.Draw(_blankTexture, new Rectangle(rect.Right - 1, rect.Top, 1, rect.Height), color);
            _spriteBatch.Draw(_blankTexture, new Rectangle(rect.Left, rect.Bottom - 1, rect.Width, 1), color);
            _spriteBatch.Draw(_blankTexture, new Rectangle(rect.Left, rect.Top, 1, rect.Height), color);
        }
    }
}

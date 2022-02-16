using Alphastorms.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Concurrent;

namespace Alphastorms.Client
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        BasicNetworkGameClient netClient;
        SpriteFont font;
        Vector2 fontLocation;
        ConcurrentQueue<string> _messageQueue;
        string _lastMessage;

        PlayerData[] _otherPlayers;
        PlayerData _self;
        Texture2D _tile;

        public PlayerData GetPlayerData() => _self;
        public PlayerData[] GetPlayers() => _otherPlayers;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _messageQueue = new ConcurrentQueue<string>();
            //int our list of other players
            _otherPlayers = new PlayerData[4];
            for (int i = 0; i < _otherPlayers.Length; i++)
            {
                //init self
                _otherPlayers[i] = new PlayerData(new Point(32, 64));
                _otherPlayers[i].TextureName = "robit";
                _otherPlayers[i].Location = new Point(0, 0);
                _otherPlayers[i].BoundingBox = new Rectangle(_otherPlayers[i].Location.X, _otherPlayers[i].Location.Y, 32, 64);
                _otherPlayers[i].IsPresent = false;
                _otherPlayers[i].PlayerId = i + 1;
            }
        }

        public void AddToMessageQueue(string message)
        {
            _messageQueue.Enqueue(message);
        }

        public string GetLastMessage()
        {
            if (_messageQueue.IsEmpty)
            {
                return _lastMessage;
            }

            string temp;
            bool success = _messageQueue.TryDequeue(out temp);
            if (success)
            {
                _lastMessage = temp;
            }

            return _lastMessage;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            fontLocation = Vector2.Zero;
            // TODO: Add your initialization logic here
            netClient = new BasicNetworkGameClient();
            netClient.AddLoggingFunc(AddToMessageQueue);
            netClient.RegisterPlayerHandler(GetPlayerData);
            netClient.RegisterAllPlayersHandler(GetPlayers);
            netClient.Start();

            //init self
            _self = new PlayerData(new Point(32, 64));
            _self.TextureName = "robit";
            _self.Location = new Point(20, 20);
            _self.BoundingBox = new Rectangle(_self.Location.X, _self.Location.Y, 32, 64);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            font = Content.Load<SpriteFont>("gameFont");

            _tile = Content.Load<Texture2D>("sandtile1");
            //content manager internally caches so we're preloading to ask for them later
            Content.Load<Texture2D>("robit");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            //move down
            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                _self.Location.Y -= _self.MovementSpeed;
                SendClientUpdate(PlayerActions.PRESS_UP);
            }

            //move down
            if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                _self.Location.Y += _self.MovementSpeed;
                SendClientUpdate(PlayerActions.PRESS_DOWN);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                _self.Location.X -= _self.MovementSpeed;
                SendClientUpdate(PlayerActions.PRESS_LEFT);
            }

            //move down
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                _self.Location.X += _self.MovementSpeed;
                SendClientUpdate(PlayerActions.PRESS_RIGHT);
            }

            _self.BoundingBox = new Rectangle(_self.Location, _self.Size);

            //update all active player bounding boxes
            foreach (var player in _otherPlayers)
            {
                if (player.IsPresent)
                {
                    player.BoundingBox = new Rectangle(player.Location, player.Size);
                }
            }

            netClient.Update(gameTime, Keyboard.GetState().GetPressedKeys());


            base.Update(gameTime);
        }
        private void SendClientUpdate(PlayerActions playerAction)
        {
            //we send actions rather than direct keys because the player could have remapped the keys to whatever they find useful
            // server only cares about what action the client took.
            netClient.SendClientActions(playerAction);
        }
        protected Texture2D GetTexture(string name)
        {
            return Content.Load<Texture2D>(name);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            // TODO: Add your drawing code here
            _spriteBatch.Begin();

            _spriteBatch.DrawString(font, $"Server Sent:{GetLastMessage()}", fontLocation, Color.Black);

            //draw self 
            _spriteBatch.Draw(GetTexture(_self.TextureName), _self.BoundingBox, Color.White);

            //draw any other known players
            foreach (var player in _otherPlayers)
            {
                if (player.IsPresent && player.PlayerId != _self.PlayerId)
                {
                    _spriteBatch.Draw(GetTexture(player.TextureName), player.BoundingBox, Color.Green);
                }
            }


            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Courage.AnimTool;

using MonoSkelly.Editor;
using Microsoft.Xna.Framework.Input;

namespace Courage.AnimTool;

public class MainWindowViewModel : MonoGameViewModel
{
    //private SpriteBatch _spriteBatch = default!;
    //private Texture2D _texture = default!;
    //private Vector2 _position;
    //private float _rotation;
    //private Vector2 _origin;
    //private Vector2 _scale;
    private float _rotationSign = 1;

    private SkellyEditor _editor;

    public override void Initialize()
    {
        base.Initialize();
		_editor = new SkellyEditor(GraphicsDevice);
        //Mouse.PrimaryWindow
        _editor.Services.AddService(GraphicsDeviceService);
        _editor.CallProtectedInitialize();

	}


	public override void LoadContent()
    {
        base.LoadContent();
        _editor.CallProtectedLoadContent();
        // Run one frame to activate the platform
        _editor.RunOneFrame();
    }

    public override void OnMouseUp(MouseStateArgs mouseState)
    {
        //_rotationSign *= -1;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        //_editor.RunOneFrame();
        _editor.CallProtectedUpdate(gameTime);

        //_position = GraphicsDevice.Viewport.Bounds.Center.ToVector2();
        //_rotation = (float)Math.Sin(_rotationSign*gameTime.TotalGameTime.TotalSeconds) / 4f;
        //_origin = _texture.Bounds.Center.ToVector2();
        //_scale = Vector2.One;
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        //GraphicsDevice.Clear(Color.CornflowerBlue);
        _editor.CallProtectedDraw(gameTime);

        //_spriteBatch.Begin();
        //_spriteBatch.Draw(_texture, _position, null, Color.White, _rotation, _origin, _scale, SpriteEffects.None, 0f);
        //_spriteBatch.End();
    }
}

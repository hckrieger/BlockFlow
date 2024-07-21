using BlockFlow.Scenes;
using EC;
using EC.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BlockFlow
{
	public class Game1 : ExtendedGame
	{
		public const string playingScene = "Main_Playing_Scene";

		public Game1()
		{
			IsMouseVisible = true;
		}



		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			base.Initialize();

			SetWindowSize(704, 640);

			SceneManager.AddScene(playingScene, new PlayingScene(this));
			SceneManager.ChangeScene(playingScene);

		}
	}
}

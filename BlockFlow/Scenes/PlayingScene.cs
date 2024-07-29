using BlockFlow.Entities;
using EC.Components.Render;
using EC.CoreSystem;
using EC.Services;
using EC.Utilities.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Transactions;

namespace BlockFlow.Scenes
{
    internal class PlayingScene : Scene
	{

		public Block[,] BlockGrid;

		public const int gridWidth = 10;
		public const int gridHeight = 20;
		public const int blockSize = 32;

		private Entity graphicalGridArea;
		private Entity gridParentPosition;


		private GameManager gameManager;
		private Shape activeShape;
		//private float timer = 1f;
		private int blockBlinkingCounter = 0;

		private float clearTimer = .275f;
		private float startClearTimer;
	//	private float startTimer;

		private int distanceToDrop = 0;

		private bool hasBlocksToRemove = false;

		private InputManager inputManager;

		private Entity scoreText, linesText, levelsText;
		private int score, lines, levels;
		private int linesCleared = 0;

		private bool isSoftDropping = false;


		public PlayingScene(Game game) : base(game)
        {
			
			gridParentPosition = new Entity(game);
			gridParentPosition.Transform.LocalPosition = new Vector2(50, 0);


			graphicalGridArea = new Entity(game);
			graphicalGridArea.LoadRectangleComponents("graphical grid area", blockSize*gridWidth, blockSize*gridHeight, Color.AntiqueWhite, game);


			AddEntity(graphicalGridArea, gridParentPosition);

			

		}

		public override void Initialize()
		{
			base.Initialize();

			BlockGrid = new Block[gridWidth, gridHeight];

			inputManager = Game.Services.GetService<InputManager>();

			activeShape = new Shape(Game);
			activeShape.HasHitLandingSpot += CheckRowMatches;
			activeShape.AddPointsFromDrop += AddToScore;
		//	activeShape.CutOffClearTimerEvent += CutOffClearTimer;

			gameManager = new GameManager(AddEntity, BlockGrid, activeShape, Game);
			gameManager.CheckShapeToBeGenerated += CheckShapeToBeGenerated;
			
			SpawnNewShape();
			

			AddEntity(activeShape, gridParentPosition);

			//startTimer = timer;
			startClearTimer = clearTimer;

			

			scoreText = new Entity(Game);
			scoreText.LoadTextComponents("Fonts/Score", $"Score: {score}", Color.White, Game);

			scoreText.Transform.LocalPosition = new Vector2(600, 100);
			AddEntity(scoreText);

			linesText = new Entity(Game);
			linesText.LoadTextComponents("Fonts/Score", $"Lines: {lines}", Color.White, Game);
			linesText.Transform.LocalPosition = new Vector2(600, 150);
			AddEntity(linesText);

			levelsText = new Entity(Game);
			levelsText.LoadTextComponents("Fonts/Score", $"Level: {levels}", Color.White, Game);
			levelsText.Transform.LocalPosition = new Vector2(600, 200);
			AddEntity(levelsText);

			Reset();

			
		}


		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (gameManager.CurrentGameState == GameManager.GameState.GameOver)
			{
				if (inputManager.KeyJustPressed(Keys.Enter))
				{
					Reset();
				}
				return;
			}


			if (hasBlocksToRemove)
			{

				activeShape.HaltDropTimer = true;

				
				clearTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
				
		


			if (clearTimer <= 0)
			{
				

				for (int y = gridHeight - 1; y >= 0; y--)
				{
					for (int x = 0; x < gridWidth; x++)
					{
						if (BlockGrid[x, y] != null && BlockGrid[x, y].CurrentBlockState == Block.BlockState.SetForRemoval)
							BlockGrid[x, y].Visible = !BlockGrid[x, y].Visible;
					}
				}

				blockBlinkingCounter++;
				clearTimer = startClearTimer;

				if (blockBlinkingCounter == 4)
				{
					blockBlinkingCounter = 0;
					CheckRowsToShiftThenRespawn();
					activeShape.HaltDropTimer = false;
					//activeShape.LandFromHardDrop = false;
				}

			}




		}

		

		private void AddToScore(int pointsToAdd, bool includeLevelMultiplication = true)
		{
		
			score += (pointsToAdd * (includeLevelMultiplication ? levels+1 : 1)); 
			scoreText.GetComponent<TextRenderer>().DynamicText = $"Score: {score}";	
			
		}

		private void AddToLinesTotal(int linesToAdd)
		{
			lines += linesToAdd;
			linesText.GetComponent<TextRenderer>().DynamicText = $"Lines: {lines}";

			if (linesCleared > 0)
			{
				switch (linesCleared)
				{
					case 1: AddToScore(100); break;
					case 2: AddToScore(300); break;
					case 3: AddToScore(500); break;
					case 4: AddToScore(800); break;
				}
			}

			linesCleared = 0;

			var previousLevel = levels;
			
			if (lines > 9)
			{
				levels = (lines / 10);
			} else
			{
				levels = 0;
			}

			float FramesPerCellToSubtract(float frames)
			{
				return (1f / 60) * frames;
			}

			if (levels != previousLevel)
			{
				activeShape.ChangeDropSpeedTimer(FramesPerCellToSubtract(2));
			}

			levelsText.GetComponent<TextRenderer>().DynamicText = $"Level: {levels}";
		

		}
	

		private void CheckRowMatches()
		{
			
			for (int y = gridHeight - 1; y >= 0; y--)
			{
				
				if (RowCheck(y))
				{
					for (int x = 0; x < gridWidth; x++)
					{
						BlockGrid[x, y].CurrentBlockState = Block.BlockState.SetForRemoval;
						hasBlocksToRemove = true;

				
					}
				}

			

			}


			if (!activeShape.LandFromHardDrop && hasBlocksToRemove)
			{
				clearTimer = 0;
			} else
			{
				activeShape.LandFromHardDrop = false;
			}
				

			if (!hasBlocksToRemove)
				 SpawnNewShape();
		}

		private bool RowCheck(int y)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				if (BlockGrid[x, y] == null)
					return false;

				

			}

			return true;
		}





		private void SpawnNewShape()
		{
			
			gameManager.GenerateShape(new Point(4, 0));


		}


		//Checks if a new block is going to be generated on an area that already has a block thus ending the game
		private void CheckShapeToBeGenerated(object sender, ShapeCheckArgs args)
		{
			
			for (int i = 0; i < 4; i++)
			{
				Point positionToCheck = args.SpawnLocation + gameManager.BlockPositions[args.BlockShapeToSpawn][0, i];
				if (gameManager.PositionHasBlock(positionToCheck))
				{
					gameManager.RemoveBlockFromLocation(positionToCheck);
					gameManager.CurrentGameState = GameManager.GameState.GameOver;
					
				}
			}
			
		}


	

		private void CheckRowsToShiftThenRespawn()
		{

			
			for (int y = gridHeight - 1; y >= 0; y--)
			{



				//If every space in the array has a block in it........
				if (RowCheck(y))
				{
					linesCleared++;
					//Then clear the row with all blocks
					ClearFullRow(y);

					//And pull the blocks above that down
					MoveBlocksDown(y);

					//Recursively call this method until it reaches the base case that there are are no filled rows detected.  
					CheckRowsToShiftThenRespawn();
					return;
					

				}


				if (y == 0)
				{
					AddToLinesTotal(linesCleared);	
					hasBlocksToRemove = false;
					SpawnNewShape();
				}
			}

			
		}


		public void ClearFullRow(int y)
		{
			for (int x = 0; x < gridWidth; x++)
				gameManager.RemoveBlockFromLocation(new Point(x, y));

				

			
		}

		public void MoveBlocksDown(int y)
		{
			y--;

			while (y >= 0)
			{
				for (int x = 0; x < gridWidth; x++)
				{
					if (BlockGrid[x, y] != null && BlockGrid[x, y].CurrentBlockState != Block.BlockState.Falling)
							BlockGrid[x, y].GridLocation += new Point(0, 1);
				}
				
				y--;
			}

		}

		public override void Reset()
		{


			for (int y = 0; y < gridHeight; y++)
			{
				for (int x = 0; x < gridWidth; x++)
				{
					if (BlockGrid[x, y] != null)
						gameManager.RemoveBlockFromLocation(new Point(x, y));
				}
			}

			gameManager.CurrentGameState = GameManager.GameState.Playing;

			gameManager.MixUpColorsPerShape();


			AddToScore(-score, false);
			AddToLinesTotal(-lines);
			

			score = lines = 0;

			activeShape.Reset();

			SpawnNewShape();


		}
	}
}

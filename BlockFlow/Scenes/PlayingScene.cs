using BlockFlow.Entities;
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
using System.Runtime.CompilerServices;
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

		private List<int> yLocationsWithFullRows = new List<int>();

		private ShapeManager shapeManager;
		private Shape activeShape;
		private float timer = 1f;
		private int blockBlinkingCounter = 0;

		private float clearTimer = .25f;
		private float startClearTimer = .5f;
		private float startTimer;

		private int distanceToDrop = 0;

		private InputManager inputManager;

		private Block[] gridRowMemory = new Block[gridWidth];

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
			activeShape.SpawnNewBlock += SpawnNewShape;
			activeShape.SpeedDownEvent += SpeedDown;
			activeShape.HasHitLandingSpot += CheckRowsToShiftThenRespawn;

			shapeManager = new ShapeManager(AddEntity, BlockGrid, activeShape, Game);
			SpawnNewShape();
			

			AddEntity(activeShape, gridParentPosition);

			startTimer = timer;
			startClearTimer = clearTimer;
		}


		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

		
		//	if (yLocationsWithFullRows.Count == 0)
				timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			

			if (timer <= 0f)
			{
				activeShape.TryMoveShape(new Point(0, 1));

				//PrepareDownWardRowShift();
				
				timer = startTimer;
			}


			//if (yLocationsWithFullRows.Count > 0)
			//{
			//	clearTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

			//	if (clearTimer <= 0f)
			//	{
					
			//		for (int y = 0; y < yLocationsWithFullRows.Count; y++)
			//		{
			//			for (int x = 0; x < gridWidth; x++)
			//			{
			//				BlockGrid[x, yLocationsWithFullRows[y]].Visible = !BlockGrid[x, yLocationsWithFullRows[y]].Visible;
			//			}
						
			//		}
			//		blockBlinkingCounter++;

			//		if (blockBlinkingCounter < 6)
			//			clearTimer = startClearTimer;
			//		else
			//		{
			//			blockBlinkingCounter = 0;
			//			//SpawnNewShape();
			//			PrepareDownWardRowShift();
			//			yLocationsWithFullRows.Clear();

			//		}
			//	} 
			//}
		}

		//public void CheckRowMatches()
		//{
			

		//	for (int y = gridHeight - 1; y >= 0; y--)
		//	{



		//		if (RowCheck(y))
		//		{
		//			 yLocationsWithFullRows.Add(y);
		//		}


				
				

		//	}

			
		//}

		private bool RowCheck(int y)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				if (BlockGrid[x, y] == null)
					return false;
			}

			return true;
		}


		private void SpeedDown(object sender, SpeedDownEventArgs args)
		{
			if (args.SetToCutOffInitialTimer)
			{
				if (timer > args.Speed)
				{
					timer = args.Speed;
				}
				else
				{
					timer = 0;
					Debug.WriteLine("hit");
				}
					

				args.SetToCutOffInitialTimer = false;
			}

			
			startTimer = args.Speed;

		}



		private void SpawnNewShape()
		{
			shapeManager.GenerateShape(new Point(4, 0));
			//timer = startTimer;	
		}




		private void CheckRowsToShiftThenRespawn()
		{


			for (int y = gridHeight - 1; y >= 0; y--)
			{

				for (int x = 0; x < gridWidth; x++)
				{

					gridRowMemory[x] = BlockGrid[x, y];


				}

				//If every space in the array has a block in it........
				if (gridRowMemory.All(m => m != null && shapeManager.PositionHasBlock(m.GridLocation) && y > 0))
				{

					//Then clear the row with all blocks
					ClearFullRow(y);

					//And pull the blocks above that down
					MoveBlocksDown(y);

					//Recursively call this method until it reaches the base case that there are are no filled rows detected.  
					CheckRowsToShiftThenRespawn();
					return;
					

				}

				Array.Clear(gridRowMemory, 0, gridWidth);

				if (y == 0)
				{
					SpawnNewShape();
				}
			}

			
		}


		public void ClearFullRow(int y)
		{
			for (int x = 0; x < gridRowMemory.Length; x++)
			{

				if (BlockGrid[x, y] != null && BlockGrid[x, y].GridLocation == gridRowMemory[x].GridLocation)
				{
					shapeManager.RemoveBlockFromLocation(new Point(x, y));

				}

			}
		}

		public void MoveBlocksDown(int y)
		{
			y--;

			while (y >= 0)
			{
				for (int x = 0; x < gridWidth; x++)
				{
					if (BlockGrid[x, y] != null)
					{
						if (BlockGrid[x, y].IsLockedIn)
							BlockGrid[x, y].GridLocation += new Point(0, 1);

					}
				}

				y--;
			}

		}
	}
}

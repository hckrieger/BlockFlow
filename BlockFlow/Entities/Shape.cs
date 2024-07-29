using BlockFlow.Scenes;
using EC.CoreSystem;
using EC.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static BlockFlow.GameManager;

namespace BlockFlow.Entities
{
	internal class Shape : Entity
	{
        
        public Block[] Blocks { get; set; }
		private List<Block> orderedBlocks;
		public BlockShape BlockShape { get; set; }

		private GameManager gameManager;
		int rotationState = 0;

		private InputManager inputManager;

		private Point shapeLocation;



		public event Action HasHitLandingSpot;


		public event Action<int, bool> AddPointsFromDrop;

		public bool LandFromHardDrop { get; set; } = false;

        private float sideMoveTimer, heldTimer, startSideMoveTimer;

		private float moveDownTimer, speedDownTimer, startMoveDownTimer;

        public bool HaltDropTimer { get; set; }


        public Point ShapeLocation { 
			get => shapeLocation;
			set
			{
				shapeLocation = value;
				for (int i = 0; i < 4; i++)
				{
					Blocks[i].GridLocation = shapeLocation + gameManager.BlockPositions[BlockShape][rotationState, i];
				}

			}
		}
        private Point[] proposedPositionalChange;

        

        public Shape(Game game) : base(game)
        {


			inputManager = game.Services.GetService<InputManager>();

			Blocks = new Block[4];
			orderedBlocks = new List<Block>();



			proposedPositionalChange = new Point[4];

			startSideMoveTimer = .25f;
			heldTimer = .1f;

			sideMoveTimer = startSideMoveTimer;

		}

		public override void Initialize()
		{
			base.Initialize();

			Reset();
			
		}

		public void ChangeDropSpeedTimer(float decrement)
		{
			// Define the lowest allowed pixels per frame
			float lowestPixelsPerFrame = 10f / 60;

			// Decrease the startMoveDownTimer by the decrement
			startMoveDownTimer -= decrement;

			// Cap the timer to the lowest allowed pixels per frame
			if (startMoveDownTimer < lowestPixelsPerFrame)
			{
				startMoveDownTimer = lowestPixelsPerFrame;
			}

			Debug.WriteLine(startMoveDownTimer);
		}


		public void ReassignBlocksToShape(BlockShape blockShape, Point shapeLocation, Color color, GameManager gameManager)
		{
			BlockShape = blockShape;
			this.gameManager = gameManager;
			rotationState = 0;

			for (int i = 0; i < 4; i++)
			{
				var blockPosition = gameManager.BlockPositions[blockShape][rotationState, i];
				

				var entity = gameManager.GenerateBlock(color, this, shapeLocation, blockPosition);


					

				Blocks[i] = entity;

				if (gameManager.CurrentGameState != GameState.GameOver)
					Blocks[i].CurrentBlockState = Block.BlockState.Falling;
				else 
					Blocks[i].CurrentBlockState = Block.BlockState.LockedIn;
			}


			ShapeLocation = shapeLocation;
		}



		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);




			if (gameManager.CurrentGameState != GameState.Playing)
			{
				return;
			}

			if (inputManager.KeyJustPressed(Keys.Space) &&
				!Blocks.Any(m => m.CurrentBlockState == Block.BlockState.SetForRemoval))
			{
				DropWholeShape();

			}

			MoveToTheSides(Keys.Left, gameTime);
			MoveToTheSides(Keys.Right, gameTime);

			if (inputManager.KeyJustPressed(Keys.Down))
			{
				moveDownTimer = 0;
			}

			if (!HaltDropTimer)
				moveDownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			
		
				
			

			if (moveDownTimer <= 0f)
			{
				TryMoveShape(new Point(0, 1));


				if (inputManager.KeyHeld(Keys.Down) && !HaltDropTimer)
				{
					moveDownTimer = speedDownTimer;
					AddPointsFromDrop.Invoke(1, true);
				}
				else
					moveDownTimer = startMoveDownTimer;


			}




			if (inputManager.KeyJustPressed(Keys.Up))
			{
				TryRotateShape();
			}

		}



		public void TryMoveShape(Point direction)
		{
			
			var newPositions = CalculateNewBlockPositions(direction);

			if (AreNewPositionsValid(newPositions))
			{
				ShapeLocation += direction;
			} else if (IsOnLandingPosition(newPositions))
			{
				
				LockInBlocks();


				
			}
				
			
		}

		private void MoveToTheSides(Keys key, GameTime gameTime)
		{
			

			if (inputManager.KeyHeld(key))
			{
				var direction = key == Keys.Right ? new Point(1, 0) : new Point(-1, 0);
				if (inputManager.KeyJustPressed(key))
				{

					TryMoveShape(direction);
					sideMoveTimer = startSideMoveTimer;
				}


				sideMoveTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

				if (sideMoveTimer < 0)
				{
					TryMoveShape(direction);

					sideMoveTimer = heldTimer;
				}


			}
		}

	


		public void TryRotateShape()
		{
			if (BlockShape == BlockShape.O) return;

			rotationState = (rotationState + 1) % 4;

			var revertedShapeLocation = shapeLocation;

			AdjustIShapeLocationForRotation();

			var newPositions = CalculateNewBlockPositions(Point.Zero);

			if (AreNewPositionsValid(newPositions))
			{
				ApplyNewPositions(newPositions);
			}	
			else
			{
				rotationState = (rotationState - 1 + 4) % 4;
				if (shapeLocation != revertedShapeLocation)
					shapeLocation = revertedShapeLocation;
			}


		}

		private void AdjustIShapeLocationForRotation()
		{
			if (BlockShape != BlockShape.I) return;

			switch (rotationState)
			{
				case 1: shapeLocation += new Point(1, 0); break;
				case 2: shapeLocation += new Point(0, 1); break;
				case 3: shapeLocation += new Point(-1, 0); break;
				case 0: shapeLocation += new Point(0, -1); break;
			}
		}

		private Point[] CalculateNewBlockPositions(Point direction)
		{
			var newPositions = new Point[4];
			var newShapeLocation = shapeLocation + direction;
			for (int i = 0; i  <  4; i++)
			{
				
				newPositions[i] = newShapeLocation + gameManager.BlockPositions[BlockShape][rotationState, i];
			}


			return newPositions;
		}

		private bool AreNewPositionsValid(Point[] positions)
		{
			for (int i = 0; i < 4; i++)
			{
				if (!Blocks[i].IsValidPosition(positions[i])) return false;
			}
			return true;
		}

		private void ApplyNewPositions(Point[] positions)
		{
			for (int i = 0; i < 4; i++)
			{
				Blocks[i].GridLocation = positions[i];
			}
		}

		private bool IsOnLandingPosition(Point[] newPositions)
		{
			for (int i = 0; i < 4; i++)
			{
				 if (Blocks[i].IsOnLandingPosition(newPositions[i]))
					return true;
			}

			return false;
		}

		private void DropWholeShape()
		{
			int shortestDistance = 21; 
			foreach (var block in Blocks)
			{
				if (block.DistanceToLandingPosition() < shortestDistance)
					 shortestDistance = block.DistanceToLandingPosition();
			}
			LandFromHardDrop = true;
			ShapeLocation += new Point(0, shortestDistance);
			AddPointsFromDrop.Invoke(shortestDistance * 2, true);
			LockInBlocks();
		}

		private void LockInBlocks()
		{
			for (int i = 0; i  <  4; i++)
			{
				gameManager.RegisterBlockToLocation(Blocks[i].GridLocation, Blocks[i]);
			}
			
			HasHitLandingSpot.Invoke();
		}

		public override void Reset()
		{
			base.Reset();

			moveDownTimer = .8f;
			startMoveDownTimer = moveDownTimer;
			speedDownTimer = .1f;
		}



	}

	


}

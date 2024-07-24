using BlockFlow.Scenes;
using EC.CoreSystem;
using EC.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static BlockFlow.ShapeManager;

namespace BlockFlow.Entities
{
	internal class Shape : Entity
	{
        
        public Block[] Blocks { get; set; }
		private List<Block> orderedBlocks;
		public BlockShape BlockShape { get; set; }

		private ShapeManager shapeManager;
		int rotationState = 0;

		private InputManager inputManager;

		private Point shapeLocation;

		public event Action SpawnNewBlock;

		public event EventHandler<SpeedDownEventArgs> SpeedDownEvent;

		public event Action HasHitLandingSpot;

		private SpeedDownEventArgs speedDownEventArgs;

		private float timer, heldTimer, startTimer;

		bool notYetPressed = true;



        public Point ShapeLocation { 
			get => shapeLocation;
			set
			{
				shapeLocation = value;
				for (int i = 0; i < 4; i++)
				{
					Blocks[i].GridLocation = shapeLocation + shapeManager.BlockPositions[BlockShape][rotationState, i];
				}

			}
		}
        private Point[] proposedPositionalChange;

        

        public Shape(Game game) : base(game)
        {
			
			
			
			inputManager = game.Services.GetService<InputManager>();

			Blocks = new Block[4];
			orderedBlocks = new List<Block>();


			speedDownEventArgs = new SpeedDownEventArgs();

			proposedPositionalChange = new Point[4];

			startTimer = .25f;
			heldTimer = .1f;

			timer = startTimer;

		}


		public void ReassignBlocksToShape(BlockShape blockShape, Point shapeLocation, Color color, ShapeManager shapeManager)
		{
			BlockShape = blockShape;
			this.shapeManager = shapeManager;
			rotationState = 0;

			for (int i = 0; i < 4; i++)
			{
				var blockPosition = shapeManager.BlockPositions[blockShape][rotationState, i];
				var entity = shapeManager.GenerateBlock(color, this, shapeLocation, blockPosition);
				
				Blocks[i] = entity;
				Blocks[i].CurrentBlockState = Block.BlockState.Falling;
			}

			

			ShapeLocation = shapeLocation;
		}



		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (inputManager.KeyJustPressed(Keys.Up))
			{
				TryRotateShape();
			}



			// SpeedDownEvent.Invoke(this, new SpeedDownEventArgs { ButtonHeldToSpeedDown = inputManager.KeyHeld(Keys.Down) });

			if (inputManager.KeyHeld(Keys.Down))
			{
				if (inputManager.KeyJustPressed(Keys.Down))
					speedDownEventArgs.SetToCutOffInitialTimer = true;

				speedDownEventArgs.ButtonHeldToSpeedDown = true;
				SpeedDownEvent(this, speedDownEventArgs);
			} else if (inputManager.KeyUpCurrently(Keys.Down))
			{
				speedDownEventArgs.ButtonHeldToSpeedDown = false;
				SpeedDownEvent(this, speedDownEventArgs);
			}


			MoveToTheSides(Keys.Left, gameTime);
			MoveToTheSides(Keys.Right, gameTime);

			if (inputManager.KeyJustPressed(Keys.Space))
			{
				DropWholeShape();
				
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
					timer = startTimer;
				}


				timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

				if (timer < 0)
				{
					TryMoveShape(direction);

					timer = heldTimer;
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
				
				newPositions[i] = newShapeLocation + shapeManager.BlockPositions[BlockShape][rotationState, i];
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

			ShapeLocation += new Point(0, shortestDistance);

			LockInBlocks();
		}

		private void LockInBlocks()
		{
			for (int i = 0; i  <  4; i++)
			{
				shapeManager.RegisterBlockToLocation(Blocks[i].GridLocation, Blocks[i]);
			}
			
			HasHitLandingSpot.Invoke();
		}

	}

	


}

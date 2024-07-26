using BlockFlow.Scenes;
using EC.CoreSystem;
using EC.Utilities.Extensions;
using Microsoft.Xna.Framework;
using System.Linq.Expressions;
using System.Reflection.Metadata;

namespace BlockFlow.Entities
{
	internal class Block : Entity
	{
		private Point gridLocation, relativeLocation;
		private GameManager gameManager;
		private int distanceDown = 0;

        public BlockState CurrentBlockState { get; set; }

        public enum BlockState
		{
			LockedIn,
			SetForRemoval,
			Falling, 
			Removed
		}

		public Point GridLocation
		{
			get { return gridLocation; }
			set
			{
				if (gameManager.BlockGrid[gridLocation.X, gridLocation.Y] == this)
					gameManager.BlockGrid[gridLocation.X, gridLocation.Y] = null;
				gridLocation = value;
				
				Transform.LocalPosition = new Vector2(gridLocation.X * 32, gridLocation.Y * 32);
				gameManager.BlockGrid[gridLocation.X, gridLocation.Y] = this;
			}
		}

		public Point RelativeLocation
		{
			get { return relativeLocation; }
			set
			{
				gameManager.BlockGrid[gridLocation.X, gridLocation.Y] = null;
				relativeLocation = value;
				gridLocation += relativeLocation;
				Transform.LocalPosition = new Vector2(gridLocation.X * 32, gridLocation.Y * 32);
				gameManager.BlockGrid[gridLocation.X, gridLocation.Y] = this;
			 }
		}


		public Block(GameManager gameManager, Game game) : base(game)
		{
			this.gameManager = gameManager;
		}

		public bool IsValidPosition(Point position)
		{
			return gameManager.PositionIsOnGrid(position) && !gameManager.PositionHasBlock(position);
		}

		public bool IsOnLandingPosition(Point position)
		{
		

			var bottomOfGrid = position.Y == PlayingScene.gridHeight;


            var topOfBlock = gameManager.PositionHasBlock(position) && position.Y - (gridLocation.Y + distanceDown) == 1;
			return (bottomOfGrid || topOfBlock); 
		}


		public int DistanceToLandingPosition()
		{
			distanceDown = 0;

			var gridLocation = GridLocation;
			while (!IsOnLandingPosition(gridLocation += new Point(0, 1)))
			{
				distanceDown++;
			}

			int setDistanceDown = distanceDown;
			distanceDown = 0;

			return setDistanceDown;
		}
    }
}

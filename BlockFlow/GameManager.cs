using BlockFlow.Entities;
using BlockFlow.Scenes;
using EC.Components;
using EC.Components.Render;
using EC.CoreSystem;
using EC.Services.AssetManagers;
using EC.Utilities;
using EC.Utilities.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static BlockFlow.GameManager;
using static System.Reflection.Metadata.BlobBuilder;

namespace BlockFlow
{
    internal class GameManager
    {
        public enum BlockShape
        {
            I, O, T, S, Z, J, L
        }

		public enum GameState
		{
			Playing,
			GameOver,
			Pause
		}

		public GameState CurrentGameState;

        private GraphicsAssetManager graphicsAssetManager;


        private Game game;

		private BlockShape[] blockShapes; 

		public Block[,] BlockGrid { get; set; }

        public Dictionary<BlockShape, Point[,]> BlockPositions { get; }

		private List<Color> blockColors;

		public List<Block> BlockPool { get; set; }

		private int blockPoolQuantity = 200;
		private Shape activeShape;

		public event EventHandler<ShapeCheckArgs> CheckShapeToBeGenerated;

		ShapeCheckArgs shapeCheckArgs = new ShapeCheckArgs();

		public GameManager(Action<Entity> addEntity, Block[,] blockGrid, Shape activeShape, Game game)
        {
            this.game = game;
            graphicsAssetManager = game.Services.GetService<GraphicsAssetManager>();

			this.activeShape = activeShape;	
			BlockGrid = blockGrid;

			blockShapes = (BlockShape[])Enum.GetValues(typeof(BlockShape));
			

            BlockPositions = new Dictionary<BlockShape, Point[,]>
            {
                { BlockShape.S, new Point[,]
                    {
						{ new Point(-1, 0), new Point(0, 0), new Point(0, -1), new Point(1, -1) },
						{ new Point(0, -1), new Point(0, 0), new Point(1, 0), new Point(1, 1) },
						{ new Point(1, 0), new Point(0, 0), new Point(0, 1), new Point(-1, 1) },
						{ new Point(0, 1), new Point(0, 0), new Point(-1, 0), new Point(-1, -1) },
					}
                },
				{ BlockShape.J, new Point[,]
					{
						{ new Point(1, 0), new Point(0, 0), new Point(-1, 0), new Point(1, 1) },
						{ new Point(0, 1), new Point(0, 0), new Point(0, -1), new Point(-1, 1) },
						{ new Point(-1, 0), new Point(0, 0), new Point(1, 0), new Point(-1, -1) },
						{ new Point(0, -1), new Point(0, 0), new Point(0, 1), new Point(1, -1) },
					}
				},
				{ BlockShape.T, new Point[,]
					{
						{ new Point(-1, 0), new Point(0, 0), new Point(1, 0), new Point(0, 1) },
						{ new Point(0, -1), new Point(0, 0), new Point(0, 1), new Point(-1, 0) },
						{ new Point(1, 0), new Point(0, 0), new Point(-1, 0), new Point(0, -1) },
						{ new Point(0, 1), new Point(0, 0), new Point(0, -1), new Point(1, 0) },
					}
				},
				{ BlockShape.L, new Point[,]
					{
						{ new Point(-1, 0), new Point(0, 0), new Point(1, 0), new Point(-1, 1) },
						{ new Point(0, -1), new Point(0, 0), new Point(0, 1), new Point(-1, -1) },
						{ new Point(1, 0), new Point(0, 0), new Point(-1, 0), new Point(1, -1) },
						{ new Point(0, 1), new Point(0, 0), new Point(0, -1), new Point(1, 1) },
					}
				},
				{ BlockShape.Z, new Point[,]
					{
						{ new Point(1, 0), new Point(0, 0), new Point(0, -1), new Point(-1, -1) },
						{ new Point(0, 1), new Point(0, 0), new Point(1, 0), new Point(1, -1) },
						{ new Point(-1, 0), new Point(0, 0), new Point(0, 1), new Point(1, 1) },
						{ new Point(0, -1), new Point(0, 0), new Point(-1, 0), new Point(-1, 1) },
					}
				},
				{ BlockShape.O, new Point[,]
					{
						{ new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) }
					}
				},
				{ BlockShape.I, new Point[,]
					{
						{ new Point(-1, 0), new Point(0, 0), new Point(1, 0), new Point(2, 0) },
						{ new Point(0, -1), new Point(0, 0), new Point(0, 1), new Point(0, 2) },
						{ new Point(1, 0), new Point(0, 0), new Point(-1, 0), new Point(-2, 0) },
						{ new Point(0, 1), new Point(0, 0), new Point(0, -1), new Point(0, -2) },
					}
				}

			};

			blockColors = new List<Color>()
			{
				Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Purple, Color.Indigo
			};

			foreach (var color in blockColors)
			{
				graphicsAssetManager.LoadRectangle($"gridSquare {color}", 32, 32);
			}

			MixUpColorsPerShape();

			BlockPool = new List<Block>();

			for (int i = 0; i < blockPoolQuantity; i++)
			{
				var block = new Block(this, game);
				block.IsActive = false;
				var size = 32;
				//block.Transform.LocalPosition = new Vector2(relativePosition.X * size, relativePosition.Y * size);
				block.LoadRectangleComponents($"gridSquare{i}", size, size, Color.White, game);
				addEntity(block);
				BlockPool.Add(block);

			}

			


		}

		
		public void MixUpColorsPerShape()
		{
			blockColors.Shuffle();
			
		}
		
		public void GenerateShape(Point gridLocation)
		{
			var rng = MathUtils.RandomInt(blockShapes.Length);
			var shapeType = blockShapes[rng];
			var color = Color.White;

			switch (shapeType)
			{
				case BlockShape.I:
					color = blockColors[0];
					break;
				case BlockShape.J:
					color = blockColors[1];
					break;
				case BlockShape.O:
					color = blockColors[2];
					break;
				case BlockShape.S:
					color = blockColors[3];
					gridLocation += new Point(0, 1);
					break;
				case BlockShape.Z:
					color = blockColors[4];
					gridLocation += new Point(0, 1);
					break;
				case BlockShape.L:
					color = blockColors[5];
					break;
				case BlockShape.T:
					color = blockColors[6];
					break;
			}


			shapeCheckArgs.BlockShapeToSpawn = shapeType;
			shapeCheckArgs.SpawnLocation = gridLocation;
			

			CheckShapeToBeGenerated.Invoke(this, shapeCheckArgs);
			
			activeShape.ReassignBlocksToShape(shapeType, gridLocation, color, this);
		}


        public Block GenerateBlock(Color color, Shape parent, Point shapePosition, Point relativePosition)
        {
			int squareNumber;
			Block block = ActivateBlock(out squareNumber);
			

            block.Transform.Parent = parent.Transform;

			block.GridLocation = shapePosition;
            block.RelativeLocation = relativePosition;



			var renderer = block.GetComponent<RectangleRenderer>();
            renderer.LayerDepth = .9f;

            Color[] colorData = new Color[renderer.PixelQuantity];
            Texture2D texture2D = graphicsAssetManager.LoadRectangle($"gridSquare{squareNumber}", 32, 32);

            for (int l = 0; l < renderer.PixelQuantity; l++)
            {
                //Conditional detects which pixels in the texture on on the border
                if (l < renderer.TextureWidth ||
                    l % renderer.TextureWidth == 0 ||
                    l % renderer.TextureWidth == renderer.TextureWidth - 1 ||
                    l >= renderer.PixelQuantity - renderer.TextureWidth)
                {
                    colorData[l] = Color.Black;  //Assigns black to the border pixels
                }
                else
                {
                    colorData[l] = color; //Assigns the preferred color to the other pixels. 
                }
            }


            texture2D.SetData(colorData);

            return block;

        }

		public Block ActivateBlock(out int squareNumber)
		{
			Block assignedBlock = null;
			int assignedIndex = 0;
			for (int i = 0; i < BlockPool.Count; i++)
			{
				if (!BlockPool[i].IsActive)
				{
					BlockPool[i].IsActive = true;
					assignedBlock = BlockPool[i];
					assignedIndex = i;
					break;
				}
				
			}
			squareNumber = assignedIndex;
			return assignedBlock;
		}

		


		public bool PositionHasBlock(Point position)
		{
			if (position.X < 0 || position.Y < 0 || position.X > PlayingScene.gridWidth - 1 || position.Y > PlayingScene.gridHeight - 1)
				return false; 

			return BlockGrid[position.X, position.Y] is Block && 
				   (BlockGrid[position.X, position.Y].CurrentBlockState == Block.BlockState.LockedIn ||
				   BlockGrid[position.X, position.Y].CurrentBlockState == Block.BlockState.SetForRemoval);
		}

		public bool PositionIsOnGrid(Point position)
		{
			if (position.X > PlayingScene.gridWidth - 1 || position.X < 0 ||
				position.Y > PlayingScene.gridHeight - 1 || position.Y < 0)
				return false;

			return true;
		}

		public void RegisterBlockToLocation(Point location, Block block)
		{
			
			block.GridLocation = location;
			block.CurrentBlockState = Block.BlockState.LockedIn;
		}

		public void RemoveBlockFromLocation(Point location)
		{
			if (BlockGrid[location.X, location.Y] == null)
				return;
			
			BlockGrid[location.X, location.Y].CurrentBlockState = Block.BlockState.Removed;
			BlockGrid[location.X, location.Y].IsActive = false;
			BlockGrid[location.X, location.Y] = null;
		}


	}
}

using BlockFlow.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFlow
{
	internal class ShapeCheckArgs : EventArgs
	{

		public GameManager.BlockShape BlockShapeToSpawn { get; set; }

		public Point SpawnLocation { get; set; }	
	}
}

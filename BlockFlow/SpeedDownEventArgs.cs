using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockFlow
{
	internal class SpeedDownEventArgs : EventArgs
	{
        public bool ButtonHeldToSpeedDown { get; set; }

        public bool SetToCutOffInitialTimer;



        public float Speed
        {
            get
            {

                if (ButtonHeldToSpeedDown)
                {
                    
                    return .1f;
                }

                return 1f;

            }
        }
    }
}

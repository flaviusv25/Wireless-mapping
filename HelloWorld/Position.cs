using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIFIScan
{
    class Position
    {
        private double xPos = 0;
        private double yPos = 0;

        public double XPos
        {
            get
            {
                return xPos;
            }

            set
            {
                xPos = value;
            }
        }

        public double YPos
        {
            get
            {
                return yPos;
            }

            set
            {
                yPos = value;
            }
        }
    }
}

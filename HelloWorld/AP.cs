using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIFIScan
{
    class AP
    {
        private string networkName;
        private uint distance;
        private double processedSignal;
        private double xPosition;
        private double yPosition;


        public string NetworkName
        {
            get
            {
                return networkName;
            }

            set
            {
                networkName = value;
            }
        }

        public uint Distance
        {
            get
            {
                return distance;
            }

            set
            {
                distance = value;
            }
        }

        public double ProcessedSignal
        {
            get
            {
                return processedSignal;
            }

            set
            {
                processedSignal = value;
            }
        }

        public double XPosition
        {
            get
            {
                return xPosition;
            }

            set
            {
                xPosition = value;
            }
        }

        public double YPosition
        {
            get
            {
                return yPosition;
            }

            set
            {
                yPosition = value;
            }
        }
    }
}

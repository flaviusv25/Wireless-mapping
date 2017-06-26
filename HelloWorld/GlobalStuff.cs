using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.WiFi;

namespace WIFIScan
{
    public static class GlobalStuff
    {
        /// <summary>
        /// wifi setup variables
        /// </summary>
        /// 
        public static IReadOnlyList<WiFiAdapter> _wiFiAdapters; //list of all wifi adapters
        public static WiFiAccessStatus _wifiAccess;
        public static bool _wifiAccessState = false;
        public static WiFiAdapter _adapterWifi; //the adapter we use

        public static IReadOnlyList<WiFiAdapter> WiFiAdapters
        {
            get
            {
                return _wiFiAdapters;
            }
            set
            {
                _wiFiAdapters = value;
            }
        }

        public static WiFiAccessStatus WifiAccess
        {
            get
            {
                return _wifiAccess;
            }
            set
            {
                _wifiAccess = value;
            }
        }

        public static bool WifiAccessState
        {
            get
            {
                return _wifiAccessState;
            }
            set
            {
                _wifiAccessState = value;
            }
        }

        public static WiFiAdapter AdapterWifi
        {
            get
            {
                return _adapterWifi;
            }
            set
            {
                _adapterWifi = value;
            }
        }

    }
}

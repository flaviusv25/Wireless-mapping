using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.WiFi;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WIFIScan
{

    public sealed partial class Client : Page
    {

        private WiFiNetworkReport Report; //report containing the networks and the dbm values of the scan
        NameValueCollection scanVals = new NameValueCollection(); //collection holding the network names and the dbm values obtained from scans
        Dictionary<string, Dictionary<uint, double>> WifiMap; //dict of area maps - final map stored here
        Dictionary<string, Position> WifiPos; //dict of ap positions
        Dictionary<string, double> WifiClientScan; //Dictionary containg the mapping for one particular area
        Dictionary<string, Position> GridAreaNames;
        Dictionary<string, Dictionary<string, object>> GridMap;
        NameValueCollection AreaswithErrorvals = new NameValueCollection(); //nvc that stores the errors for each area
        List<KeyValuePair<string, double>> finalList = new List<KeyValuePair<string, double>>();

        private bool intParse = false;
        private const UInt16 sampleNumber = 20;
        private double[] sampleArray;
        private bool networkFound = false;
        /// <summary>
        /// What we use after each calibration
        /// </summary>
        private string networkName;
        private uint distance;
        private double processedSignal;
        private const uint samples = 20;

        public Client()
        {
            this.InitializeComponent();
            buttonScan.IsEnabled = false;
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {

            uint percentage = 0;
            listboxWifi.Items.Clear();
            if (GlobalStuff.WifiAccessState == false)
            {
                textblockMessage.Text = "acces denied!";
            }
            else
            {
                textblockMessage.Text = "Scanning..0%";
                for (uint cnt = 0; cnt < samples; cnt++)
                {
                    percentage = (cnt + 1) * 100 / samples;

                    await GlobalStuff.AdapterWifi.ScanAsync(); //scan
                    Report = GlobalStuff.AdapterWifi.NetworkReport;
                    foreach (var network in Report.AvailableNetworks)
                    {
                        // listBox1.Items.Add(network.Ssid + " " + network.NetworkRssiInDecibelMilliwatts + "dBm");
                        //scanVals.Add(network.Ssid, network.NetworkRssiInDecibelMilliwatts.ToString());
                        scanVals.Add(network.Ssid, network.NetworkRssiInDecibelMilliwatts.ToString());
                    }

                    textblockMessage.Text = "Scanning.." + percentage.ToString() + "%";
                }//end of scans

                //next take the mean of the values obtained for each network
                //and replace the stored values with only the mean
                //we use an aux list to store the values for each network and to use Average() fct.
                List<int> dbmValues; //aux list
                foreach (String key in scanVals.AllKeys)
                {
                    dbmValues = scanVals.GetValues(key).Select(int.Parse).ToList();
                    scanVals.Set(key, Math.Truncate(dbmValues.Average()).ToString()); //to be updated
                }

                WifiClientScan = NvcToDictionary(scanVals); //convert to dictionary
                scanVals.Clear();
                
                //sort
                var myList = WifiClientScan.ToList();
                double dist = -1.0;
                myList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));


                /// areas
                double difference = 0.0;
                double valueAux = 0.0;
                List<double> Diffs = new List<double>();
                uint consideredAPs = 4;
                double errSum = 0.0;
                foreach (var area in GridMap.Keys)
                {
                    Diffs.Clear();
                    errSum = 0.0;
                    consideredAPs = 4;

                    foreach (var ap in myList)
                    {
                        if (GridMap[area].ContainsKey(ap.Key))
                        {
                            difference = Math.Abs(Math.Abs(Convert.ToDouble(ap.Value)) - Math.Abs(Convert.ToDouble(GridMap[area][ap.Key])));
                            valueAux = Math.Abs(Convert.ToDouble(ap.Value));
                            //penalties
                            if ((valueAux > 50) && (valueAux <= 60))
                            {
                                difference *= 2;
                            }
                            else if ((valueAux > 60) && (valueAux <= 70))
                            {
                                difference *= 3;
                            }
                            else if ((valueAux > 70) && (valueAux <= 80))
                            {
                                difference *= 4;
                            }
                            else if (valueAux > 80)
                            {
                                difference *= 5;
                            }
                            Diffs.Add(difference);
                        }
                    }
                    Diffs.Sort((pair1, pair2) => pair2.CompareTo(pair1));
                    if (Diffs.Count == 0)
                    {
                        errSum = 1000; //penalty
                    }
                    else
                    {
                        if (Diffs.Count < consideredAPs)
                        {
                            consideredAPs = (uint)Diffs.Count;
                        }
                        for (int cnt = 0; cnt < consideredAPs; cnt++)
                        {
                            errSum += Diffs[cnt];
                        }
                        errSum = errSum / consideredAPs;
                        if (Diffs.Count < 3) //penalty
                        {
                            errSum = errSum * 2;
                        }
                    }
                    AreaswithErrorvals.Set(area, errSum.ToString());
                }
                var OrderedAreasByError = NvcToDictionary(AreaswithErrorvals);
                AreaswithErrorvals.Clear();
                OrderedAreasByError = OrderedAreasByError.ToDictionary(pair => pair.Key, pair => Convert.ToDouble(pair.Value));

                finalList = OrderedAreasByError.ToList();

                finalList.Sort((firstPair, nextPair) =>
                {
                    return firstPair.Value.CompareTo(nextPair.Value);
                }
                );
                listboxAreas.Items.Clear();
                foreach (KeyValuePair<string, double> netw in finalList)
                {
                    listboxAreas.Items.Add(netw.Key + " " + netw.Value);
                }

                ///distance
                var dictVal = new Dictionary<uint, double>();
                foreach (var network in myList)
                {
                    //listboxWifi.Items.Add(network.Key + " " + network.Value.ToString() + "dBm");
                    if(WifiMap.TryGetValue(network.Key, out dictVal))
                    {
                        var listDistAndSignal = dictVal.ToList();
                        listDistAndSignal.Sort((pair1, pair2) => pair1.Key.CompareTo(pair2.Key));
                        dist = Interpolate(network.Value, listDistAndSignal);
                        dist = Math.Round(dist, 2);
                        listboxWifi.Items.Add(network.Key + " " + network.Value.ToString() + "dBm " + dist.ToString() + "m xpos " + WifiPos[network.Key].XPos.ToString() + " ypos " + WifiPos[network.Key].YPos.ToString());
                    }
                }
            }
        }

        private void buttonMain_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //read Json file and load data
            if (GridMap == null)
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (File.Exists(storageFolder.Path + "\\Gridmap.txt"))
                {
                    Windows.Storage.StorageFile mapFile = await storageFolder.GetFileAsync("Gridmap.txt");
                    string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
                   // listBoxTest.Items.Clear();
                  //  listBoxTest.Items.Add(text);
                    //conver JSON back to dict collection
                    GridMap = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(text);
                }
                else
                {
                    //WifiMap = new Dictionary<string, Dictionary<uint, double>>();
                    textblockMessage.Text = "No gridmap file!";
                    buttonScan.IsEnabled = false;
                }
            }
            //read Json file and load data
            if (GridAreaNames == null)
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (File.Exists(storageFolder.Path + "\\GridAreas.txt"))
                {
                    Windows.Storage.StorageFile mapFile = await storageFolder.GetFileAsync("GridAreas.txt");
                    string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
                   //conver JSON back to dict collection
                    GridAreaNames = JsonConvert.DeserializeObject<Dictionary<string,Position>>(text);
                }
                else
                {
                    //WifiMap = new Dictionary<string, Dictionary<uint, double>>();
                    textblockMessage.Text += "No gridAreas file!";
                    buttonScan.IsEnabled = false;
                }
            }
            //read Json file and load data
            if (WifiMap == null)
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (File.Exists(storageFolder.Path + "\\map.txt"))
                {
                    Windows.Storage.StorageFile mapFile = await storageFolder.GetFileAsync("map.txt");
                    string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
                    listBoxTest.Items.Clear();
                    listBoxTest.Items.Add(text);
                    //conver JSON back to dict collection
                    WifiMap = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<uint, double>>>(text);
                }
                else
                {
                    //WifiMap = new Dictionary<string, Dictionary<uint, double>>();
                    textblockMessage.Text += " No map file!";
                    buttonScan.IsEnabled = false;
                }
            }

            //read Json for Wifipos and load data
            if (WifiPos == null)
            {
                Windows.Storage.StorageFolder storageFolder2 = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (File.Exists(storageFolder2.Path + "\\apPos.txt"))
                {
                    Windows.Storage.StorageFile mapFile2 = await storageFolder2.GetFileAsync("apPos.txt");
                    string text2 = await Windows.Storage.FileIO.ReadTextAsync(mapFile2); //read Json from file
                    listBoxJson.Items.Clear();
                    listBoxJson.Items.Add(text2);
                    //conver JSON back to dict collection
                    WifiPos = JsonConvert.DeserializeObject<Dictionary<string, Position>>(text2);
                    if(WifiMap != null)
                    {
                        buttonScan.IsEnabled = true;
                    }
                }
                else
                {
                    buttonScan.IsEnabled = false;
                    textblockMessage.Text += " No APpos file!";
                }
            }
            if((GridMap != null) &&
                (GridAreaNames != null) &&
                (WifiMap != null) &&
                (WifiPos != null))
            {
                textblockMessage.Text = "All files loaded";
            }
        }

        static Dictionary<string, double> NvcToDictionary(NameValueCollection nvc)
        {
            var result = new Dictionary<string, double>();
            foreach (string key in nvc.Keys)
            {
                result.Add(key, Double.Parse(nvc[key]));
            }

            return result;
        }
        private double Interpolate(double val, List<KeyValuePair<uint, double>> distAndSignal)
        {
            KeyValuePair<uint, double> first, last, prev, current;
            int index;

            if (distAndSignal.Count == 1)
            {
                return 100.0;
            }
           first = distAndSignal.First();
            if (val > first.Value)
            {
                return (double)first.Key;
            }
            last = distAndSignal.Last();
            if (val < last.Value)
            {
                index = distAndSignal.IndexOf(last);
                prev = distAndSignal[index - 1];
                current = last;
                return Math.Abs(((double)prev.Key - (double)current.Key) * (val - current.Value) / (prev.Value - current.Value) - (double)current.Key);
            }
            foreach(KeyValuePair<uint, double> pair in distAndSignal)
            {
                if(val == pair.Value)
                {
                    return (double)pair.Key;
                }
                if(val > pair.Value)
                {
                    index = distAndSignal.IndexOf(pair);
                    if (index - 1 > -1)
                    {
                        prev = distAndSignal[index - 1];
                        current = pair;
                    }
                    break;
                }
            }
            if (!prev.Equals(null))
            {
              return Math.Abs(((double)prev.Key - (double)current.Key) * (val - current.Value) / (prev.Value - current.Value) - (double)current.Key);
            }
            return 99.0;
        }

    }
}

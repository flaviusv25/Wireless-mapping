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


namespace WIFIScan
{
    public sealed partial class MainPage : Page
    {
        Dictionary<int, string> Areas = new Dictionary<int, string>(); //Dictionary for the area names (names are auto. generated)
        int areaNb = -1; // used to set the id for each scanned area
        private IReadOnlyList<WiFiAdapter> wiFiAdapters; //list of all wifi adapters
        WiFiAdapter adapter, adapter2; //the adapter we use
        WiFiNetworkReport report; //report containing the networks and the dbm values of the scan
        NameValueCollection scanVals = new NameValueCollection(); //collection holding the network names and the dbm values obtained from scans
        Dictionary<string, Dictionary<string, object>> WifiMap = new Dictionary<string, Dictionary<string, object>>(); //dict of area maps - final map stored here
        Dictionary<string, Dictionary<string, object>> WifiMapImported;//imported map from JSON conversion.
        Dictionary<string, object> AreaWifiMap; //Dictionary containg the mapping for one particular area
        Dictionary<string, double> ClientScan = new Dictionary<string, double>(); //results from client scan for networks
        List<KeyValuePair<string, double>> myList, finalList; //lists used to sort some values
        NameValueCollection AreaswithErrorvals = new NameValueCollection(); //nvc that stores the errors for each area
        Dictionary<string, double> AreasWithError = new Dictionary<string, double>(); //

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void BScan_Click(object sender, RoutedEventArgs e)
        {
            listBox1.Items.Clear();
            areaNb++;
            Areas.Add(areaNb, "area" + areaNb);
            // RequestAccessAsync must have been called at least once by the app before using the API
            // Calling it multiple times is fine but not necessary
            // RequestAccessAsync must be called from the UI thread
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                MesajFin.Text = "acces denied!";
            }
            else
            {
                MesajFin.Text = "acces allowed!";
                if (wiFiAdapters == null)
                {
                    wiFiAdapters = await WiFiAdapter.FindAllAdaptersAsync();
                }
                adapter = wiFiAdapters.First();

                //ten scans to build the map
                for (int cnt = 0; cnt < 10; cnt++)
                {
                    await adapter.ScanAsync(); //scan
                    report = adapter.NetworkReport;
                    foreach (var network in report.AvailableNetworks)
                    {
                        // listBox1.Items.Add(network.Ssid + " " + network.NetworkRssiInDecibelMilliwatts + "dBm");
                        scanVals.Add(network.Ssid, network.NetworkRssiInDecibelMilliwatts.ToString());
                    }

                }//end of ten scans

                //next take the mean of the values obtained for each network
                //and replace the stored values with only the mean
                //we use an aux list to store the values for each network and to use Average() fct.
                List<int> dbmValues; //aux list
                foreach (String key in scanVals.AllKeys)
                {
                    dbmValues = scanVals.GetValues(key).Select(int.Parse).ToList();
                    scanVals.Set(key, Math.Truncate(dbmValues.Average()).ToString());
                    //  listBox1.Items.Add(key + " " + scanVals[key]);
                }

                AreaWifiMap = NvcToDictionary(scanVals); //convert to dictionary
                scanVals.Clear();

                WifiMap.Add(Areas[areaNb], AreaWifiMap); //add new entry for each scan *a dictionary of dicts
                foreach (KeyValuePair<string, Dictionary<string, object>> kvp in WifiMap)
                {
                    listBox1.Items.Add(kvp.Key + " >>>>> ");
                    foreach (KeyValuePair<string, object> kvp2 in kvp.Value)
                    {
                        listBox1.Items.Add(kvp2.Key + " " + kvp2.Value);
                    }
                }
                listBox1.Items.Add("::::::::::::::::::::::::::::::::::::::::");
                //  MesajFin.Text = String.Format("DSDSdss");
            }
        }
        //store it in a dict of dicts, put it in a file, import it into the client soft, make a scan, arange scan results by power
        // look for first 3 in each areas and compute the mean error, if not found in an area, put error max, if error max for all areas,
        // drop that specific network and take the next network in list till, there is at least one area with error different thant max or
        // we arrive at the end of the list in which case if no network is found send signal out of area
        static Dictionary<string, object> NvcToDictionary(NameValueCollection nvc)
        {
            var result = new Dictionary<string, object>();
            foreach (string key in nvc.Keys)
            {
                result.Add(key, nvc[key]);
            }

            return result;
        }

        private async void JsonButton_Click(object sender, RoutedEventArgs e)
        {
            string output = JsonConvert.SerializeObject(WifiMap, Formatting.Indented);
            // Get the app's local folder.
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile mapFile = await storageFolder.CreateFileAsync("map.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(mapFile, output); //write JSON to file
            string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
            listBox1.Items.Clear();
            listBox1.Items.Add(text);
            WifiMapImported = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(text); //conver JSON back to dict collection
        }


        private async void readJsonBtn_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            if (File.Exists(storageFolder.Path + "\\map.txt"))
            {
                Windows.Storage.StorageFile mapFile = await storageFolder.GetFileAsync("map.txt");
                string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
                listBox1.Items.Clear();
                listBox1.Items.Add(text);
                //conver JSON back to dict collection
                WifiMapImported = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(text);
            }
            else
            {
                MesajFin.Text = "map file does not exist!";
            }
        }

        private void ResultButton_Click(object sender, RoutedEventArgs e)
        {
            listBox1.Items.Clear();
            foreach (KeyValuePair<string, Dictionary<string, object>> kvp in WifiMapImported)
            {
                listBox1.Items.Add(kvp.Key + " >>>>> ");
                foreach (KeyValuePair<string, object> kvp2 in kvp.Value)
                {
                    listBox1.Items.Add(kvp2.Key + "><><" + kvp2.Value);
                }
            }
        }

        private async void ClientBtn_Click(object sender, RoutedEventArgs e)
        {
            var access2 = await WiFiAdapter.RequestAccessAsync();
            if (access2 != WiFiAccessStatus.Allowed)
            {
                MesajFin.Text = "acces denied!";
            }
            else
            {
                MesajFin.Text = "acces allowed!";
                if (wiFiAdapters == null)
                {
                    wiFiAdapters = await WiFiAdapter.FindAllAdaptersAsync();
                }
                adapter2 = wiFiAdapters.First();
                await adapter2.ScanAsync(); //scan
                report = adapter2.NetworkReport;
                scanVals.Clear();
                foreach (var network in report.AvailableNetworks)
                {
                    //listBox1.Items.Add(network.Ssid + " " + network.NetworkRssiInDecibelMilliwatts + "dBm");
                    scanVals.Add(network.Ssid, network.NetworkRssiInDecibelMilliwatts.ToString());
                }
                List<int> dbmValues;
                foreach (String key in scanVals.AllKeys)
                {
                    dbmValues = scanVals.GetValues(key).Select(int.Parse).ToList();
                    scanVals.Set(key, Math.Truncate(dbmValues.Average()).ToString()); //take the mean of the values for each network
                                                                                      //  listBox1.Items.Add(key + " " + scanVals[key]);
                }
                var rez = NvcToDictionary(scanVals);
                ClientScan = rez.ToDictionary(pair => pair.Key, pair => Convert.ToDouble(pair.Value));
                scanVals.Clear();
                myList = ClientScan.ToList();

                myList.Sort((firstPair, nextPair) =>
                {
                    return nextPair.Value.CompareTo(firstPair.Value);
                }
                );
                listBox1.Items.Clear();
                foreach (KeyValuePair<string, double> netw in myList)
                {
                    listBox1.Items.Add(netw.Key + " " + netw.Value);
                }// look first three networks or if nb < 3 put that nb.If zero network.. -> messsage. if no area - > error
                //if key(SSID) in area - compute error and save it to an nvc for area 1, look in next area, do the same, iterate for next
                //network found and so on. If a network is not found in an area , add maxerror to nvc. if no area contains a network raise flag and take
                //another network. in de end, do a mean for all nvc entries, convert nvc to dictionary then to dict double and select the one with smallest
                //error
            }
        }
        private void ComputeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (myList == null || myList.Count() == 0)
            {
                MesajFin.Text = "No networks found!";
            }
            else
            {
                int NbofNetworks = 3; //how many networks we are considering
                if (myList.Count() < NbofNetworks)
                {
                    NbofNetworks = myList.Count();
                }
                double rez;
                int NbOfNetworkNotFound = 0;

                for (int inc = 0; inc < NbofNetworks; inc++)
                {
                    var network = myList[inc];
                    foreach (KeyValuePair<string, Dictionary<string, object>> kvp in WifiMapImported)
                    {
                        if (kvp.Value.ContainsKey(network.Key)) //if network in area
                        {
                            rez = Math.Abs(Math.Abs(Convert.ToDouble(network.Value)) - Math.Abs(Convert.ToDouble(kvp.Value[network.Key])));
                            AreaswithErrorvals.Set(kvp.Key, rez.ToString());
                        }
                        else //if network is not found in area
                        {
                            rez = 60; //max error
                            NbOfNetworkNotFound++;
                            AreaswithErrorvals.Set(kvp.Key, rez.ToString());
                        }
                        //  foreach (KeyValuePair<string, object> kvp2 in kvp.Value)
                        //   {
                        //   listBox1.Items.Add(kvp2.Key + "><><" + kvp2.Value);

                        //   }
                    }
                    if (NbOfNetworkNotFound == WifiMapImported.Count() && NbofNetworks < myList.Count() - 1)
                    {
                        NbofNetworks++;
                    }
                    NbOfNetworkNotFound = 0;
                }
                List<int> errors;
                foreach (String area in AreaswithErrorvals.AllKeys)
                {
                    errors = AreaswithErrorvals.GetValues(area).Select(int.Parse).ToList();
                    AreaswithErrorvals.Set(area, Math.Truncate(errors.Average()).ToString()); //take the mean of the values for each network
                }
                var OrderedAreasByError = NvcToDictionary(AreaswithErrorvals);
                AreaswithErrorvals.Clear();
                AreasWithError = OrderedAreasByError.ToDictionary(pair => pair.Key, pair => Convert.ToDouble(pair.Value));

                finalList = AreasWithError.ToList();

                finalList.Sort((firstPair, nextPair) =>
                {
                    return firstPair.Value.CompareTo(nextPair.Value);
                }
                );
                listBox1.Items.Clear();
                foreach (KeyValuePair<string, double> netw in finalList)
                {
                    listBox1.Items.Add(netw.Key + " " + netw.Value);
                }
            }
        }//end function


    }//end class
}//end namespace

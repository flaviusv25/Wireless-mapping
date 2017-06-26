using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Windows.Devices.WiFi;
using System.Collections.Specialized;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WIFIScan
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GridSetup : Page
    {
        Dictionary<string, Position> GridAreaNames;

        WiFiNetworkReport report; //report containing the networks and the dbm values of the scan
        NameValueCollection scanVals = new NameValueCollection(); //collection holding the network names and the dbm values obtained from scans
        Dictionary<string, object> AreaWifiMap; //Dictionary containg the mapping for one particular area
        Dictionary<string, Dictionary<string, object>> GridMap; //dict of area maps - final map stored here
        private const UInt16 sampleNumber = 20;

        private string areaName;



        public GridSetup()
        {
            this.InitializeComponent();
            BScan.IsEnabled = false;
            JsonButton.IsEnabled = false;
        }

        private void buttonClientPage_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void ButtonGoToSetup_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GridAreas));
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //read Json file and load data
            if (GridAreaNames == null)
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (File.Exists(storageFolder.Path + "\\GridAreas.txt"))
                {
                    Windows.Storage.StorageFile GridmapFile = await storageFolder.GetFileAsync("GridAreas.txt");
                    string text = await Windows.Storage.FileIO.ReadTextAsync(GridmapFile); //read Json from file

                    listBox1.Items.Clear();
                    //conver JSON back to dict collection
                    GridAreaNames = JsonConvert.DeserializeObject<Dictionary<string,Position>>(text);
                    foreach(var area in GridAreaNames.Keys)
                    {
                        listBox1.Items.Add(area);
                    }
                }
                else
                {
                    MesajFin.Text = "No  Grid areas file!";
                    BScan.IsEnabled = false;
                }
            }
            //read Json file for dbm values and load data
            if (GridMap == null)
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (File.Exists(storageFolder.Path + "\\GridMap.txt"))
                {
                    Windows.Storage.StorageFile GridmapFile = await storageFolder.GetFileAsync("GridMap.txt");
                    string text = await Windows.Storage.FileIO.ReadTextAsync(GridmapFile); //read Json from file
                    //conver JSON back to dict collection
                    GridMap = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(text);
                }
                else
                {
                    GridMap = new Dictionary<string, Dictionary<string, object>>();
                    // MesajFin.Text = "No  Grid areas file!";
                    // BScan.IsEnabled = false;
                }
            }
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                areaName = listBox1.SelectedItem.ToString();
                BScan.IsEnabled = true;
            }
            else
            {
                BScan.IsEnabled = false;
            }
        }

        private async void BScan_Click(object sender, RoutedEventArgs e)
        {

            uint percentage = 0;
            listBox1.IsEnabled = false;
            if (GlobalStuff.WifiAccessState == false)
            {
                MesajFin.Text = "acces denied!";
            }
            else
            {
                MesajFin.Text = "acces allowed!";

                //ten scans to build the map
                for (uint cnt = 0; cnt < sampleNumber; cnt++)
                {
                    percentage = (cnt + 1) * 100 / sampleNumber;
                    await GlobalStuff.AdapterWifi.ScanAsync(); //scan
                    report = GlobalStuff.AdapterWifi.NetworkReport;
                    foreach (var network in report.AvailableNetworks)
                    {
                        scanVals.Add(network.Ssid, network.NetworkRssiInDecibelMilliwatts.ToString());
                    }
                    MesajFin.Text = "Scanning.." + percentage.ToString() + "%";

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
                if(GridMap.ContainsKey(areaName))
                {
                    GridMap[areaName] = AreaWifiMap;
                }
                else
                {
                    GridMap.Add(areaName, AreaWifiMap); //add new entry
                }
                JsonButton.IsEnabled = true;
                listBox1.IsEnabled = true;
                MesajFin.Text = "";
                //  foreach (KeyValuePair<string, Dictionary<string, object>> kvp in WifiMap)
                //  {
                //      listBox1.Items.Add(kvp.Key + " >>>>> ");
                //     foreach (KeyValuePair<string, object> kvp2 in kvp.Value)
                //     {
                //         listBox1.Items.Add(kvp2.Key + " " + kvp2.Value);
                //     }
                // }
                // listBox1.Items.Add("::::::::::::::::::::::::::::::::::::::::");
                //  MesajFin.Text = String.Format("DSDSdss");
            }
        }

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
            JsonButton.IsEnabled = false;
            string output = JsonConvert.SerializeObject(GridMap, Formatting.Indented);
            // Get the app's local folder.
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile mapFile = await storageFolder.CreateFileAsync("Gridmap.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(mapFile, output); //write JSON to file
            string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
            GridMap = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(text); //conver JSON back to dict collection
        }
    }
}

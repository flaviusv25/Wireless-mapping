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
    public sealed partial class Setup : Page
    {
        private WiFiNetworkReport Report; //report containing the networks and the dbm values of the scan
        Dictionary<string, Dictionary<uint, double>> WifiMap; //dict of area maps - final map stored here
        Dictionary<string, Position> WifiPos; //dict of ap positions


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

        public Setup()
        {
            this.InitializeComponent();
            textBoxDistance.IsEnabled = false;
            buttonCalibrate.IsEnabled = false;
            sampleArray = new double[sampleNumber];
            textboxMessage.IsReadOnly = true;
            textboxTitle.IsReadOnly = true;
            buttonToJSON.IsEnabled = false;
            listboxWifi.IsEnabled = false;
        }

        private void buttonMain_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.GotFocus -= TextBox_GotFocus;
        }

        private void listboxWifi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listboxWifi.SelectedItem != null)
            {
                networkName = listboxWifi.SelectedItem.ToString();
                if(WifiPos.ContainsKey(networkName))
                {
                    textBoxDistance.IsEnabled = true;
                    textboxMessage.Text = "";
                    textBlockXposVal.Text = WifiPos[networkName].XPos.ToString();
                    textBlockYposVal.Text = WifiPos[networkName].YPos.ToString();
                }
                else
                {
                    textboxMessage.Text = "No defined XYPos!";
                    textBoxDistance.IsEnabled = false;
                    buttonCalibrate.IsEnabled = false;

                    textBlockXposVal.Text = "-";
                    textBlockYposVal.Text = "-";
                }

            }
            else
            {
                textBoxDistance.IsEnabled = false;
                buttonCalibrate.IsEnabled = false;

                textBoxDistance.Text = "Enter distance";
                textBoxDistance.GotFocus += TextBox_GotFocus;
                textBlockXposVal.Text = "-";
                textBlockYposVal.Text = "-";
            }
        }

        private void textBoxDistance_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if (uint.TryParse(textBoxDistance.Text, out distance))
            {
                textBoxDistance.Text = distance.ToString();
                intParse = true;
            }
            else
            {
                intParse = false;
                if (textBoxDistance.IsEnabled == true)
                {
                    textBoxDistance.Text = "";
                }
            }
        }

        private void textBoxDistance_TextChanged(object sender, TextChangedEventArgs e)
        {
            textboxMessage.Text = "";
            if (intParse == true)
            {
                buttonCalibrate.IsEnabled = true;
            }
            else
            {
                buttonCalibrate.IsEnabled = false;
            }
        }

        private void buttonCalibrate_Click(object sender, RoutedEventArgs e)
        {
            buttonScan.IsEnabled = false;
            listboxWifi.IsEnabled = false;
            textBoxDistance.IsEnabled = false;
            buttonCalibrate.IsEnabled = false;

            getRssiValues();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            listboxWifi.Items.Clear();
            if (GlobalStuff.WifiAccessState == false)
            {
                textboxMessage.Text = "acces denied!";
            }
            else
            {
                textboxMessage.Text = "Scanning..";

                await GlobalStuff.AdapterWifi.ScanAsync(); //scan
                Report = GlobalStuff.AdapterWifi.NetworkReport;

                foreach (var network in Report.AvailableNetworks)
                {
                    listboxWifi.Items.Add(network.Ssid);//, network.NetworkRssiInDecibelMilliwatts.ToString());
                }

                //read Json file and load data
                if(WifiMap == null)
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
                        WifiMap = new Dictionary<string, Dictionary<uint, double>>();
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
                    }
                    else
                    {
                        WifiPos = new Dictionary<string, Position>();
                    }
                }
                listboxWifi.IsEnabled = true;
                textboxMessage.Text = "";
            }
        }

        private async void getRssiValues()
        {
            uint percentage = 0;
            textboxMessage.Text = "Scanning..0%";

            for (uint i = 0; i < sampleNumber; i++)
            {
                percentage = (i+1) * 100 / sampleNumber;

                await GlobalStuff.AdapterWifi.ScanAsync(); //scan
                Report = GlobalStuff.AdapterWifi.NetworkReport;
                networkFound = false;
                foreach (var network in Report.AvailableNetworks)
                {
                    if(network.Ssid.Equals(networkName))
                    {
                        networkFound = true;
                        sampleArray[i] = network.NetworkRssiInDecibelMilliwatts;
                        break;
                    }
                }
                if(networkFound == false)
                {
                    textboxMessage.Text = "Network not found";
                    break;
                }

                textboxMessage.Text = "Scanning.." + percentage.ToString() +"%";
            }
            if(networkFound == true)
            {
                textboxMessage.Text = "Done";
                processDbmSamples();

                if(WifiMap.ContainsKey(networkName))
                {
                    if(WifiMap[networkName].ContainsKey(distance))
                    {
                        WifiMap[networkName][distance] = processedSignal; //value was updated
                    }
                    else
                    {
                        WifiMap[networkName].Add(distance, processedSignal); // new distance - signalpower pair
                    }
                }
                else
                {
                    WifiMap[networkName] = new Dictionary<uint, double>();
                    WifiMap[networkName].Add(distance, processedSignal); //Ap added with first measurement
                }


                buttonToJSON.IsEnabled = true;

            }

            buttonScan.IsEnabled = true;
            listboxWifi.IsEnabled = true;
            textBoxDistance.IsEnabled = true;
            buttonCalibrate.IsEnabled = true;

        }//end getRssiValues()

        private void processDbmSamples()
        {
            double sum = 0;
            for (uint i = 0; i < sampleNumber; i++)
            {
                sum += sampleArray[i];
            }
            processedSignal = sum / sampleNumber;
        }

        private async void buttonToJSON_Click(object sender, RoutedEventArgs e)
        {

            buttonToJSON.IsEnabled = false;

            string output = JsonConvert.SerializeObject(WifiMap, Formatting.Indented);
            // Get the app's local folder.
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile mapFile = await storageFolder.CreateFileAsync("map.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(mapFile, output); //write JSON to file
            string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
            listBoxTest.Items.Clear();
            listBoxTest.Items.Add(text);
            WifiMap = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<uint, double>>>(text); //conver JSON back to dict collection
        }

        private void buttonApPos_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ApPosXY));
        }
    }
}

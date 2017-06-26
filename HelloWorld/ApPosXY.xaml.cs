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
    public sealed partial class ApPosXY : Page
    {
        private WiFiNetworkReport Report; //report containing the networks and the dbm values of the scan
        Dictionary<string, Position> WifiPos; //dict of ap positions
        
        private string networkName;
        private double xPos;
        private double yPos;
        private bool xPosParse = false;
        private bool yPosParse = false;

        public ApPosXY()
        {
            this.InitializeComponent();
            textBoxXPOS.IsEnabled = false;
            textBoxYPOS.IsEnabled = false;
            buttonToJSON.IsEnabled = false;
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            listboxWifi.Items.Clear();
            if (GlobalStuff.WifiAccessState == false)
            {
                textblockMessage.Text = "acces denied!";
            }
            else
            {
                textblockMessage.Text = "Scanning..";

                await GlobalStuff.AdapterWifi.ScanAsync(); //scan
                Report = GlobalStuff.AdapterWifi.NetworkReport;

                foreach (var network in Report.AvailableNetworks)
                {
                    listboxWifi.Items.Add(network.Ssid);//, network.NetworkRssiInDecibelMilliwatts.ToString());
                }

                //read Json file and load data
                if (WifiPos == null)
                {
                    Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    if (File.Exists(storageFolder.Path + "\\apPos.txt"))
                    {
                        Windows.Storage.StorageFile mapFile = await storageFolder.GetFileAsync("apPos.txt");
                        string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
                        listBoxJson.Items.Clear();
                        listBoxJson.Items.Add(text);
                        //conver JSON back to dict collection
                        WifiPos = JsonConvert.DeserializeObject<Dictionary<string, Position>>(text);
                    }
                    else
                    {
                        WifiPos = new Dictionary<string, Position>();
                    }
                }

                textblockMessage.Text = "";
            }
        }

        private void buttonSetup_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Setup));
        }

        private void listboxWifi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listboxWifi.SelectedItem != null)
            {
                networkName = listboxWifi.SelectedItem.ToString();
                textBoxXPOS.IsEnabled = true;
                textBoxYPOS.IsEnabled = true;
                textblockMessage.Text = "";
                processJsonData();
            }
            else
            {
                textBoxXPOS.IsEnabled = false;
                textBoxYPOS.IsEnabled = false;

                textBoxXPOS.Text = "";
                textBoxYPOS.Text = "";
                // textBoxDistance.GotFocus += TextBox_GotFocus;
            }
        }

        private void textBoxXPOS_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if ((double.TryParse(textBoxXPOS.Text, out xPos)) &&
                (xPos >= 0.0))
            {
                textBoxXPOS.Text = xPos.ToString();
                xPosParse = true;
            }
            else
            {
                xPosParse = false;
                if (textBoxXPOS.IsEnabled == true)
                {
                    textBoxXPOS.Text = "";
                }
            }
        }

        private void textBoxXPOS_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((xPosParse == true) &&
                (yPosParse == true))
            {
                buttonToJSON.IsEnabled = true;
            }
            else
            {
                buttonToJSON.IsEnabled = false;
            }
        }

        private void textBoxYPOS_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if ((double.TryParse(textBoxYPOS.Text, out yPos)) &&
                (yPos >= 0.0))
            {
                textBoxYPOS.Text = yPos.ToString();
                yPosParse = true;
            }
            else
            {
                yPosParse = false;
                if (textBoxYPOS.IsEnabled == true)
                {
                    textBoxYPOS.Text = "";
                }
            }

        }

        private void textBoxYPOS_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((xPosParse == true) &&
                (yPosParse == true))
            {
                buttonToJSON.IsEnabled = true;
            }
            else
            {
                buttonToJSON.IsEnabled = false;
            }

        }

        private async void buttonToJSON_Click(object sender, RoutedEventArgs e)
        {
            buttonToJSON.IsEnabled = false;

            if (WifiPos.ContainsKey(networkName))
            {
                WifiPos[networkName].XPos = xPos;
                WifiPos[networkName].YPos = yPos;
            }
            else
            {
                WifiPos[networkName] = new Position();
                WifiPos[networkName].XPos = xPos;
                WifiPos[networkName].YPos = yPos;
            }
           await toJson();
        }


        private void processJsonData()
        {
            if(WifiPos.ContainsKey(networkName))
            {
                textBoxXPOS.Text = WifiPos[networkName].XPos.ToString();
                textBoxYPOS.Text = WifiPos[networkName].YPos.ToString();
            }
            else
            {
                textBoxXPOS.Text = "";
                textBoxYPOS.Text = "";
            }
        }

        private async Task toJson()
        {
            string output = JsonConvert.SerializeObject(WifiPos, Formatting.Indented);
            // Get the app's local folder.
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile mapFile = await storageFolder.CreateFileAsync("apPos.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(mapFile, output); //write JSON to file
            string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
            listBoxJson.Items.Clear();
            listBoxJson.Items.Add(text);
            WifiPos = JsonConvert.DeserializeObject<Dictionary<string, Position>>(text); //conver JSON back to dict collection
        }

    }
}

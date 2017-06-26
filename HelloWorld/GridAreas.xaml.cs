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
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WIFIScan
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GridAreas : Page
    {
        Dictionary<string, Position> GridAreaNames;

        private double xPos;
        private double yPos;
        private string areaName;
        private bool xPosParse = false;
        private bool yPosParse = false;
        public GridAreas()
        {
            this.InitializeComponent();
            textBoxXPOS.IsEnabled = false;
            textBoxYPOS.IsEnabled = false;
            buttonToJSON.IsEnabled = false;
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
                    //conver JSON back to dict collection
                    GridAreaNames = JsonConvert.DeserializeObject<Dictionary<string, Position>>(text); //conver JSON back to dict collection
                    listBoxJson.Items.Clear();
                    listBoxJson.Items.Add(text);
                }
                else
                {
                    GridAreaNames = new Dictionary<string, Position>();
                    listBoxJson.Items.Clear();
                    //  MesajFin.Text = "No  Grid map file!";
                    //  BScan.IsEnabled = false;
                }
            }
        }

        private void textBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(textBoxName.Text))
            {
                textBoxXPOS.IsEnabled = false;
                textBoxYPOS.IsEnabled = false;
            }
            else
            {
                textBoxXPOS.IsEnabled = true;
                textBoxYPOS.IsEnabled = true;
                areaName = textBoxName.Text;
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

            if (GridAreaNames.ContainsKey(areaName))
            {
                GridAreaNames[areaName].XPos = xPos;
                GridAreaNames[areaName].YPos = yPos;
            }
            else
            {
                GridAreaNames[areaName] = new Position();
                GridAreaNames[areaName].XPos = xPos;
                GridAreaNames[areaName].YPos = yPos;
            }
            await toJson();
        }

        private async Task toJson()
        {
            string output = JsonConvert.SerializeObject(GridAreaNames, Formatting.Indented);
            // Get the app's local folder.
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile mapFile = await storageFolder.CreateFileAsync("GridAreas.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(mapFile, output); //write JSON to file
            string text = await Windows.Storage.FileIO.ReadTextAsync(mapFile); //read Json from file
            listBoxJson.Items.Clear();
            listBoxJson.Items.Add(text);
            GridAreaNames = JsonConvert.DeserializeObject<Dictionary<string, Position>>(text); //conver JSON back to dict collection
        }

        private void buttonSetup_Click(object sender, RoutedEventArgs e)
        {

            this.Frame.Navigate(typeof(GridSetup));
        }
    }
}

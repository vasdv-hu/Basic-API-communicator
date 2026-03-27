using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace APIBullshit
{
    public class ConfigFile(string URL)
    {
        public string URL { get; } = URL;
    }
    public class Cat(string id, List<string> tags, DateTime created_at, string url, string mimetype)
    {
        public string id { get; } = id;
        public List<string> tags { get; } = tags;
        public DateTime created_at { get; } = created_at;
        public string url { get; } = url;
        public string mimetype { get; } = mimetype;
    }
    public partial class MainWindow : Window
    {

        private string LogPath = String.Empty;
        private void ValidateDirectory(string directoryPath){
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }
        private string APIURL()
        {
            try
            {
                string rawJSON = File.ReadAllText("config.json");
                ConfigFile? config = JsonSerializer.Deserialize<ConfigFile>(rawJSON);

                return config.URL;
            }
            catch (Exception e){
                WriteToLog(e);

                ConfigFile configDefault = new ConfigFile(URL:"https://cataas.com/");
                string rawJSON = JsonSerializer.Serialize<ConfigFile>(configDefault);

                File.WriteAllText("config.json", rawJSON);

                return configDefault.URL;
            }
        }
        private static HttpClient jsonClient = new HttpClient();
        private static HttpClient imgClient = new HttpClient();
        string timeNow()
        {
            return DateTime.Now.ToString("yyyy-mm-dd-HH-mm-ss"); 
        }
        private Cat? currentCat;

        private void CreateLogFile()
        {
            ValidateDirectory("logs");

            LogPath = $"logs/{timeNow()}.txt";
            File.WriteAllText(LogPath, $"{timeNow()} : App started\n");
        }
        private void WriteToLog(Exception? error = null, string? extraData = "")
        {
            bool errorExists = error != null;
            bool extraExists = extraData != string.Empty;

            if (!errorExists && !extraExists)
                return;

            string logText = (error != null) ? $"$\"ERROR|{error.GetType()}|{error.Message}\"" : "INFO|";

            File.AppendAllText(LogPath, $"{timeNow()}|{logText}|{extraData}\n");
        }

        private void CacheKitty(Cat cacheableCat)
        {
            ValidateDirectory("cache");

            string rawJSON = JsonSerializer.Serialize<Cat>(cacheableCat);
            File.WriteAllText($"cache/{cacheableCat.id}.json", rawJSON);
        }


        public MainWindow()
        {
            try
            {
                CreateLogFile();
                InitializeComponent();

                Uri addressUri = new Uri(APIURL());
                jsonClient.BaseAddress = addressUri;
                imgClient.BaseAddress = addressUri;
                jsonClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                imgClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
            }
            catch(Exception e)
            {
                WriteToLog(error:e);
            }
        }
        private async Task GetAPIResponse()
        {
            using HttpResponseMessage response = await jsonClient.GetAsync("cat?json=true");
            var jsonResponse = await response.Content.ReadAsStringAsync();
            WriteToLog(extraData: $"response: {jsonResponse}");

            Cat? kittySerialised = JsonSerializer.Deserialize<Cat>(jsonResponse);
            bool serialSuccess = kittySerialised != null;
            WriteToLog(extraData: $"Kitty is serialised : {serialSuccess}");

            if (!serialSuccess)
                return;

            CacheKitty(kittySerialised!);

            id_TextBox.Text = kittySerialised.id;

            string tagsInLine = string.Empty;
            foreach(string tag in kittySerialised.tags)
            {
                tagsInLine += $"{tag},";
            }
            tags_TextBox.Text = tagsInLine;

            created_at_TextBox.Text = kittySerialised.created_at.ToString();
            mimetype_TextBox.Text = kittySerialised.mimetype;

            ValidateDirectory("cache/imgs");
            var cachedimgs = Directory.GetFiles("cache/imgs");
            if (cachedimgs.Count() > 30)
            {
                for(int i = 0; i >= 10; i++)
                {
                    File.Delete(cachedimgs[i]);
                }
            }

            string pathToImg = $"cache/imgs/{kittySerialised.id}.png";
            if (!File.Exists(pathToImg))
            {
                byte[] fileBytes = await imgClient.GetByteArrayAsync(kittySerialised.url);
                await File.WriteAllBytesAsync(pathToImg, fileBytes);
            }

            BitmapImage bit = new BitmapImage();
            bit.BeginInit(); 
            bit.CacheOption = BitmapCacheOption.OnLoad;
            bit.UriSource = new Uri(Path.GetFullPath(pathToImg), UriKind.Absolute);
            bit.EndInit();

            cat_Image.Source = bit;
            WriteToLog(extraData: "Data loaded into UI");
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // TODO : call function that changes Image source to gif

            WriteToLog(extraData: "Calling on API");
            await GetAPIResponse();
        }

        private void DirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", $"{Directory.GetCurrentDirectory()}");
        }

        private void id_TextBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(id_TextBox.Text != String.Empty)
            Clipboard.SetText(id_TextBox.Text);
        }
    }
}
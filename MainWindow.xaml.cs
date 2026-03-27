using APIBullshit.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace APIBullshit
{
    public class CatResponse(string id, List<string> tags, DateTime created_at, string url, string mimetype)
    {
        public string id { get; } = id;
        public List<string> tags { get; } = tags;
        public DateTime created_at { get; } = created_at;
        public string url { get; } = url;
        public string mimetype { get; } = mimetype;
    }
    public class ConfigFile(string URL)
    {
        public string URL { get; } = URL;
    }
    public partial class MainWindow : Window
    {
        BitmapImage throbber = new BitmapImage();
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

                return config!.URL;
            }
            catch (Exception e){
                WriteToLog(e);

                ConfigFile configDefault = new ConfigFile(URL:"https://cataas.com/");
                string rawJSON = JsonSerializer.Serialize<ConfigFile>(configDefault);

                File.WriteAllText("config.json", rawJSON);

                return configDefault.URL;
            }
        }
        private static HttpClient jsonClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        private static HttpClient imgClient = new HttpClient() 
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        string timeNow()
        {
            return DateTime.Now.ToString("yyyy-mm-dd-HH-mm-ss"); 
        }
        private CatViewModel? currentCatView;
        

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

        private void CacheKitty(CatViewModel cacheableCat)
        {
            ValidateDirectory("cache");

            string rawJSON = JsonSerializer.Serialize<CatViewModel>(cacheableCat);
            File.WriteAllText($"cache/{cacheableCat.ID}.json", rawJSON);
        }
        private void TrimCache()
        {
            WriteToLog(extraData: "Trying to trim cache");

            try
            {
                string[] cachedImages = Directory.GetFiles("cache/imgs");

                if (cachedImages.Count() >= 30)
                {
                    var firstTenFiles = cachedImages.Take(10);
                    foreach (var file in firstTenFiles)
                    {
                        File.Delete($"cache/{Path.GetFileNameWithoutExtension(file)}.json");
                        File.Delete(file);
                    }
                    WriteToLog(extraData: "Deleted 10 oldest cached imgs and json files");
                }
            }
            catch (Exception e)
            {
                WriteToLog(e, extraData: "Trimming cache failed");
            }
        }
        private string ListToString(List<string> listInput)
        {
            string listInString = string.Empty;

            for(int i = 0; i < listInput.Count; i++)
            {
                WriteToLog(extraData: $"Tag {i} out of {listInput.Count}");
                bool isLastItem = i == (listInput.Count - 1);
                string lastChar = isLastItem ? "" : ",";

                listInString += $"{listInput[i]}{lastChar}";
            }

            return listInString;
        }

        public MainWindow()
        {
            try
            {
                CreateLogFile();
                InitializeComponent();

                DataContext = currentCatView;

                Uri addressUri = new Uri(APIURL());
                jsonClient.BaseAddress = addressUri;
                imgClient.BaseAddress = addressUri;
                jsonClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                imgClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

                throbber.BeginInit();
                throbber.UriSource = new Uri("pack://application:,,,/Images/Spinner.gif");
                throbber.EndInit();
            }
            catch(Exception e)
            {
                WriteToLog(error:e);
            }
        }
        private async Task GetAPIResponse()
        {
            HttpResponseMessage response = await jsonClient.GetAsync("cat?json=true");
            var jsonResponse = await response.Content.ReadAsStringAsync();
            WriteToLog(extraData: $"response: {jsonResponse}");

            CatResponse? kittyDeserialised = JsonSerializer.Deserialize<CatResponse>(jsonResponse);
            bool serialSuccess = kittyDeserialised != null;
            WriteToLog(extraData: $"Kitty is deserialised : {serialSuccess}");

            if (!serialSuccess)
                return;
            else
                WriteToLog(extraData: "Aborting GetAPIResponse due to Deserialize fail");

                CatViewModel catTranslate = new CatViewModel()
                {
                    ID = kittyDeserialised!.id,
                    URL = kittyDeserialised.url,
                    Created_At = kittyDeserialised.created_at,
                    MimeType = kittyDeserialised.mimetype,
                    Tags = ListToString(kittyDeserialised.tags)
                };

            currentCatView = catTranslate;
            CacheKitty(catTranslate);

            ValidateDirectory("cache/imgs");
            TrimCache();

            string pathToImg = $"cache/imgs/{catTranslate.ID}.png";

            if (!File.Exists(pathToImg))
            {
                WriteToLog(extraData: $"Saving image to {pathToImg}");
                byte[] fileBytes = await imgClient.GetByteArrayAsync(catTranslate.URL);
                await File.WriteAllBytesAsync(pathToImg, fileBytes);
            }
            else
                WriteToLog(extraData: $"{pathToImg} already exists");

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.UriSource = new Uri(pathToImg, UriKind.Relative);
            bitmapImage.EndInit();

            ImageBehavior.SetAnimatedSource(cat_Image, null);

            cat_Image.Source = bitmapImage;
            DataContext = currentCatView;
            WriteToLog(extraData: "Data loaded into UI");
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            ImageBehavior.SetAnimatedSource(cat_Image, throbber);

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
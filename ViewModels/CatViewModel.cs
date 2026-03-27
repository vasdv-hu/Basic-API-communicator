using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace APIBullshit.ViewModels
{
    public class CatViewModel : INotifyPropertyChanged
    {
        private string _id;
        private string _tags = "No Tags";
        private DateTime _created_at;
        private string _url;
        private string _mimetype;

        public string ID
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }
        public string Tags
        {
            get
            {
                if (_tags.Length > 0)
                    return _tags;
                else
                    return _tags = "No Tags";
            }
            set { _tags = value; OnPropertyChanged(); }
        }
        public DateTime Created_At
        {
            get => _created_at;
            set { _created_at = value; OnPropertyChanged(); }
        }
        public string URL
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); }
        }
        public string MimeType
        {
            get => _mimetype;
            set { _mimetype = value; OnPropertyChanged(); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace wpf_click_to_save_file
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();
        new MainPageBindingContext DataContext => (MainPageBindingContext)base.DataContext;

        public SaveFileDialog SaveFileDialog
        {
            get
            {
                if (_saveFileDialogSingleton is null)
                {
                    _saveFileDialogSingleton = new SaveFileDialog
                    {
                        Filter = "PNG Files (*.png)|*.png|JPG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg",
                        Title = "Save an Image File",
                        DefaultExt = ".png",
                        AddExtension = true,
                        InitialDirectory = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Assembly.GetExecutingAssembly().GetName().Name,
                        "Images")
                    };
                    Directory.CreateDirectory(SaveFileDialog.InitialDirectory);
                }
                return _saveFileDialogSingleton;
            }
        }
        SaveFileDialog? _saveFileDialogSingleton = null;

        private void Save_Clicked(object sender, RoutedEventArgs e)
        {
            BitmapEncoder encoder;
            if(SaveFileDialog.ShowDialog() == true)
            {
                switch (System.IO.Path.GetExtension(SaveFileDialog.FileName).ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                    default: throw new FileFormatException();
                }
                if (DataContext.CurrentImageSource is BitmapSource source)
                {
                    encoder.Frames.Add(BitmapFrame.Create(source));
                }
                using (FileStream fileStream = new FileStream(SaveFileDialog.FileName, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
                switch (MessageBox.Show("View file?", "Alert", MessageBoxButton.YesNo))
                {
                    case MessageBoxResult.Yes:
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                UseShellExecute = true,
                                FileName = SaveFileDialog.FileName,
                            });
                        }
                        catch{ Debug.Fail("Couldn't start default editor"); }
                        break;
                }
            }
        }
    }

    // <PackageReference Include = "CommunityToolkit.Mvvm" Version="8.3.2" />
    partial class MainPageBindingContext : ObservableObject
    {
        public MainPageBindingContext() => SelectedImageIndex = 0;

        [ObservableProperty]
        string _webFormatUrl = string.Empty;

        [ObservableProperty]
        ImageSource? _currentImageSource = default;

        [ObservableProperty]
        int _selectedImageIndex = -1;
        public Dictionary<string, string> ComboBoxItems { get; } = new Dictionary<string, string>()
        {
            {"Cat JPEG", "https://images.pexels.com/photos/1870376/pexels-photo-1870376.jpeg?auto=compress&cs=tinysrgb&w=800" },
            {"Rocks PNG", "https://filesampleshub.com/download/image/png/sample1.png" },
        };

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            switch (e.PropertyName)
            {
                case nameof(SelectedImageIndex):
                    WebFormatUrl = ComboBoxItems.Values.ToArray()[SelectedImageIndex];
                    break;
                case nameof(WebFormatUrl):
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(WebFormatUrl);
                    bitmap.EndInit();
                    CurrentImageSource = bitmap;
                    break;
            }
        }
    }

    class Command : ICommand
    {
        public Command(Action<object> action) => _action = action;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        private Action<object> _action;
        public void Execute(object? parameter) => _action(parameter);
    }
}
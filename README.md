The edit to your question _specifically, a button click_ requires a new answer. The binding that you show, `<Image Source="{Binding WebformatUrl}" />` doesn't seem to make sense if this is the objective. Why not just bind to `<Image Source="{Binding CurrentImageSource}" >` so that everything else is straightforward?

#### VM

```
// <PackageReference Include = "CommunityToolkit.Mvvm" Version="8.3.2" />
partial class MainPageBindingContext : ObservableObject
{
    public MainPageBindingContext() => SelectedImageIndex = 0;
    public ICommand SaveImageCommand { get; }

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
```

#### Code behind

```
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
```
___

#### Xaml (with a more performative binding)

```
<Window x:Class="wpf_click_to_save_file.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:wpf_click_to_save_file"
        mc:Ignorable="d"
        Title="MainWindow" Width="500" Height="300" 
        WindowStartupLocation="CenterScreen">
    <Window.DataContext>
        <local:MainPageBindingContext/>
    </Window.DataContext>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="10"/>
        </Grid.RowDefinitions>
        <Image
            Source="{Binding CurrentImageSource}"
            Margin="20" />
        <StackPanel
            Grid.Row="1"
            HorizontalAlignment="Center"
            Orientation="Horizontal">
            <ComboBox
                ItemsSource="{Binding ComboBoxItems}"
                SelectedIndex="{Binding SelectedImageIndex}" 
                VerticalContentAlignment="Center"
                DisplayMemberPath="Key"
                SelectedValuePath="Value"
                Width="150" Height="30" Margin="20,0" />
        </StackPanel>
        <Button 
            Grid.Row="2"
            Width="150" 
            Height="30"
            Margin="0,10"
            VerticalContentAlignment="Center"
            Content="Save" 
            Click="Save_Clicked"/>
    </Grid>
</Window>
```


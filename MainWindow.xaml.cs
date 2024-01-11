using NAudio.Wave;
using Newtonsoft.Json;
using Record.Models;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Record
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<StationModel> _stationList;

        private StationModel _selectedStation;

        // WasapiOut?
        private WaveOutEvent _wo = new();

        private System.Windows.Forms.NotifyIcon _tray;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            Background = GetImageBrush("record_new.jpeg");
            Image1.Source = GetBitmapImage("record_image600_white_fill.png");
            Image2.Source = GetBitmapImage("play.png");

            ComboBox2.Items.Add("64 kbit/s");
            ComboBox2.Items.Add("128 kbit/s");
            ComboBox2.Items.Add("320 kbit/s");
            ComboBox2.SelectedIndex = 1;

            _tray = new System.Windows.Forms.NotifyIcon()
            {
                Icon = Properties.Resources.favicon,
                Visible = false,
                Text = Title
            };
            _tray.Click += (sender, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                _tray.Visible = false;
            };

            _ = LoadStations();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private async Task LoadStations()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);

                    var response = await client.GetAsync("https://www.radiorecord.ru/api/stations/");

                    var json = await response.Content.ReadAsStringAsync();

                    var model = JsonConvert.DeserializeObject<StationsModel>(json);

                    _stationList = model.Result.Stations;
                    _stationList.ForEach(st =>
                    {
                        ComboBox1.Items.Add(new ComboBoxItem
                        {
                            Text = st.Title,
                            Value = st.Id,
                        });
                    });
                    ComboBox1.SelectedIndex = 0;

                    //Debug.WriteLine("debug: " + JsonConvert.SerializeObject(model, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private async void PlayStop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;

            if (_wo.PlaybackState == PlaybackState.Playing)
            {
                _wo.Stop();
                Image2.Source = GetBitmapImage("play.png");
            }
            else
            {
                await Play();
                Image2.Source = GetBitmapImage("stop.png");
            }
        }

        private void PlayStop_MouseEnter(object sender, MouseEventArgs e)
        {
            Image2.Source = GetBitmapImage((_wo.PlaybackState == PlaybackState.Playing ? "stop-hover" : "play-hover") + ".png");
        }

        private void PlayStop_MouseLeave(object sender, MouseEventArgs e)
        {
            Image2.Source = GetBitmapImage((_wo.PlaybackState == PlaybackState.Playing ? "stop" : "play") + ".png");
        }

        private async void ComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedStationId = ((ComboBoxItem)ComboBox1.SelectedItem).Value;

            _selectedStation = _stationList.Find(st => st.Id == selectedStationId);

            //Title = $"{_selectedStation.Title} - {_selectedStation.Tooltip}";

            _ = Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    Background = GetImageBrushByUrl(_selectedStation.BgImage);
                    Image1.Source = GetImageSourceByUrl(_selectedStation.IconFillWhite);
                });
            });

            if (_wo.PlaybackState == PlaybackState.Playing)
            {
                _wo.Stop();
                Image2.Source = GetBitmapImage("play.png");
                await Play();
                Image2.Source = GetBitmapImage("stop.png");
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _wo.Volume = (float) Slider1.Value / 20;
        }

        private async void ComboBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_wo.PlaybackState == PlaybackState.Playing)
            {
                _wo.Stop();
                await Play();
                Image2.Source = GetBitmapImage("stop.png");
            }
        }

        private static BitmapImage GetBitmapImage(string fileName)
        {
            return new BitmapImage(new Uri(@"pack://application:,,,/Resources/" + fileName));
        }

        private static ImageBrush GetImageBrushByUrl(string url)
        {
            return new ImageBrush
            {
                ImageSource = GetImageSourceByUrl(url)
            };
        }

        private static ImageSource GetImageSourceByUrl(string url)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url);
            bitmap.EndInit();

            return bitmap;
        }

        private static ImageBrush GetImageBrush(string url)
        {
            return new ImageBrush
            {
                ImageSource = GetImageSource(url)
            };
        }

        private static ImageSource GetImageSource(string fileName)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(@"pack://application:,,,/Resources/" + fileName);
            bitmap.EndInit();

            return bitmap;
        }

        private async Task Play()
        {
            var selectedIndex = ComboBox2.SelectedIndex;

            var task = Task.Run(() =>
            {
                try
                {
                    var urls = new string[] {
                    _selectedStation.Stream64,
                    _selectedStation.Stream128,
                    _selectedStation.Stream320
                };
                    var url = urls[selectedIndex];

                    using (var mf = new MediaFoundationReader(url))
                    {
                        _wo.Init(mf);
                        _wo.Play();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            });

            await task;
        }

        private void close_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void minimize_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Hide();
            WindowState = WindowState.Minimized;

            _tray.Visible = true;
        }
    }
}
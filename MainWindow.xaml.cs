using NAudio.Extras;
using NAudio.Wave;
using Newtonsoft.Json;
using Record.Models;
using System.Diagnostics;
using System.IO;
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
        private readonly WaveOutEvent _player = new ();

        private System.Windows.Forms.NotifyIcon _tray;

        private Equalizer _equalizer;

        private readonly List<CustomEqualizer> _equalizerList;

        private CustomEqualizerBand[] _equalizerBandList = new CustomEqualizerBand[8];

        private PreferencesModel _preferences;

        private bool _manualChangeEqualizer = true;

        public MainWindow()
        {
            try
            {
                _preferences = JsonConvert.DeserializeObject<PreferencesModel>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Record.json")));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Application.Current.Shutdown();
            }

            _equalizerList = new List<CustomEqualizer> {
                new()
                {
                    Name = "Base",
                    BandList = new CustomEqualizerBand[] {
                        new() { Name = "Band1", Bandwidth = 0.8f, Frequency = 100, Gain = 0 },
                        new() { Name = "Band2", Bandwidth = 0.8f, Frequency = 200, Gain = 0 },
                        new() { Name = "Band3", Bandwidth = 0.8f, Frequency = 400, Gain = 0 },
                        new() { Name = "Band4", Bandwidth = 0.8f, Frequency = 800, Gain = 0 },
                        new() { Name = "Band5", Bandwidth = 0.8f, Frequency = 1200, Gain = 0 },
                        new() { Name = "Band6", Bandwidth = 0.8f, Frequency = 2400, Gain = 0 },
                        new() { Name = "Band7", Bandwidth = 0.8f, Frequency = 4800, Gain = 0 },
                        new() { Name = "Band8", Bandwidth = 0.8f, Frequency = 9600, Gain = 0 }
                    }
                },
                new()
                {
                    Name = "Classic",
                    BandList = new CustomEqualizerBand[] {
                        new() { Name = "Band1", Bandwidth = 0.8f, Frequency = 100, Gain = 9 },
                        new() { Name = "Band2", Bandwidth = 0.8f, Frequency = 200, Gain = -3 },
                        new() { Name = "Band3", Bandwidth = 0.8f, Frequency = 400, Gain = -12 },
                        new() { Name = "Band4", Bandwidth = 0.8f, Frequency = 800, Gain = -3 },
                        new() { Name = "Band5", Bandwidth = 0.8f, Frequency = 1200, Gain = 3 },
                        new() { Name = "Band6", Bandwidth = 0.8f, Frequency = 2400, Gain = 9 },
                        new() { Name = "Band7", Bandwidth = 0.8f, Frequency = 4800, Gain = 12 },
                        new() { Name = "Band8", Bandwidth = 0.8f, Frequency = 9600, Gain = 15 }
                    }
                },
                new()
                {
                    Name = "Loudness",
                    BandList = new CustomEqualizerBand[] {
                        new() { Name = "Band1", Bandwidth = 0.8f, Frequency = 100, Gain = 18 },
                        new() { Name = "Band2", Bandwidth = 0.8f, Frequency = 200, Gain = 9 },
                        new() { Name = "Band3", Bandwidth = 0.8f, Frequency = 400, Gain = 3 },
                        new() { Name = "Band4", Bandwidth = 0.8f, Frequency = 800, Gain = 0 },
                        new() { Name = "Band5", Bandwidth = 0.8f, Frequency = 1200, Gain = 0 },
                        new() { Name = "Band6", Bandwidth = 0.8f, Frequency = 2400, Gain = 0 },
                        new() { Name = "Band7", Bandwidth = 0.8f, Frequency = 4800, Gain = 6 },
                        new() { Name = "Band8", Bandwidth = 0.8f, Frequency = 9600, Gain = 12 }
                    }
                },
                new()
                {
                    Name = "Pop",
                    BandList = new CustomEqualizerBand[] {
                        new() { Name = "Band1", Bandwidth = 0.8f, Frequency = 100, Gain = 0 },
                        new() { Name = "Band2", Bandwidth = 0.8f, Frequency = 200, Gain = 6 },
                        new() { Name = "Band3", Bandwidth = 0.8f, Frequency = 400, Gain = 9 },
                        new() { Name = "Band4", Bandwidth = 0.8f, Frequency = 800, Gain = 6 },
                        new() { Name = "Band5", Bandwidth = 0.8f, Frequency = 1200, Gain = 3 },
                        new() { Name = "Band6", Bandwidth = 0.8f, Frequency = 2400, Gain = 0 },
                        new() { Name = "Band7", Bandwidth = 0.8f, Frequency = 4800, Gain = 0 },
                        new() { Name = "Band8", Bandwidth = 0.8f, Frequency = 9600, Gain = 0 }
                    }
                },
                new()
                {
                    Name = "Rock",
                    BandList = new CustomEqualizerBand[] {
                        new() { Name = "Band1", Bandwidth = 0.8f, Frequency = 100, Gain = 12 },
                        new() { Name = "Band2", Bandwidth = 0.8f, Frequency = 200, Gain = 6 },
                        new() { Name = "Band3", Bandwidth = 0.8f, Frequency = 400, Gain = 0 },
                        new() { Name = "Band4", Bandwidth = 0.8f, Frequency = 800, Gain = -6 },
                        new() { Name = "Band5", Bandwidth = 0.8f, Frequency = 1200, Gain = -3 },
                        new() { Name = "Band6", Bandwidth = 0.8f, Frequency = 2400, Gain = 3 },
                        new() { Name = "Band7", Bandwidth = 0.8f, Frequency = 4800, Gain = 9 },
                        new() { Name = "Band8", Bandwidth = 0.8f, Frequency = 9600, Gain = 12 }
                    }
                },
                _preferences.CustomEqualizer
            };

            /**
             * Sub-Bass (20-60Hz)
             * Low Mids (200-600Hz)
             * Mids (600Hz-3kHz)
             * Upper Mids (3-8kHz)
             * Highs (8kHz+)
             * */

            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            Background = GetImageBrush("record_new.jpg");
            Image1.Source = GetBitmapImage("record_image600_white_fill.png");
            Image2.Source = GetBitmapImage("play.png");

            ComboBox2.Items.Add("64 kbit/s");
            ComboBox2.Items.Add("128 kbit/s");
            ComboBox2.Items.Add("320 kbit/s");
            ComboBox2.SelectedIndex = 1;

            foreach (var equalizer in _equalizerList)
            {
                ComboBox3.Items.Add(equalizer.Name);
            }

            ComboBox3.SelectedIndex = _preferences.ActiveEqualizerIndex;

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

            Header.MouseLeftButtonDown += (sender, e) =>
            {
                DragMove();
            };

            _ = LoadStations();
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

                    //Debug.WriteLine("debug: " + JsonConvert.SerializeObject(model, Formatting.Indented));

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

                    _ = Task.Run(async () => {
                        while (true)
                        {
                            await LoadStationsNow();
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    });
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private async Task LoadStationsNow()
        {
            try
            {
                using (var client = new HttpClient()) {
                    var response = await client.GetAsync("https://www.radiorecord.ru/api/stations/now/");

                    var json = await response.Content.ReadAsStringAsync();

                    var model = JsonConvert.DeserializeObject<StationsNowModel>(json);

                    var track = model.Result.SingleOrDefault(station => station.Id == _selectedStation.Id).Track;

                    _ = Task.Run(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var trackImageUrl = track.Image100.StartsWith("http") 
                                ? track.Image100 
                                : "https://www.radiorecord.ru" + track.Image100;

                            TrackImage.Source = GetImageSourceByUrl(trackImageUrl);
                            TrackImage.Tag = track.ShareUrl;

                            Song.Content = track.Song;
                            Artist.Content = track.Artist;
                        });
                    });
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

            if (_player.PlaybackState == PlaybackState.Playing)
            {
                _player.Stop();
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
            Image2.Source = GetBitmapImage((_player.PlaybackState == PlaybackState.Playing ? "stop-hover" : "play-hover") + ".png");
        }

        private void PlayStop_MouseLeave(object sender, MouseEventArgs e)
        {
            Image2.Source = GetBitmapImage((_player.PlaybackState == PlaybackState.Playing ? "stop" : "play") + ".png");
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
                    Background = GetImageBrushByUrl(_selectedStation.BgImageMobile);
                    Image1.Source = GetImageSourceByUrl(_selectedStation.IconFillWhite);
                });
            });

            if (_player.PlaybackState == PlaybackState.Playing)
            {
                _player.Stop();
                Image2.Source = GetBitmapImage("play.png");
                await Play();
                Image2.Source = GetBitmapImage("stop.png");
            }
        }

        private void VolumeValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _player.Volume = (float) Volume.Value / 10;
        }

        private async void ComboBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_player.PlaybackState == PlaybackState.Playing)
            {
                _player.Stop();
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
            var selectedBitrateIndex = ComboBox2.SelectedIndex;

            var task = Task.Run(() =>
            {
                try
                {
                    var urls = new string[] {
                        _selectedStation.Stream64,
                        _selectedStation.Stream128,
                        _selectedStation.Stream320
                    };
                    var url = urls[selectedBitrateIndex];

                    using (var reader = new MediaFoundationReader(url))
                    {
                        _equalizer = new Equalizer(reader.ToSampleProvider(), _equalizerBandList);

                        _player.Init(_equalizer);
                        _player.Play();
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

        private void BandGainValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //var bandName = (sender as Slider).Name;
            var bandName = (sender as Slider).Tag.ToString();

            if (_manualChangeEqualizer && ComboBox3.SelectedIndex != 5)
            {
                ComboBox3.SelectedIndex = 5;
                return;
            }

            _equalizerBandList.SingleOrDefault(band => band.Name == bandName).Gain = (float)e.NewValue;
            _equalizer?.Update();
        }

        private void EqualizerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ComboBox3.SelectedIndex;

            _preferences.ActiveEqualizerIndex = index;

            for (int i = 0; i < 8; i++)
            {
                _equalizerBandList[i] = _equalizerList[index].BandList[i];
            }

            _manualChangeEqualizer = false;

            Band1Gain.Value = _equalizerBandList.SingleOrDefault(band => band.Name == "Band1").Gain;
            Band2Gain.Value = _equalizerBandList.SingleOrDefault(band => band.Name == "Band2").Gain;
            Band3Gain.Value = _equalizerBandList.SingleOrDefault(band => band.Name == "Band3").Gain;
            Band4Gain.Value = _equalizerBandList.SingleOrDefault(band => band.Name == "Band4").Gain;
            Band5Gain.Value = _equalizerBandList.SingleOrDefault(band => band.Name == "Band5").Gain;
            Band6Gain.Value = _equalizerBandList.SingleOrDefault(band => band.Name == "Band6").Gain;
            Band7Gain.Value = _equalizerBandList.SingleOrDefault(band => band.Name == "Band7").Gain;
            Band8Gain.Value = _equalizerBandList.SingleOrDefault(band => band.Name == "Band8").Gain;

            _manualChangeEqualizer = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 8; i++)
            {
                _equalizerList[5].BandList[i].Gain = 0;
            }

            ComboBox3.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "Record.json"), JsonConvert.SerializeObject(_preferences, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ClickTrackImage(object sender, MouseButtonEventArgs e)
        {
            var url = (sender as Image).Tag.ToString();

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
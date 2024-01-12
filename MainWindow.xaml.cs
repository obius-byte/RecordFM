using NAudio.Extras;
using NAudio.Wave;
using Newtonsoft.Json;
using Record.Models;
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
        private readonly WaveOutEvent _wo = new();

        private System.Windows.Forms.NotifyIcon _tray;

        private Equalizer _equalizer;

        private PreferencesModel _preferences;

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

            foreach (var customEqualizer in _preferences.CustomEqualizerList)
            {
                ComboBox3.Items.Add(customEqualizer.Name);
            }
            ComboBox3.SelectedIndex = _preferences.ActiveEqualizerIndex;

            EQActivate.IsChecked =_preferences.ActiveEqualizerIndex != -1;

            if (_preferences.ActiveEqualizerIndex != -1)
            {
                var bandList = _preferences.CustomEqualizerList[_preferences.ActiveEqualizerIndex].BandList;
                
                Band1Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band1").Gain;
                Band2Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band2").Gain;
                Band3Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band3").Gain;
                Band4Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band4").Gain;
                Band5Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band5").Gain;
                Band6Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band6").Gain;
                Band7Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band7").Gain;
                Band8Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band8").Gain;
            }

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
                    Background = GetImageBrushByUrl(_selectedStation.BgImageMobile);
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
            var selectedBitrateIndex = ComboBox2.SelectedIndex;
            var isEqActivated = EQActivate.IsChecked ?? false;
            var selectedEqualizerIndex = ComboBox3.SelectedIndex;

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

                    using (var mf = new MediaFoundationReader(url))
                    {
                        var equalizerBandList = isEqActivated && selectedEqualizerIndex != -1
                            ? _preferences.CustomEqualizerList[selectedEqualizerIndex].BandList
                            : _preferences.BaseEqualizerBandList;

                        _equalizer = new Equalizer(mf.ToSampleProvider(), equalizerBandList);

                        _wo.Init(_equalizer);
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

        private void CustomEqualizerValueChanged(int index, float value)
        {
            if (ComboBox3.SelectedIndex > 3)
            {
                _preferences.CustomEqualizerList[ComboBox3.SelectedIndex].BandList[index].Gain = value;
            }

            _equalizer?.Update();
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CustomEqualizerValueChanged(0, (float)e.NewValue);
        }

        private void Slider_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CustomEqualizerValueChanged(1, (float)e.NewValue);
        }

        private void Slider_ValueChanged_3(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CustomEqualizerValueChanged(2, (float)e.NewValue);
        }

        private void Slider_ValueChanged_4(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CustomEqualizerValueChanged(3, (float)e.NewValue);
        }

        private void Slider_ValueChanged_5(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CustomEqualizerValueChanged(4, (float)e.NewValue);
        }

        private void Slider_ValueChanged_6(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CustomEqualizerValueChanged(5, (float)e.NewValue);
        }

        private void Slider_ValueChanged_7(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CustomEqualizerValueChanged(6, (float)e.NewValue);
        }

        private void Slider_ValueChanged_8(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CustomEqualizerValueChanged(7, (float)e.NewValue);
        }

        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_wo.PlaybackState == PlaybackState.Playing)
            {
                _wo.Stop();
                _ = Play();
            }
        }

        private void ComboBox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ComboBox3.SelectedIndex;

            _preferences.ActiveEqualizerIndex = index;

            var bandList = index == -1 ? _preferences.BaseEqualizerBandList : _preferences.CustomEqualizerList[index].BandList;

            var isEditable = index > 3;

            Band1Gain.IsEnabled = isEditable;
            Band2Gain.IsEnabled = isEditable;
            Band3Gain.IsEnabled = isEditable;
            Band4Gain.IsEnabled = isEditable;
            Band5Gain.IsEnabled = isEditable;
            Band6Gain.IsEnabled = isEditable;
            Band7Gain.IsEnabled = isEditable;
            Band8Gain.IsEnabled = isEditable;

            Band1Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band1").Gain;
            Band2Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band2").Gain;
            Band3Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band3").Gain;
            Band4Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band4").Gain;
            Band5Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band5").Gain;
            Band6Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band6").Gain;
            Band7Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band7").Gain;
            Band8Gain.Value = bandList.FirstOrDefault(b => b.Name == "Band8").Gain;

            var isActivated = EQActivate.IsChecked ?? false;

            if (isActivated &&  _wo.PlaybackState == PlaybackState.Playing)
            {
                _wo.Stop();
                _ = Play();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Band1Gain.Value = 0;
            Band2Gain.Value = 0;
            Band3Gain.Value = 0;
            Band4Gain.Value = 0;
            Band5Gain.Value = 0;
            Band6Gain.Value = 0;
            Band7Gain.Value = 0;
            Band8Gain.Value = 0;

            ComboBox3.SelectedItem = null;
        }
    }
}
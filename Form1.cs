using NAudio.Wave;
using Newtonsoft.Json;
using Record.Models;
using Record.Properties;
using System.Diagnostics;

namespace Record
{
    public partial class Form1 : Form
    {
        public List<StationModel> _stationList;

        public StationModel _selectedStation;

        // WasapiOut?
        public WaveOutEvent _wo = new();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox2.SelectedIndex = 2;
            _wo.Volume = 1;

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

                    _stationList = model.Result.Stations;
                    _stationList.ForEach(st =>
                    {
                        comboBox1.Items.Add(new ComboboxItem
                        {
                            Text = st.Title,
                            Value = st.Id,
                        });
                    });

                    comboBox1.SelectedIndex = 0;

                    //Debug.WriteLine("debug: " + JsonConvert.SerializeObject(model, Formatting.Indented));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to load stations: {e}");
            }
        }

        class ComboboxItem
        {
            public string Text { get; set; }

            public int Value { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private void Play()
        {
            try
            {
                var urls = new string[] {
                    _selectedStation.Stream64,
                    _selectedStation.Stream128,
                    _selectedStation.Stream320
                };
                var url = urls[comboBox2.SelectedIndex];

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
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedStationId = ((ComboboxItem)comboBox1.SelectedItem).Value;

            _selectedStation = _stationList.Find(st => st.Id == selectedStationId);

            Text = $"{_selectedStation.Title} - {_selectedStation.Tooltip}";

            Task.Run(async () =>
            {
                BackgroundImage = await GetImageByUrl(_selectedStation.BgImage);
                pictureBox1.BackgroundImage = await GetImageByUrl(_selectedStation.IconFillWhite);
            });

            if (_wo.PlaybackState == PlaybackState.Playing)
            {
                _wo.Stop();
                Play();
                pictureBox2.BackgroundImage = Resources.stop;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var isPalying = _wo.PlaybackState == PlaybackState.Playing;

            pictureBox2.BackgroundImage = isPalying ? Resources.play : Resources.stop;

            if (isPalying)
            {
                _wo.Stop();
            }
            else
            {
                Play();
            }
        }

        private async Task<Image> GetImageByUrl(string url)
        {
            using var client = new HttpClient();
            {
                using var stream = await client.GetStreamAsync(url);
                {
                    return Image.FromStream(stream);
                }
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            _wo.Volume = (float)trackBar1.Value / 10;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_wo.PlaybackState == PlaybackState.Playing)
            {
                _wo.Stop();
                Play();
                pictureBox2.BackgroundImage = Resources.stop;
            }
        }
    }
}

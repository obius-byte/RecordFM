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

        private readonly ToolStripMenuItem _trayPlay;
        private readonly ToolStripMenuItem _trayStop;

        // WasapiOut?
        public WaveOutEvent _wo = new();

        public Form1()
        {
            InitializeComponent();

            notifyIcon1 = new NotifyIcon()
            {
                Icon = Resources.favicon,
                ContextMenuStrip = new ContextMenuStrip()
            };

            _trayPlay = new("PLAY", null, new EventHandler((sender, e) =>
            {
                if (_wo.PlaybackState == PlaybackState.Stopped)
                {
                    Play();
                    _trayStop.Enabled = true;
                    _trayPlay.Enabled = false;
                }
            }), "PLAY");

            _trayStop = new("STOP", null, new EventHandler((sender, e) =>
            {
                _wo.Stop();
                _trayStop.Enabled = false;
                _trayPlay.Enabled = true;
            }), "STOP")
            {
                Enabled = false
            };

            notifyIcon1.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                _trayPlay,
                _trayStop,
                new ToolStripSeparator(),
                new ToolStripMenuItem("EXIT", null, new EventHandler((sender, e) => {
                    notifyIcon1.Visible = false;
                    Environment.Exit(1);
                }), "EXIT")
            });
            notifyIcon1.DoubleClick += (sender, e) =>
            {
                notifyIcon1.Visible = false;
                Show();
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox2.SelectedIndex = 1;
            _wo.Volume = 0.5f;

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
                MessageBox.Show(e.ToString());
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

            _trayPlay.Enabled = isPalying;
            _trayStop.Enabled = !isPalying;
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
            _wo.Volume = (float)trackBar1.Value / 20;
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;

            notifyIcon1.Visible = true;
            Hide();
        }
    }
}

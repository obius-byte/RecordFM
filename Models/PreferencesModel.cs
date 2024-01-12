namespace Record.Models
{
    class PreferencesModel
    {
        public int ActiveEqualizerIndex { get; set; }

        public CustomEqualizerBand[] BaseEqualizerBandList { get; set; }

        public CustomEqualizer[] CustomEqualizerList { get; set; }
    }
}

namespace Record.Models
{
    public class StationNowTrackModel
    {
        public int Id { get; set; }

        public string Artist { get; set; }

        public string Song { get; set; }

        public string Image100 { get; set; }

        public string Image200 { get; set; }

        public string ListenUrl { get; set; }

        public string ItunesUrl { get; set; }

        public string ItunesId { get; set; }

        public bool NoFav { get; set; }

        public bool NoShow { get; set; }

        public string ShareUrl { get; set; }
    }
}

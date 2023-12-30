using Newtonsoft.Json;

namespace Record.Models
{
    public class TagModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [JsonProperty("detail_picture")]
        public string DetailPicture { get; set; }

        public string Picture { get; set; }

        public string Svg { get; set; }

        public string Pdf { get; set; }
    }
}

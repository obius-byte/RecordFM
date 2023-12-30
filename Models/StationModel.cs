using Newtonsoft.Json;

namespace Record.Models
{
    public class StationModel
    {
        public int Id { get; set; }

        public string Prefix { get; set; }

        public string Title { get; set; }

        public string Tooltip { get; set; }

        public int Sort { get; set; }

        [JsonProperty("bg_color")]
        public string BgColor { get; set; }

        [JsonProperty("bg_image")]
        public string BgImage { get; set; }

        [JsonProperty("bg_image_mobile")]
        public string BgImageMobile { get; set; }

        [JsonProperty("svg_outline")]
        public string SvgOutline { get; set; }

        [JsonProperty("svg_fill")]
        public string SvgFill { get; set; }

        [JsonProperty("pdf_outline")]
        public string PdfOutline { get; set; }

        [JsonProperty("pdf_fill")]
        public string PdfFill { get; set; }

        [JsonProperty("short_title")]
        public string ShortTitle { get; set; }

        [JsonProperty("icon_gray")]
        public string IconGray { get; set; }

        [JsonProperty("icon_fill_colored")]
        public string IconFillColored { get; set; }

        [JsonProperty("icon_fill_white")]
        public string IconFillWhite { get; set; }

        public bool New {  get; set; }

        [JsonProperty("new_date")]
        public string NewDate { get; set; }

        [JsonProperty("stream_64")]
        public string Stream64 { get; set; }

        [JsonProperty("stream_128")]
        public string Stream128 { get; set; }

        [JsonProperty("stream_320")]
        public string Stream320 { get; set; }

        [JsonProperty("stream_hls")]
        public string StreamHls { get; set; }

        public List<GenreModel> Genre { get; set; }

        [JsonProperty("detail_page_url")]
        public string DetailPageUrl { get; set; }

        public string ShareUrl { get; set; }

        public string Mark { get; set; }

        public string Updated { get; set; }
    }
}

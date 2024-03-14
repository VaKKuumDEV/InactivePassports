using Newtonsoft.Json;

namespace MVD.Util
{
    public class Config
    {
        [JsonProperty("link")] public string Link { get; set; } = "http://xn--b1ab2a0a.xn--b1aew.xn--p1ai/upload/expired-passports/list_of_expired_passports.csv.bz2";
        [JsonProperty("update_time"), JsonConverter(typeof(TimeConverter))] public TimeOnly UpdateTime { get; set; } = new(23, 00, 00);
    }
}

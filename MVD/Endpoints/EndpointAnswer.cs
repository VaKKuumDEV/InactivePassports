using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MVD.Endpoints
{
    public class EndpointAnswer
    {
        public const int ERROR_CODE = 0;
        public const int SUCCESS_CODE = 1;

        public int Code { get; private set; } = 0;
        public string Message { get; private set; }
        public Dictionary<string, object> Params { get; private set; }

        public EndpointAnswer(int code, string message, Dictionary<string, object> pars)
        {
            Code = code;
            Message = message;
            Params = pars;
        }

        public EndpointAnswer(int code, string message) : this(code, message, new()) { }

        public override string ToString()
        {
            JObject data = new()
            {
                ["code"] = Code,
                ["message"] = Message,
            };

            foreach (var kv in Params) data.Add(kv.Key, JToken.FromObject(kv.Value));

            return data.ToString(Formatting.Indented);
        }
    }
}

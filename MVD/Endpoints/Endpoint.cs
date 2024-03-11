namespace MVD.Endpoints
{
    abstract public class Endpoint
    {
        public enum Types
        {
            GET,
            POST,
            PUT,
            DELETE,
        };

        public string Name { get; }
        public bool HasIdParam { get; } = false;
        public Types Type { get; }

        public Endpoint(string name, Types type, bool hasIdParam = false)
        {
            Name = name;
            Type = type;
            HasIdParam = hasIdParam;
        }

        public abstract Task<EndpointAnswer> Execute(params string[] httpParams);
    }
}

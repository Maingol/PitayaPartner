namespace PitayaPartner;

public class RequestTimeoutException : Exception
{
    public string Route { get; }

    public RequestTimeoutException(string route)
        : base($"Request timed out. Route info: {route}.")
    {
        Route = route;
    }
}

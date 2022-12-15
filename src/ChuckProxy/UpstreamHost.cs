namespace ChuckProxy;

/// <summary>
///     Represents an upstream host to which requests can be forwarded to.
/// </summary>
public sealed class UpstreamHost : IEquatable<UpstreamHost>
{
    #region Properties

    public string Scheme { get; }

    public HostString Host { get; }

    public PathString PathBase { get; }

    public uint Weight { get; }

    public Uri Uri { get; }

    #endregion

    #region Constructors

    public UpstreamHost(string scheme, HostString host, PathString pathBase = default, uint weight = 1)
    {
        if (string.IsNullOrWhiteSpace(scheme))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(scheme));
        if (!host.HasValue)
            throw new ArgumentException("Value must be supplied", nameof(host));

        Scheme = scheme;
        Host = host;
        PathBase = pathBase;
        Weight = weight;
        Uri = GetUri();
    }

    public UpstreamHost(string uri, uint weight = 1)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(uri));

        var upstreamUri = new Uri(uri);

        Scheme = upstreamUri.Scheme;
        Host = HostString.FromUriComponent(upstreamUri);
        PathBase = PathString.FromUriComponent(upstreamUri);
        Weight = weight;
        Uri = GetUri();
    }

    #endregion

    #region Private Methods

    private Uri GetUri()
    {
        var port = Host.Port ?? DefaultPort(Scheme);
        var builder = new UriBuilder(Scheme, Host.Host, port, PathBase.Value);
        return builder.Uri;
    }

    private static int DefaultPort(string scheme)
    {
        return scheme.ToLower() switch
        {
            "http" or "ws" => 80,
            "https" or "wss" => 443,
            _ => throw new NotSupportedException(),
        };
    }

    #endregion

    #region IEquatable Impl

    public override string ToString()
    {
        return $"{Scheme}://{Host}{PathBase.Value}";
    }

    public static implicit operator UpstreamHost(string uri) => new Uri(uri);

    public static implicit operator UpstreamHost(Uri upstreamUri) => new(
        upstreamUri.Scheme,
        HostString.FromUriComponent(upstreamUri),
        PathString.FromUriComponent(upstreamUri)
    );

    public bool Equals(UpstreamHost? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(Scheme, other.Scheme) &&
            Host.Equals(other.Host) &&
            PathBase.Equals(other.PathBase);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((UpstreamHost)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Scheme.GetHashCode();
            hashCode = (hashCode * 397) ^ Host.GetHashCode();
            hashCode = (hashCode * 397) ^ PathBase.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(UpstreamHost left, UpstreamHost right) => Equals(left, right);

    public static bool operator !=(UpstreamHost left, UpstreamHost right) => !Equals(left, right);

    #endregion
}
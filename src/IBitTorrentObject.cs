using System.Text;

namespace codecrafters_bittorrent;

public interface IBitTorrentObject;

public sealed class BitTorrentNumber : IBitTorrentObject 
{
    public long Value { get; }

    public BitTorrentNumber(long value) => Value = value;
    
    public override string ToString()
    {
        return Value.ToString();
    }
}

public sealed class BitTorrentString : IBitTorrentObject
{
    public long Length { get; }
    public string Value { get; }

    public BitTorrentString(long len, string value) => (Length, Value) = (len, value);

    public override string ToString()
    {
        return $"\"{Value}\"";
    }
}

public sealed class BitTorrentList : IBitTorrentObject
{
    public List<IBitTorrentObject> Values { get; }

    public BitTorrentList(List<IBitTorrentObject> values) => Values = values;

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append('[');
        var i = 1;
        foreach (var value in Values)
        {
            builder.Append(value);
            if (i < Values.Count)
                builder.Append(',');
            i++;
        }
        builder.Append(']');

        return builder.ToString();
    }
}

public sealed class BitTorrentByteArray : IBitTorrentObject
{
    public long Length { get; }
    public byte[] Value { get; }
    
    public BitTorrentByteArray(long len, byte[] value) => (Length, Value) = (len, value);

    public override string ToString()
    {
        return Convert.ToHexString(Value).ToLower();
    }
}

public sealed class BitTorrentDictionary : IBitTorrentObject
{
    public Dictionary<BitTorrentString, IBitTorrentObject> Dict { get; } = [];

    public BitTorrentDictionary() {}

    public void Add(BitTorrentString key, IBitTorrentObject value) => Dict.Add(key, value);

    public IBitTorrentObject GetByString(string keyStr)
    {
        foreach (var (key, value) in Dict)
        {
            if (key.Value == keyStr)
                return value;
        }

        return new BitTorrentString(0, string.Empty);
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append('{');
        var i = 1;
        foreach (var (key, value) in Dict)
        {
            builder.Append(key).Append(':').Append(value);
            if (i < Dict.Count)
                builder.Append(',');
            i++;
        }
        builder.Append('}');

        return builder.ToString();
    }
}

public sealed class BitTorrentMetainfo
{
    public const string InfoHashMarker = "4:infod";
    public string? Announce { get; init; }
    public string? CreatedBy { get; init; }
    public BitTorrentMetinfoInfo? Info { get; init; }
    public string? Hash { get; init; }
    public byte[]? HashBytes { get; init; }

    public override string ToString()
    {
        StringBuilder builder = new();
        if (Announce is not null) builder.Append("Tracker URL: ").Append(Announce).Append('\n');
        if (Info is not null) builder.Append(Info);
        if (Hash is not null) builder.Append("Hash info: ").Append(Hash).Append('\n');
        return builder.ToString();
    }
}

public sealed class BitTorrentMetinfoInfo
{
    public long? Length { get; init; }
    public string? Name { get; init; }
    public long PieceLength { get; init; }
    public byte[]? Pieces { get; init; }

    public override string ToString()
    {
        StringBuilder builder = new();
        if (Length is not null) builder.Append("Length: ").Append(Length).Append('\n');
        return builder.ToString();
    }
}

public sealed class BitTorrentPeerResponse
{
    public int Interval { get; init; }
    public string Peers { get; init; }
} 
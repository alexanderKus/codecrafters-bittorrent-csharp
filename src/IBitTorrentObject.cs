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

public sealed class BitTorrentDictionary : IBitTorrentObject
{
    public Dictionary<BitTorrentString, IBitTorrentObject> Dictionary { get; } = [];

    public BitTorrentDictionary() {}

    public void Add(BitTorrentString key, IBitTorrentObject value) => Dictionary.Add(key, value);

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append('{');
        var i = 1;
        foreach (var (key, value) in Dictionary)
        {
            builder.Append(key).Append(':').Append(value);
            if (i < Dictionary.Count)
                builder.Append(',');
            i++;
        }
        builder.Append('}');

        return builder.ToString();
    }
}
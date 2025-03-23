using System.Security.Cryptography;

namespace codecrafters_bittorrent;

public sealed class BitTorrentParser
{
    private int _index = 0;
    private ReadOnlyMemory<byte> _bytes;

    public BitTorrentParser(ReadOnlyMemory<byte> bytes) => _bytes = bytes.ToArray();
    
    public IBitTorrentObject Parse(ReadOnlySpan<char> data)
    {
        if (char.IsDigit(data[_index]))
            return ParseString(data);
        if (data[_index] == 'i')
            return ParseNumber(data);
        if (data[_index] == 'l')
            return ParseList(data);
        if (data[_index] == 'd')
            return ParseDictionary(data);

        throw new Exception("Unreachable");
    }

    public IBitTorrentObject FreshParse(ReadOnlySpan<char> data)
    {
        _index = 0;
        return Parse(data);
    }
    
    private void AdvanceTill(char c, ReadOnlySpan<char> data)
    {
        while (data[_index] != c) _index++;
    }
    
    private BitTorrentNumber ParseNumber(ReadOnlySpan<char> data)
    {
        _index++;
        var s = _index;
        var isNegative = data[_index] == '-';
        if (isNegative)
        {
            s++;
            AdvanceTill('e', data);
            return new BitTorrentNumber(-long.Parse(data[s.._index]));
        }
        AdvanceTill('e', data);
        return new BitTorrentNumber(long.Parse(data[s.._index++]));
    }
    
    private BitTorrentString ParseString(ReadOnlySpan<char> data)
    {
        var s = _index;
        AdvanceTill(':', data);
        var len = int.Parse(data[s.._index]);
        var val = data[++_index..(_index+len)].ToString();
        _index += len;
        return new BitTorrentString(len, val);
    }

    private BitTorrentList ParseList(ReadOnlySpan<char> data)
    {
        _index++;
        List<IBitTorrentObject> values = [];
        while (data[_index] != 'e')
            values.Add(Parse(data));
        _index++;
        return new BitTorrentList(values);
    }
    
    private BitTorrentByteArray ParseByteArray(ReadOnlySpan<char> data)
    {
        Console.WriteLine($"xxx: {data.Length == _bytes.Length}, {data.Length}, {_bytes.Length}");
        var s = _index;
        AdvanceTill(':', data);
        var len = int.Parse(data[s.._index]);
        var val = _bytes[++_index..(_index+len)].ToArray();
        _index += len;
        return new BitTorrentByteArray(len, val);
    }
    
    private BitTorrentDictionary ParseDictionary(ReadOnlySpan<char> data)
    {
        _index++;
        BitTorrentDictionary dictionary = new();
        while (data[_index] != 'e')
        {
            var key = (BitTorrentString)Parse(data);
            var value = key.Value is "pieces" or "peers" ? ParseByteArray(data) : Parse(data);
            Console.WriteLine($"adding {key}:{value}. index:{_index},tl: {data.Length}");
            dictionary.Add(key, value);
        }
        _index++;
        return dictionary;
    }

    public static BitTorrentMetainfo ParseMetainfo(ReadOnlySpan<byte> bytes, ReadOnlySpan<char> stream, IBitTorrentObject value)
    {
        if (value is not BitTorrentDictionary dictionary)
            throw new Exception("WFT?");
        var info = (BitTorrentDictionary)dictionary.GetByString("info");
        var infoHashStart = stream.IndexOf(BitTorrentMetainfo.InfoHashMarker, StringComparison.Ordinal) 
            + BitTorrentMetainfo.InfoHashMarker.Length - 1;
        var chunk = bytes[infoHashStart..^1];
        var hash = SHA1.HashData(chunk);
        return new BitTorrentMetainfo
        {
            Announce = ((BitTorrentString)dictionary.GetByString("announce")).Value,
            CreatedBy = ((BitTorrentString)dictionary.GetByString("created by")).Value,
            Info = new BitTorrentMetinfoInfo()
            {
                Length = ((BitTorrentNumber)info.GetByString("length")).Value,
                Name = ((BitTorrentString)info.GetByString("name")).Value,
                PieceLength = ((BitTorrentNumber)info.GetByString("piece length")).Value,
                Pieces = ((BitTorrentByteArray)info.GetByString("pieces")).Value
            },
            Hash = Convert.ToHexString(hash).ToLower(),
            HashBytes = hash
        };
    }
}
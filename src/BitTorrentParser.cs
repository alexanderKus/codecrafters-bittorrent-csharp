namespace codecrafters_bittorrent;

public sealed class BitTorrentParser
{
    private int _index = 0;
    
    public IBitTorrentObject Parse(ReadOnlySpan<char> data)
    {
        if (char.IsDigit(data[_index]))
            return ParseString(data);
        if (data[_index] == 'i')
            return ParseNumber(data);
        if (data[_index] == 'l')
            return ParseList(data);

        throw new Exception("Unreachable");
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
}
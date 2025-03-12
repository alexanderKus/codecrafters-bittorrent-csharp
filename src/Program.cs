using System.Text.Json;
using codecrafters_bittorrent;

var (command, param) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    1 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    _ => (args[0], args[1])
};

if (command == "decode")
{
    BitTorrentParser parser = new();
    var result = parser.Parse(param);
    Console.WriteLine(result);
}
else if (command == "info")
{
    using var file = new StreamReader(param);
    var content = file.ReadToEnd();
    BitTorrentParser parser = new();
    var result = parser.Parse(content);
    Console.WriteLine(BitTorrentParser.ParseMetainfo(result));
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}

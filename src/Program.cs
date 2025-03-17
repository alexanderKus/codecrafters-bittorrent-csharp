using System.Text;
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
    using var file = new StreamReader(param, Encoding.ASCII);
    var content = file.ReadToEnd();
    var bytes = File.ReadAllBytes(param);
    BitTorrentParser parser = new();
    var result = parser.Parse(content);
    //Console.WriteLine(result);
    var hash = BitTorrentParser.ParseMetainfo(bytes, content, result).Hash;
    Console.WriteLine($"Info Hash: {hash}");
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
} 
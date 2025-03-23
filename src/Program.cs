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
    BitTorrentParser parser = new(Array.Empty<byte>());
    var result = parser.Parse(param);
    Console.WriteLine(result);
}
else if (command == "info")
{
    using var file = new StreamReader(param, Encoding.ASCII);
    var content = file.ReadToEnd();
    var bytes = File.ReadAllBytes(param);
    BitTorrentParser parser = new(bytes);
    var result = parser.Parse(content);
    //Console.WriteLine(result);
    var info = BitTorrentParser.ParseMetainfo(bytes, content, result);
    Console.WriteLine($"Tracker URL: {info.Announce}\nLength: {info.Info!.Length}\nInfo Hash: {info.Hash}");
    Console.WriteLine($"Piece Length: {info.Info.PieceLength}");
    var pieces = info!.Info!.Pieces!.Chunk(20);
    Console.WriteLine("Piece Hashes:");
    foreach (var piece in pieces)
    {
        Console.WriteLine(Convert.ToHexString(piece).ToLower());
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
} 
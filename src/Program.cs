using System.Buffers.Binary;
using System.Text;
using System.Web;
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
else if (command == "peers")
{
    using var file = new StreamReader(param, Encoding.ASCII);
    var content = file.ReadToEnd();
    var bytes = File.ReadAllBytes(param);
    BitTorrentParser parser = new(bytes);
    var result = parser.Parse(content);
    var info = BitTorrentParser.ParseMetainfo(bytes, content, result);
    using var httpClient = new HttpClient();
    var url = new Uri($"{info.Announce}?info_hash={HttpUtility.UrlEncode(info.HashBytes)}&peer_id=asdfghjklzxcvbbnmqwe&port=6881&uploaded=0&downloaded=0&left={info.Info!.Length}&compact=1");
    var response = await httpClient.GetAsync(url);
    var responseBytes = await response.Content.ReadAsByteArrayAsync();
    using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
    var responseString = reader.ReadToEnd();
    Console.WriteLine(responseString);
    parser = new BitTorrentParser(responseBytes);
    var parsedResponse = parser.Parse(responseString);
    var peers = ((BitTorrentByteArray)((BitTorrentDictionary)parsedResponse).GetByString("peers")).Value.Chunk(6);
    foreach (var peer in peers)
    {
        Console.WriteLine($"{peer[0]}.{peer[1]}.{peer[2]}.{peer[3]}:{BinaryPrimitives.ReadUInt16BigEndian(peer.AsSpan()[4..])}");
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
} 
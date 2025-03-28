using System.Buffers.Binary;
using System.Net.Sockets;
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
    Console.WriteLine(result);
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
    Console.WriteLine(Convert.ToHexString(responseBytes).ToLower());
    parser = new BitTorrentParser(responseBytes);
    var parsedResponse = parser.Parse(responseString);
    var peers = ((BitTorrentByteArray)((BitTorrentDictionary)parsedResponse).GetByString("peers")).Value.Chunk(6);
    foreach (var peer in peers)
    {
        Console.WriteLine($"{peer[0]}.{peer[1]}.{peer[2]}.{peer[3]}:{BinaryPrimitives.ReadUInt16BigEndian(peer.AsSpan()[4..])}");
    }
}
else if (command == "handshake")
{
    using var file = new StreamReader(param, Encoding.ASCII);
    var content = file.ReadToEnd();
    var bytes = File.ReadAllBytes(param);
    BitTorrentParser parser = new(bytes);
    var result = parser.Parse(content);
    var info = BitTorrentParser.ParseMetainfo(bytes, content, result);
    var data = Array.Empty<byte>()
            .Append((byte)19)
            .Concat("BitTorrent protocol"u8.ToArray())
            .Concat(new byte[8])
            .Concat(info.HashBytes)
            .Concat("00112233445566778899"u8.ToArray())
            .ToArray();
    var addr = args[2].Split(':');
    var tcpClient = new TcpClient(addr[0], int.Parse(addr[1]));
    var buffer = new byte[data.Length];
    using var stream = tcpClient.GetStream();
    stream.Write(data);
    stream.Flush();
    stream.Read(buffer);
    Console.WriteLine($"Peer ID: {Convert.ToHexString(buffer[48..]).ToLower()}");
}
else if (command == "download_piece")
{
    var path = args[2];
    var torrentFile = args[3];
    var pieceIndex = byte.Parse(args[4]);
    var outputFile = File.Create(path); 
    using var file = new StreamReader(torrentFile, Encoding.ASCII);
    var content = file.ReadToEnd();
    var bytes = File.ReadAllBytes(torrentFile);
    BitTorrentParser parser = new(bytes);
    var result = parser.Parse(content);
    var info = BitTorrentParser.ParseMetainfo(bytes, content, result);
    using var httpClient = new HttpClient();
    var url = new Uri($"{info.Announce}?info_hash={HttpUtility.UrlEncode(info.HashBytes)}&peer_id=asdfghjklzxcvbbnmqwe&port=6881&uploaded=0&downloaded=0&left={info.Info!.Length}&compact=1");
    var response = await httpClient.GetAsync(url);
    var responseBytes = await response.Content.ReadAsByteArrayAsync();
    using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
    var responseString = reader.ReadToEnd();
    parser = new BitTorrentParser(responseBytes);
    var parsedResponse = parser.Parse(responseString);
    var peers = ((BitTorrentByteArray)((BitTorrentDictionary)parsedResponse).GetByString("peers")).Value.Chunk(6);
    foreach (var peer in peers)
    {
        var data = Array.Empty<byte>()
            .Append((byte)19)
            .Concat("BitTorrent protocol"u8.ToArray())
            .Concat(new byte[8])
            .Concat(info.HashBytes)
            .Concat("00112233445566778899"u8.ToArray())
            .ToArray();
        var tcpClient = new TcpClient($"{peer[0]}.{peer[1]}.{peer[2]}.{peer[3]}", BinaryPrimitives.ReadUInt16BigEndian(peer.AsSpan()[4..]));
        var buffer = new byte[data.Length];
        await using var stream = tcpClient.GetStream();
        stream.Write(data);
        stream.Flush();
        stream.Read(buffer);
        Console.WriteLine($"Peer ID: {Convert.ToHexString(buffer[48..]).ToLower()}");
        var bitFieldBuffer = new byte[128];
        stream.Read(bitFieldBuffer);
        Console.WriteLine($"BitFieldBuffer: {Convert.ToHexString(bitFieldBuffer).ToLower()}");
        var interestedBuffer = Array.Empty<byte>()
            .Concat(new byte [] {0,0,0,1})
            .Append((byte)BitTorrentMessageType.Interested)
            .ToArray();
        stream.Write(interestedBuffer);
        var unchokeBuffer = new byte[128];
        stream.Read(unchokeBuffer);
        Console.WriteLine($"UnchokeBuffer: {Convert.ToHexString(unchokeBuffer).ToLower()}");
        for (var i = 0; i < 16; i++)
        {
            var len = (i == 15 ? info!.Info!.Length % info!.Info!.PieceLength : info!.Info!.PieceLength) ?? throw new Exception("Cannot calculate length of a piece");
            var requestBuffer = Array.Empty<byte>()
                .Concat(new byte [] {0,0,0,17})
                .Append((byte)BitTorrentMessageType.Request)
                .Append((byte)0x0).Append((byte)0x0).Append((byte)0x0).Append(pieceIndex)
                .Concat(BitConverter.GetBytes(i*(2^14)))
                .Concat(BitConverter.GetBytes((int)len))
                .ToArray();
            Console.WriteLine($"RequestBuffer id:{i}: {Convert.ToHexString(requestBuffer).ToLower()}");
            stream.Write(requestBuffer);
            
            var pieceBuffer = new byte[1024];
            stream.Read(pieceBuffer);
            Console.WriteLine($"PieceBuffer: {Convert.ToHexString(pieceBuffer).ToLower()}");
        }
        
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
} 
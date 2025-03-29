using System.Buffers.Binary;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
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
    // NOTE: Just terrible :(
    var path = args[2];
    var torrentFile = args[3];
    var pieceIndex = int.Parse(args[4]);
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
    var peers = ((BitTorrentByteArray)((BitTorrentDictionary)parsedResponse).GetByString("peers")).Value.Chunk(6).ToArray();
    var hashes = info!.Info!.Pieces!.Chunk(20).Select(x => Convert.ToHexString(x).ToLower()).ToArray();
    Console.WriteLine(string.Join('\n', hashes));
    List<byte> pieces = [];
    for (var index = 2; index < peers.Length; index++)
    {
        Console.WriteLine("----------------------------------------------------");
        var peer = peers[index];
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
        //Console.WriteLine($"BitFieldBuffer: {Convert.ToHexString(bitFieldBuffer).ToLower()}");
        var interestedBuffer = Array.Empty<byte>()
            .Concat(new byte [] {0,0,0,1})
            .Append((byte)BitTorrentMessageType.Interested)
            .ToArray();
        stream.Write(interestedBuffer);
        stream.Flush();
        var unchokeBuffer = new byte[128];
        stream.Read(unchokeBuffer);
        //Console.WriteLine($"UnchokeBuffer: {Convert.ToHexString(unchokeBuffer).ToLower()}");
        List<byte> piece = [];
        var fileLength = (int)info!.Info!.Length;
        var pieceLength = (int)info!.Info!.PieceLength;
        var endOfBlock = (pieceIndex + 1) * info!.Info!.PieceLength;
        var actualPieceLength = endOfBlock > fileLength ? fileLength - (pieceIndex * pieceLength) : pieceLength;
        var blockLength = 16 * 1024;
        for (var i = 0; actualPieceLength > 0; i++)
        {
            var size = actualPieceLength < blockLength ? actualPieceLength : blockLength;
            var begin = BitConverter.GetBytes(i * blockLength);
            var length = BitConverter.GetBytes(size);
            var correctPieceIndex = BitConverter.GetBytes(pieceIndex);
            Array.Reverse(begin);
            Array.Reverse(length);
            Array.Reverse(correctPieceIndex);
            var requestBuffer = Array.Empty<byte>()
                .Concat(new byte[] {0,0,0,13})
                .Append((byte)BitTorrentMessageType.Request)
                .Concat(correctPieceIndex)
                .Concat(begin)
                .Concat(length)
                .ToArray();
            Console.WriteLine($"RequestBuffer id:{i}: {Convert.ToHexString(requestBuffer).ToLower()}");
            actualPieceLength -= blockLength;
            stream.Write(requestBuffer);
            
            var pieceBuffer = new byte[blockLength + 13];
            await stream.ReadExactlyAsync(pieceBuffer, 0, size + 13);
            var responseBlockLength = BitConverter.ToInt32(pieceBuffer[..4].Reverse().ToArray()) - 9;
            piece.AddRange(pieceBuffer[13..(13+responseBlockLength)].ToArray());
        }
        var pieceHash = SHA1.HashData(piece.ToArray());
        Console.WriteLine($"GOT: {Convert.ToHexString(pieceHash).ToLower()} ? {hashes[index]}");
        Console.WriteLine($"Piece Len: {piece.Count}");
        pieces.AddRange(piece);
        Console.WriteLine("----------------------------------------------------");
        break;
    }
    File.WriteAllBytes(path, pieces.ToArray());
}
else if (command == "download")
{
    var path = args[2];
    var torrentFile = args[3];
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
    var peers = ((BitTorrentByteArray)((BitTorrentDictionary)parsedResponse).GetByString("peers")).Value.Chunk(6).ToArray();
    var hashes = info!.Info!.Pieces!.Chunk(20).Select(x => Convert.ToHexString(x).ToLower()).ToArray();
    List<byte> pieces = [];
    var index = 0;
    Console.WriteLine("----------------------------------------------------");
    var peer = peers[index];
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
    //Console.WriteLine($"BitFieldBuffer: {Convert.ToHexString(bitFieldBuffer).ToLower()}");
    var interestedBuffer = Array.Empty<byte>()
        .Concat(new byte [] {0,0,0,1})
        .Append((byte)BitTorrentMessageType.Interested)
        .ToArray();
    stream.Write(interestedBuffer);
    stream.Flush();
    var unchokeBuffer = new byte[128];
    stream.Read(unchokeBuffer);
    //Console.WriteLine($"UnchokeBuffer: {Convert.ToHexString(unchokeBuffer).ToLower()}");
    List<byte> piece = [];
    var m = info.Info.Pieces.Length / 20;
    Console.WriteLine($"Max: {m}");
    for (int j = 0; j < m; j++)
    {
        var fileLength = (int)info!.Info!.Length;
        var pieceLength = (int)info!.Info!.PieceLength;
        var endOfBlock = (j + 1) * info!.Info!.PieceLength;
        var actualPieceLength = endOfBlock > fileLength ? fileLength - (j * pieceLength) : pieceLength;
        var blockLength = 16 * 1024;
        for (var i = 0; actualPieceLength > 0; i++)
        {
            var size = actualPieceLength < blockLength ? actualPieceLength : blockLength;
            var begin = BitConverter.GetBytes(i * blockLength);
            var length = BitConverter.GetBytes(size);
            var correctPieceIndex = BitConverter.GetBytes(j);
            Array.Reverse(begin);
            Array.Reverse(length);
            Array.Reverse(correctPieceIndex);
            var requestBuffer = Array.Empty<byte>()
                .Concat(new byte[] {0,0,0,13})
                .Append((byte)BitTorrentMessageType.Request)
                .Concat(correctPieceIndex)
                .Concat(begin)
                .Concat(length)
                .ToArray();
            Console.WriteLine($"RequestBuffer id:{i}: {Convert.ToHexString(requestBuffer).ToLower()}");
            actualPieceLength -= blockLength;
            stream.Write(requestBuffer);
        
            var pieceBuffer = new byte[blockLength + 13];
            await stream.ReadExactlyAsync(pieceBuffer, 0, size + 13);
            var responseBlockLength = BitConverter.ToInt32(pieceBuffer[..4].Reverse().ToArray()) - 9;
            piece.AddRange(pieceBuffer[13..(13+responseBlockLength)].ToArray());
        }
        var pieceHash = SHA1.HashData(piece.ToArray());
        if (Convert.ToHexString(pieceHash).ToLower() == hashes[index]) break;
        Console.WriteLine($"GOT: {Convert.ToHexString(pieceHash).ToLower()} ? {hashes[index]}");
        Console.WriteLine($"Piece Len: {piece.Count}");
        pieces.AddRange(piece);
        Console.WriteLine("----------------------------------------------------");
    }
    File.WriteAllBytes(path, pieces.ToArray());
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
} 
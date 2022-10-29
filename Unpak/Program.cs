// https://github.com/chromium/chromium/blob/master/ui/base/resource/data_pack.cc
// https://stackoverflow.com/questions/10633357/how-to-unpack-resources-pak-from-google-chrome
using System.Buffers;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: <input file>");
    return 1;
}

Console.WriteLine($"Reading: {args[0]}");
using var stream = File.OpenRead(args[0]);
using var br = new BinaryReader(stream);

var version = br.ReadUInt32();
var encoding = br.ReadByte();
stream.Seek(3, SeekOrigin.Current);
var resourceCount = br.ReadUInt16();
var aliasCount = br.ReadUInt16();

Span<Entry> entries = stackalloc Entry[resourceCount + 1];
Span<Alias> aliases = stackalloc Alias[aliasCount];

Console.WriteLine(new {version, encoding, resourceCount, aliasCount});


for (int i = 0; i < resourceCount + 1; i++)
{
    var resourceId = br.ReadUInt16();
    var fileOffset = br.ReadUInt32();
    entries[i] = new Entry(resourceId, fileOffset);
    Console.WriteLine(new {resourceId, fileOffset});
}

for (int i = 0; i < aliasCount; i++)
{
    var resourceId = br.ReadUInt16();
    var entryIndex = br.ReadUInt16();
    aliases[i] = new Alias(resourceId, entryIndex);
    Console.WriteLine(new {resourceId, entryIndex});
}

const string outputDirectory = "resources";
Directory.CreateDirectory(outputDirectory);

for (int i = 0; i < resourceCount; i++)
{
    stream.Seek(entries[i].FileOffset, SeekOrigin.Begin);
    var length = (int)(entries[i + 1].FileOffset - entries[i].FileOffset);
    var buff = ArrayPool<byte>.Shared.Rent(length);
    _ = stream.Read(buff, 0, length);

    using var file = File.Create(Path.Combine(outputDirectory, $"f_{entries[i].ResourceId:x4}"));
    {
        file.Write(buff, 0, length);
    }
}

return 0;

record struct Entry(ushort ResourceId, uint FileOffset);
record struct Alias(ushort ResourceId, ushort EntryIndex);

using Microsoft.AspNetCore.Components.Forms;
using SharpNBT;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Net;
using System.Numerics;

namespace MinecraftLayoutEditor.Schematics;

public record BlockPos(int X, int Y, int Z);
public record LevelDatInfo(int SpawnX, int SpawnZ, string LevelName);

public static class WorldImporter
{
    private const int SECTOR_BYTES = 4096;
    private const int CHUNK_HEADER_SIZE = 5;

    public static async Task<(List<BlockPos> blocks, Vector2 spawn, string worldName)> ImportWorld(IBrowserFile[] files)
    {
        // Debug: Alle hochgeladenen Dateien anzeigen
        Console.WriteLine($"Total files uploaded: {files.Length}");
        foreach (var file in files)
        {
            Console.WriteLine($"File: {file.Name}, Size: {file.Size}, ContentType: {file.ContentType}");
        }

        var mcaFiles = files.Where(f => f.Name.EndsWith(".mca")).ToArray();
        var levelDatFile = files.FirstOrDefault(f => f.Name == "level.dat");

        Console.WriteLine($"After filtering - MCA files: {mcaFiles.Length}");
        foreach (var mcaFile in mcaFiles)
        {
            Console.WriteLine($"MCA file found: {mcaFile.Name}");
        }

        var blocks = new List<BlockPos>();
        Vector2 spawn = Vector2.Zero;
        string worldName = "Unknown";

        if (levelDatFile != null)
        {
            try
            {
                Console.WriteLine("Starting level.dat parsing...");
                await using var stream = levelDatFile.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                var levelData = await ParseLevelDat(stream);
                spawn = new Vector2(levelData.SpawnX, levelData.SpawnZ);
                worldName = levelData.LevelName;
                Console.WriteLine($"Successfully parsed level.dat: spawn=({spawn.X}, {spawn.Y}), name={worldName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse level.dat: {ex.Message}");
                throw new Exception($"level.dat: {ex.Message}");
            }
        }

        Console.WriteLine($"Starting to parse {mcaFiles.Length} region files...");

        if (mcaFiles.Length == 0)
        {
            Console.WriteLine("No .mca files found, returning empty block list");
            return (blocks, spawn, worldName);
        }

        foreach (var mcaFile in mcaFiles)
        {
            try
            {
                Console.WriteLine($"Parsing {mcaFile.Name}...");

                // Parse region coordinates from filename
                var (regionX, regionZ) = ParseRegionCoordinates(mcaFile.Name);
                Console.WriteLine($"Region coordinates: ({regionX}, {regionZ})");

                await using var stream = mcaFile.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
                var regionBlocks = await ParseRegionFile(stream, regionX, regionZ);
                blocks.AddRange(regionBlocks);
                Console.WriteLine($"Parsed {mcaFile.Name}: {regionBlocks.Count} blocks found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse {mcaFile.Name}: {ex.Message}");
            }
        }

        return (blocks, spawn, worldName);

        Console.WriteLine($"Total blocks imported: {blocks.Count}");
        return (blocks, spawn, worldName);
    }

    private static async Task<LevelDatInfo> ParseLevelDat(Stream stream)
    {
        try
        {
            Console.WriteLine($"Stream length: {stream.Length}");

            await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);

            // Decompress to memory first
            using var decompressedStream = new MemoryStream();
            await gzipStream.CopyToAsync(decompressedStream);
            Console.WriteLine($"Decompressed size: {decompressedStream.Length}");
            decompressedStream.Position = 0;

            // Read from memory stream
            using var reader = new TagReader(decompressedStream, FormatOptions.Java);

            // Try reading as named tag first (level.dat usually has a root name)
            CompoundTag root;
            try
            {
                root = reader.ReadTag<CompoundTag>(named: true);
                Console.WriteLine($"Successfully read named tag, count: {root.Count()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read as named tag: {ex.Message}");
                // Reset stream and try as unnamed tag
                decompressedStream.Position = 0;
                using var reader2 = new TagReader(decompressedStream, FormatOptions.Java);
                root = reader2.ReadTag<CompoundTag>(named: false);
                Console.WriteLine($"Successfully read unnamed tag, count: {root.Count()}");
            }

            // Debug: print available tag names
            var tagNames = new List<string>();
            foreach (var tag in root)
            {
                tagNames.Add(tag.Name ?? "<unnamed>");
            }
            Console.WriteLine($"Root tag names: {string.Join(", ", tagNames)}");

            // Find the Data tag
            var dataTag = root.Find("Data", deep: false);
            if (dataTag is not CompoundTag data)
            {
                throw new Exception($"Could not find Data tag. Available tags: {string.Join(", ", tagNames)}");
            }

            Console.WriteLine("Found Data tag");

            // Debug: print Data tag contents
            var dataTagNames = new List<string>();
            foreach (var tag in data)
            {
                dataTagNames.Add(tag.Name ?? "<unnamed>");
            }
            Console.WriteLine($"Data tag names: {string.Join(", ", dataTagNames)}");

            // Get spawn coordinates - try modern format first (spawn/pos), then legacy format (SpawnX/SpawnZ)
            int spawnX = 0;
            int spawnZ = 0;

            // Modern format: Data/spawn/pos
            var spawnTag = data.Find("spawn", deep: false) as CompoundTag;
            if (spawnTag != null)
            {
                Console.WriteLine("Found modern spawn tag");
                var posTag = spawnTag.Find("pos", deep: false) as ListTag;
                if (posTag != null && posTag.Count >= 3)
                {
                    // pos is typically a list of 3 doubles [X, Y, Z]
                    var coords = posTag.Cast<DoubleTag>().ToArray();
                    if (coords.Length >= 3)
                    {
                        spawnX = (int)coords[0].Value;
                        spawnZ = (int)coords[2].Value; // Z is the third coordinate
                        Console.WriteLine($"Modern spawn format: X={spawnX}, Z={spawnZ}");
                    }
                }
            }
            else
            {
                // Legacy format: Data/SpawnX, Data/SpawnZ
                Console.WriteLine("Trying legacy spawn format");
                var spawnXTag = data.Find("SpawnX", deep: false) as IntTag;
                var spawnZTag = data.Find("SpawnZ", deep: false) as IntTag;

                if (spawnXTag != null)
                {
                    spawnX = spawnXTag.Value;
                    Console.WriteLine($"Found legacy SpawnX: {spawnX}");
                }
                if (spawnZTag != null)
                {
                    spawnZ = spawnZTag.Value;
                    Console.WriteLine($"Found legacy SpawnZ: {spawnZ}");
                }
            }

            // Get world name
            string levelName = "Unknown";
            var levelNameTag = data.Find("LevelName", deep: false) as StringTag;
            if (levelNameTag != null)
            {
                levelName = levelNameTag.Value ?? "Unknown";
                Console.WriteLine($"Found LevelName: {levelName}");
            }

            // Get world version
            var versionTag = data.Find("version", deep: false) as IntTag;
            var dataVersionTag = data.Find("DataVersion", deep: false) as IntTag;
            Console.WriteLine($"World version: {versionTag?.Value}, DataVersion: {dataVersionTag?.Value}");

            return new LevelDatInfo(spawnX, spawnZ, levelName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Detailed error: {ex}");
            throw new Exception($"Error parsing level.dat: {ex.Message}");
        }
    }

    private static async Task<List<BlockPos>> ParseRegionFile(Stream stream, int regionX, int regionZ)
    {
        var blocks = new List<BlockPos>();

        try
        {
            Console.WriteLine($"Region file stream length: {stream.Length}");

            // Load entire .mca file into memory first
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var buffer = new byte[4];

            // Read 8KB header: 4KB offsets + 4KB timestamps
            var offsets = new int[1024];
            var timestamps = new int[1024];

            for (int i = 0; i < 1024; i++)
            {
                await memoryStream.ReadExactlyAsync(buffer, 0, 4);
                offsets[i] = BinaryPrimitives.ReadInt32BigEndian(buffer);
            }
            for (int i = 0; i < 1024; i++)
            {
                await memoryStream.ReadExactlyAsync(buffer, 0, 4);
                timestamps[i] = BinaryPrimitives.ReadInt32BigEndian(buffer);
            }

            int validChunks = offsets.Count(offset => offset != 0);
            Console.WriteLine($"Found {validChunks} valid chunks in region file");

            if (validChunks == 0) return blocks;

            // Verarbeite ALLE Chunks (nicht nur 3 für Debug)
            for (int i = 0; i < 1024; i++)
            {
                int offset = offsets[i];
                if (offset == 0) continue;

                try
                {
                    int sectorStart = offset >> 8;
                    int sectorCount = offset & 0xFF;

                    long byteOffset = (long)sectorStart * SECTOR_BYTES;
                    if (byteOffset >= memoryStream.Length) continue;

                    memoryStream.Seek(byteOffset, SeekOrigin.Begin);

                    await memoryStream.ReadExactlyAsync(buffer, 0, 4);
                    int length = BinaryPrimitives.ReadInt32BigEndian(buffer);

                    if (length <= 1 || length > sectorCount * SECTOR_BYTES) continue;

                    var versionBuffer = new byte[1];
                    await memoryStream.ReadExactlyAsync(versionBuffer, 0, 1);
                    byte version = versionBuffer[0];

                    var chunkData = new byte[length - 1];
                    await memoryStream.ReadExactlyAsync(chunkData, 0, length - 1);

                    var chunkNbt = await DecompressChunk(chunkData, version);
                    if (chunkNbt == null) continue;

                    var section = GetSectionAtY0(chunkNbt);
                    if (section == null) continue;

                    // KORRIGIERTE Chunk - Koordinaten - Berechnung:
                    int localChunkX = i % 32;
                    int localChunkZ = i / 32;

                    // Berechne globale Chunk-Koordinaten basierend auf Region
                    int globalChunkX = regionX * 32 + localChunkX;
                    int globalChunkZ = regionZ * 32 + localChunkZ;
                    int blocksBefore = blocks.Count;
                    ExtractBlocksFromSection(section, globalChunkX, globalChunkZ, blocks);
                    int blocksAdded = blocks.Count - blocksBefore;

                    if (blocksAdded > 0)
                    {
                        Console.WriteLine($"Chunk {i} ({localChunkX}, {localChunkZ}): extracted {blocksAdded} blocks");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing chunk {i}: {ex.Message}");
                }
            }

            Console.WriteLine($"Region file processing complete: {blocks.Count} total blocks");
            return blocks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ParseRegionFile: {ex.Message}");
            throw;
        }
    }

    private static async Task<CompoundTag?> DecompressChunk(byte[] data, byte version)
    {
        try
        {
            Console.WriteLine($"Decompressing {data.Length} bytes with version {version}");

            await using var inputStream = new MemoryStream(data);

            Stream decompressStream;
            switch (version)
            {
                case 1: // GZip
                    decompressStream = new GZipStream(inputStream, CompressionMode.Decompress);
                    break;
                case 2: // ZLib
                    try
                    {
                        decompressStream = new ZLibStream(inputStream, CompressionMode.Decompress);
                    }
                    catch (NotSupportedException)
                    {
                        inputStream.Position = 2; // Skip ZLib header
                        decompressStream = new DeflateStream(inputStream, CompressionMode.Decompress);
                    }
                    break;
                case 3: // Uncompressed
                    decompressStream = inputStream;
                    break;
                default:
                    throw new NotSupportedException($"Compression version {version} not supported");
            }

            await using (decompressStream)
            {
                // Decompress all data to memory first
                using var decompressedStream = new MemoryStream();
                await decompressStream.CopyToAsync(decompressedStream);
                Console.WriteLine($"Decompressed to {decompressedStream.Length} bytes");

                if (decompressedStream.Length == 0)
                {
                    Console.WriteLine("Decompressed stream is empty!");
                    return null;
                }

                decompressedStream.Position = 0;

                // Debug: Show first few bytes of decompressed data
                var previewBytes = new byte[Math.Min(32, decompressedStream.Length)];
                decompressedStream.Read(previewBytes, 0, previewBytes.Length);
                Console.WriteLine($"First bytes: {string.Join(" ", previewBytes.Select(b => b.ToString("X2")))}");
                decompressedStream.Position = 0;

                // Try reading as NAMED compound tag first (this is the key change!)
                using var reader = new TagReader(decompressedStream, FormatOptions.Java);

                CompoundTag? result = null;
                try
                {
                    // For 1.8.9 chunks, try reading as named compound first
                    result = reader.ReadTag<CompoundTag>(named: true);
                    Console.WriteLine($"Read NAMED compound tag: '{result.Name}' with {result.Count()} children");

                    if (result.Count() > 0)
                    {
                        var tagNames = new List<string>();
                        foreach (var tag in result)
                        {
                            tagNames.Add($"{tag.GetType().Name}:{tag.Name ?? "<unnamed>"}");
                        }
                        Console.WriteLine($"Root tag children: {string.Join(", ", tagNames)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read as named compound: {ex.Message}");

                    // Reset and try unnamed
                    decompressedStream.Position = 0;
                    using var reader2 = new TagReader(decompressedStream, FormatOptions.Java);
                    try
                    {
                        result = reader2.ReadTag<CompoundTag>(named: false);
                        Console.WriteLine($"Read unnamed compound tag with {result.Count()} children");
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"Failed to read as unnamed compound: {ex2.Message}");

                        // Last resort: try to manually parse the NBT structure
                        Console.WriteLine("Attempting manual NBT parsing...");
                        decompressedStream.Position = 0;
                        return TryManualNbtParse(decompressedStream);
                    }
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decompressing chunk: {ex.Message}");
            return null;
        }
    }

    private static CompoundTag? GetSectionAtY0(CompoundTag chunk)
    {
        // Für Minecraft 1.8.9: Die Daten sind in chunk["Level"]
        var levelTag = chunk.Find("Level", deep: false) as CompoundTag;
        if (levelTag == null)
        {
            Console.WriteLine("No 'Level' tag found in chunk");
            return null;
        }

        Console.WriteLine("Found Level tag in chunk");

        var sectionsTag = levelTag.Find("Sections", deep: false) as ListTag;
        if (sectionsTag == null)
        {
            Console.WriteLine("No 'Sections' tag found in Level");
            // Debug: Was ist in Level drin?
            var levelTagNames = new List<string>();
            foreach (var tag in levelTag)
            {
                levelTagNames.Add(tag.Name ?? "<unnamed>");
            }
            Console.WriteLine($"Level contains: {string.Join(", ", levelTagNames)}");
            return null;
        }

        Console.WriteLine($"Found {sectionsTag.Count} sections in chunk Level");

        // Debug: Alle verfügbaren Y-Werte anzeigen
        var availableYLevels = new List<int>();
        foreach (var sec in sectionsTag)
        {
            if (sec is CompoundTag section)
            {
                var yTag = section.Find("Y", deep: false) as ByteTag;
                if (yTag != null)
                {
                    availableYLevels.Add(yTag.Value);
                }
            }
        }

        if (availableYLevels.Count > 0)
        {
            Console.WriteLine($"Available Y levels in chunk: {string.Join(", ", availableYLevels.OrderBy(y => y))}");
        }

        // Für 1.8.9: Y=0 ist der Boden, also Blöcke Y=0-15
        foreach (var sec in sectionsTag)
        {
            if (sec is CompoundTag section)
            {
                var yTag = section.Find("Y", deep: false) as ByteTag;
                if (yTag?.Value == 0)
                {
                    Console.WriteLine($"Using section at Y={yTag.Value}");
                    return section;
                }
            }
        }

        // Falls Y=0 nicht existiert, nimm das niedrigste verfügbare Y-Level
        if (availableYLevels.Count > 0)
        {
            int lowestY = availableYLevels.Min();
            foreach (var sec in sectionsTag)
            {
                if (sec is CompoundTag section)
                {
                    var yTag = section.Find("Y", deep: false) as ByteTag;
                    if (yTag?.Value == lowestY)
                    {
                        Console.WriteLine($"Using lowest available section at Y={lowestY}");
                        return section;
                    }
                }
            }
        }

        return null;
    }

    private static void ExtractBlocksFromSection(CompoundTag section, int chunkX, int chunkZ, List<BlockPos> blocks)
    {
        var blocksTag = section.Find("Blocks", deep: false) as ByteArrayTag;
        var yTag = section.Find("Y", deep: false) as ByteTag;

        if (blocksTag == null || yTag == null) return;

        var blockIds = blocksTag.ToArray();
        int sectionY = yTag.Value;

        // Nur Y=0 Sections verarbeiten
        if (sectionY != 0) return;

        int initialBlockCount = blocks.Count;

        // Nur localY = 0 (absolutes Y = 0)
        for (int localZ = 0; localZ < 16; localZ++)
        {
            for (int localX = 0; localX < 16; localX++)
            {
                int index = 0 * 256 + localZ * 16 + localX; // localY = 0
                if (index >= blockIds.Length) continue;

                byte blockId = blockIds[index];
                if (blockId == 0) continue; // Skip air

                int worldX = chunkX * 16 + localX;
                int worldZ = chunkZ * 16 + localZ;

                blocks.Add(new BlockPos(worldX, 0, worldZ)); // Y bleibt 0
            }
        }

        int blocksAdded = blocks.Count - initialBlockCount;
        if (blocksAdded > 0)
        {
            Console.WriteLine($"Section Y={sectionY}: Added {blocksAdded} blocks at Y=0");
        }
    }

    private static (int regionX, int regionZ) ParseRegionCoordinates(string filename)
    {
        // Parse "r.X.Z.mca" format
        var parts = filename.Replace(".mca", "").Split('.');
        if (parts.Length >= 3 && parts[0] == "r")
        {
            if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int z))
            {
                return (x, z);
            }
        }
        return (0, 0); // fallback
    }

    private static CompoundTag? TryManualNbtParse(MemoryStream stream)
    {
        try
        {
            using var reader = new BinaryReader(stream);

            // Read first tag type
            byte tagType = reader.ReadByte();
            Console.WriteLine($"Root tag type: {tagType} (should be 10 for compound)");

            if (tagType != 10) // TAG_Compound
            {
                Console.WriteLine("Root is not a compound tag!");
                return null;
            }

            // Read root name length (should be 0 for chunk root)
            short nameLength = IPAddress.NetworkToHostOrder(reader.ReadInt16());
            Console.WriteLine($"Root name length: {nameLength}");

            if (nameLength > 0)
            {
                string rootName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                Console.WriteLine($"Root name: '{rootName}'");
            }

            // Now try to manually find the "Level" tag
            while (stream.Position < stream.Length - 1)
            {
                byte nextTagType = reader.ReadByte();
                if (nextTagType == 0) // TAG_End
                {
                    Console.WriteLine("Hit TAG_End");
                    break;
                }

                short nextNameLength = IPAddress.NetworkToHostOrder(reader.ReadInt16());
                if (nextNameLength <= 0 || nextNameLength > 100) break;

                string tagName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nextNameLength));
                Console.WriteLine($"Found tag: type={nextTagType}, name='{tagName}'");

                if (tagName == "Level" && nextTagType == 10) // TAG_Compound
                {
                    Console.WriteLine("Found Level compound tag! Creating CompoundTag manually...");
                    // We found the Level tag - for now just return a dummy result to confirm we can find it
                    var dummyCompound = new CompoundTag("Level");
                    return dummyCompound;
                }

                // Skip this tag's content (this is a simplified skip)
                break; // For now, just break after finding the first tag
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Manual NBT parse failed: {ex.Message}");
            return null;
        }
    }
}
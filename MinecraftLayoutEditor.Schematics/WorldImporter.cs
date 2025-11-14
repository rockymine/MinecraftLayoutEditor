using Microsoft.AspNetCore.Components.Forms;
using MinecraftLayoutEditor.XML;
using SharpNBT;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;

namespace MinecraftLayoutEditor.Schematics;

public record BlockPos(int X, int Y, int Z);
public record LevelDatInfo(int SpawnX, int SpawnZ, string LevelName);

/// <summary>
/// Minecraft World Importer - Key Resources and References
/// 
/// CRITICAL DOCUMENTATION:
/// 
/// 1. Minecraft Wiki (NBT Format):
///    - https://minecraft.wiki/w/NBT_format
///    - https://minecraft.wiki/w/Level.dat_format  
///    - Essential for understanding tag structure and coordinate systems
/// 
/// 2. SharpNBT Library:
///    - https://github.com/ForeverZer0/SharpNBT
///    - Key insight: Chunk NBT must be read with named: true for 1.8.9
///    - CompoundTag.Find() method for navigating NBT hierarchy
/// 
/// 3. Region File Format:
///    - https://minecraft.wiki/w/Region_file_format
///    - Critical for understanding .mca header structure and chunk indexing
///    - Coordinate transformation: regionX*32 + localChunkX = globalChunkX
/// 
/// 4. Blazor WebAssembly Limitations:
///    - BrowserFileStream.Seek() not supported - must load to MemoryStream first
///    - ZLibStream may need fallback to DeflateStream in WASM environment
/// 
/// 5. Minecraft Version Differences:
///    - 1.8.9: Uses "Blocks" ByteArray with simple block IDs
///    - 1.13+: Uses "Palette" + "BlockStates" with complex block states
///    - 1.8.9: SpawnX/SpawnZ in level.dat vs modern "spawn/pos" format
/// 
/// 6. AutoPruner Tool Context:
///    - https://github.com/ocn/autopruner (explains empty sections in minigame maps)
///    - Many chunks have Level tag but no Sections due to optimization
/// 
/// DEBUGGING INSIGHTS:
/// - NBT hex signatures: 0A 00 00 0A 00 05 4C 65 76 65 6C = Root->Level structure
/// - Section Y=0 contains blocks at world Y coordinates 0-15
/// - Block index formula: localY * 256 + localZ * 16 + localX
/// - Empty chunks still exist in .mca but contain no block data
/// </summary>
public static class WorldImporter
{
    private const int SECTOR_BYTES = 4096;

    public static async Task<(List<BlockPos> blocks, Vector2 spawn, string worldName, MapElement? map)> ImportWorld(IBrowserFile[] files)
    {
        var mcaFiles = files.Where(f => f.Name.EndsWith(".mca")).ToArray();
        var levelDatFile = files.FirstOrDefault(f => f.Name == "level.dat");
        var xmlFile = files.FirstOrDefault(f => f.Name == "map.xml");

        var blocks = new List<BlockPos>();
        Vector2 spawn = Vector2.Zero;
        string worldName = "Unknown";
        MapElement? map = null;

        // Parse level.dat
        if (levelDatFile != null)
        {
            try
            {
                await using var stream = levelDatFile.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                var levelData = await ParseLevelDat(stream);
                spawn = new Vector2(levelData.SpawnX, levelData.SpawnZ);
                worldName = levelData.LevelName;
            }
            catch (Exception ex)
            {
                throw new Exception($"level.dat: {ex.Message}");
            }
        }

        // Parse .xml file
        if (xmlFile != null)
        {
            Console.WriteLine("xml file not null");
            try
            {
                await using var stream = xmlFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                map = await XMLImporter.LoadFromStream(stream);
                Console.WriteLine($"Loaded XML map: {map.Name} (proto {map.Proto})");
            }
            catch (Exception ex)
            {
                throw new Exception($"map.xml: {ex.Message}");
            }
        }

        // Parse .mca files
        if (mcaFiles.Length == 0)
            return (blocks, spawn, worldName, map);

        foreach (var mcaFile in mcaFiles)
        {
            try
            {
                var (regionX, regionZ) = ParseRegionCoordinates(mcaFile.Name);

                await using var stream = mcaFile.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
                var regionBlocks = await ParseRegionFile(stream, regionX, regionZ);
                blocks.AddRange(regionBlocks);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse {mcaFile.Name}: {ex.Message}");
            }
        }

        return (blocks, spawn, worldName, map);
    }

    /// <summary>
    /// Parses Minecraft level.dat file to extract world metadata (spawn point, world name).
    /// 
    /// File Structure:
    /// ┌─────────────────────────────────────────────────────────────────┐
    /// │ level.dat (GZip-compressed NBT file)                            │
    /// │ ├── Root Compound Tag (named, usually empty name)               │
    /// │     └── "Data" Compound Tag                                     │
    /// │         ├── SpawnX (Int): World spawn X coordinate              │
    /// │         ├── SpawnY (Int): World spawn Y coordinate              │
    /// │         ├── SpawnZ (Int): World spawn Z coordinate              │
    /// │         ├── LevelName (String): World display name              │
    /// │         ├── version (Int): NBT format version (19133 for 1.8.9) │
    /// │         ├── GameType (Int): Default game mode                   │
    /// │         ├── Time (Long): World age in ticks                     │
    /// │         ├── LastPlayed (Long): Last save timestamp              │
    /// │         └── ... (many other world settings)                     │
    /// └─────────────────────────────────────────────────────────────────┘
    /// 
    /// Note: Modern versions (1.13+) use "spawn/pos" instead of SpawnX/SpawnZ
    /// </summary>
    private static async Task<LevelDatInfo> ParseLevelDat(Stream stream)
    {
        try
        {
            // Decompress GZip to memory
            await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            await gzipStream.CopyToAsync(decompressedStream);
            decompressedStream.Position = 0;

            // Read NBT data
            using var reader = new TagReader(decompressedStream, FormatOptions.Java);
            var root = reader.ReadTag<CompoundTag>(named: true);

            // Find Data tag
            var data = root.Find("Data", deep: false) as CompoundTag
                ?? throw new Exception("Missing Data tag in level.dat");

            // Extract spawn coordinates (legacy 1.8.9 format)
            int spawnX = (data.Find("SpawnX", deep: false) as IntTag)?.Value ?? 0;
            int spawnZ = (data.Find("SpawnZ", deep: false) as IntTag)?.Value ?? 0;

            // Extract level name
            string levelName = (data.Find("LevelName", deep: false) as StringTag)?.Value ?? "Unknown";

            return new LevelDatInfo(spawnX, spawnZ, levelName);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error parsing level.dat: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses Minecraft .mca (region) file to extract block positions at Y=0 (ground level).
    /// 
    /// File Structure:
    /// ┌─────────────────────────────────────────────────────────────────┐
    /// │ r.X.Z.mca (Region file covering 512×512 blocks, 32×32 chunks)   │
    /// │ ├── Header (8KB total)                                          │
    /// │ │   ├── Chunk Offsets (4KB): [1024 × 4 bytes] sector locations  │
    /// │ │   └── Timestamps (4KB): [1024 × 4 bytes] last modified times  │
    /// │ └── Chunk Data (variable, ZLib/GZip compressed)                 │
    /// │     └── Chunk (16×16 blocks horizontally, 256 blocks tall)      │
    /// │         └── Root NBT Compound (named: true)                     │
    /// │             ├── "Level" Compound Tag                            │
    /// │             │   ├── "Sections" List Tag [0-15]                  │
    /// │             │   │   └── Section (16×16×16 block cube)           │
    /// │             │   │       ├── "Y" Byte: Section height (0=Y:0-15) │
    /// │             │   │       ├── "Blocks" ByteArray: [4096] block IDs│
    /// │             │   │       └── "Data" ByteArray: block metadata    │
    /// │             │   ├── "Biomes" ByteArray: [256] biome data        │
    /// │             │   ├── "Entities" List: mobs, items, etc.          │
    /// │             │   └── "TileEntities" List: chests, signs, etc.    │
    /// │             └── "DataVersion" Int: chunk format version         │
    /// └─────────────────────────────────────────────────────────────────┘
    /// 
    /// Coordinate System:
    /// - Region coordinates: filename r.X.Z.mca maps to world chunks (X*32 to X*32+31, Z*32 to Z*32+31)
    /// - Chunk index: i = localChunkZ * 32 + localChunkX (0-1023)
    /// - Block index within Section: localY * 256 + localZ * 16 + localX (0-4095)
    /// - World coordinates: chunkCoord * 16 + localBlockCoord
    /// 
    /// Note: We only extract Y=0 section blocks for 2D top-down visualization
    /// </summary>
    private static async Task<List<BlockPos>> ParseRegionFile(Stream stream, int regionX, int regionZ)
    {
        var blocks = new List<BlockPos>();

        // Load entire .mca file into memory
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var buffer = new byte[4];

        // Read header: 4KB offsets + 4KB timestamps
        var offsets = new int[1024];
        for (int i = 0; i < 1024; i++)
        {
            await memoryStream.ReadExactlyAsync(buffer, 0, 4);
            offsets[i] = BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        // Skip timestamps (not needed)
        memoryStream.Position += 4096;

        // Process all chunks
        for (int i = 0; i < 1024; i++)
        {
            int offset = offsets[i];
            if (offset == 0) continue;

            try
            {
                int sectorStart = offset >> 8;
                long byteOffset = (long)sectorStart * SECTOR_BYTES;
                if (byteOffset >= memoryStream.Length) continue;

                memoryStream.Seek(byteOffset, SeekOrigin.Begin);

                await memoryStream.ReadExactlyAsync(buffer, 0, 4);
                int length = BinaryPrimitives.ReadInt32BigEndian(buffer);
                if (length <= 1) continue;

                var versionBuffer = new byte[1];
                await memoryStream.ReadExactlyAsync(versionBuffer, 0, 1);
                byte version = versionBuffer[0];

                var chunkData = new byte[length - 1];
                await memoryStream.ReadExactlyAsync(chunkData, 0, length - 1);

                var chunkNbt = await DecompressChunk(chunkData, version);
                if (chunkNbt == null) continue;

                var section = GetSectionAtY0(chunkNbt);
                if (section == null) continue;

                // Calculate global chunk coordinates
                int localChunkX = i % 32;
                int localChunkZ = i / 32;
                int globalChunkX = regionX * 32 + localChunkX;
                int globalChunkZ = regionZ * 32 + localChunkZ;

                ExtractBlocksFromSection(section, globalChunkX, globalChunkZ, blocks);
            }
            catch
            {
                // Silently skip problematic chunks
                continue;
            }
        }

        return blocks;
    }

    private static async Task<CompoundTag?> DecompressChunk(byte[] data, byte version)
    {
        try
        {
            await using var inputStream = new MemoryStream(data);

            // Select decompression stream
            Stream decompressStream = version switch
            {
                1 => new GZipStream(inputStream, CompressionMode.Decompress),
                2 => new ZLibStream(inputStream, CompressionMode.Decompress),
                3 => inputStream,
                _ => throw new NotSupportedException($"Compression version {version}")
            };

            await using (decompressStream)
            {
                using var decompressedStream = new MemoryStream();
                await decompressStream.CopyToAsync(decompressedStream);
                if (decompressedStream.Length == 0) return null;

                decompressedStream.Position = 0;
                using var reader = new TagReader(decompressedStream, FormatOptions.Java);
                return reader.ReadTag<CompoundTag>(named: true);
            }
        }
        catch
        {
            return null;
        }
    }

    private static CompoundTag? GetSectionAtY0(CompoundTag chunk)
    {
        var levelTag = chunk.Find("Level", deep: false) as CompoundTag;
        var sectionsTag = levelTag?.Find("Sections", deep: false) as ListTag;

        if (sectionsTag == null) return null;

        // Find Y=0 section, or lowest available
        CompoundTag? fallbackSection = null;
        int lowestY = int.MaxValue;

        foreach (var sec in sectionsTag.Cast<CompoundTag>())
        {
            var yTag = sec.Find("Y", deep: false) as ByteTag;
            if (yTag == null) continue;

            if (yTag.Value == 0) return sec;  // Perfect match

            if (yTag.Value < lowestY)
            {
                lowestY = yTag.Value;
                fallbackSection = sec;
            }
        }

        return fallbackSection; // Return lowest available if Y=0 not found
    }

    private static void ExtractBlocksFromSection(CompoundTag section, int chunkX, int chunkZ, List<BlockPos> blocks)
    {
        var blocksTag = section.Find("Blocks", deep: false) as ByteArrayTag;
        var yTag = section.Find("Y", deep: false) as ByteTag;

        if (blocksTag == null || yTag == null) 
            return;

        var blockIds = blocksTag.ToArray();
        int sectionY = yTag.Value;

        if (sectionY != 0) 
            return;

        // Nur localY = 0 (absolutes Y = 0)
        for (int localZ = 0; localZ < 16; localZ++)
        {
            for (int localX = 0; localX < 16; localX++)
            {
                int index = 0 * 256 + localZ * 16 + localX;
                if (index >= blockIds.Length) continue;

                byte blockId = blockIds[index];
                if (blockId == 0) continue; // Skip air

                int worldX = chunkX * 16 + localX;
                int worldZ = chunkZ * 16 + localZ;

                blocks.Add(new BlockPos(worldX, 0, worldZ));
            }
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
        return (0, 0);
    }
}
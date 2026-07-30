#!/usr/bin/env python3
"""Generate a 1.8.9-format Minecraft world (level.dat + region files + map.xml).

The world importer only reads the y=0 layer of section Y=0, so this writes exactly
that layer: a bedrock floor shaped like a two-lane CTW map, sized to match a
mid-size community map (~70k floor blocks). That is the payload the canvas has to
draw, which makes it the fixture for rendering measurements.

Usage: python3 tools/make-sample-world.py <outputDir>
Output: <outputDir>/level.dat, <outputDir>/r.<x>.<z>.mca, <outputDir>/map.xml
"""

import math
import os
import struct
import sys
import zlib

SECTOR_BYTES = 4096
BEDROCK = 7

# Map footprint in block coordinates, centred on the origin.
HALF_WIDTH = 160
HALF_DEPTH = 120


# --- NBT writing ------------------------------------------------------------
# Payloads are (tagId, bytes) pairs; a compound is an ordered dict of them.

def encode_name(text):
    raw = text.encode("utf-8")
    return struct.pack(">H", len(raw)) + raw


def tag_byte(value):
    return 1, struct.pack(">b", value)


def tag_int(value):
    return 3, struct.pack(">i", value)


def tag_long(value):
    return 4, struct.pack(">q", value)


def tag_string(value):
    raw = value.encode("utf-8")
    return 8, struct.pack(">H", len(raw)) + raw


def tag_byte_array(data):
    return 7, struct.pack(">i", len(data)) + bytes(data)


def tag_list(itemTagId, itemPayloads):
    body = struct.pack(">b", itemTagId) + struct.pack(">i", len(itemPayloads))
    return 9, body + b"".join(itemPayloads)


def tag_compound(fields):
    body = b""
    for fieldName, (tagId, payload) in fields.items():
        body += struct.pack(">b", tagId) + encode_name(fieldName) + payload
    return 10, body + b"\x00"


def write_root(compoundPayload):
    return struct.pack(">b", 10) + encode_name("") + compoundPayload


# --- world shape ------------------------------------------------------------

def is_floor(worldX, worldZ):
    """The map footprint: a rounded arena with two carved-out void pockets,
    mirrored across the z axis so the two team halves match."""
    if abs(worldX) > HALF_WIDTH or abs(worldZ) > HALF_DEPTH:
        return False

    # Rounded corners.
    cornerX = abs(worldX) - (HALF_WIDTH - 40)
    cornerZ = abs(worldZ) - (HALF_DEPTH - 40)
    if cornerX > 0 and cornerZ > 0 and cornerX * cornerX + cornerZ * cornerZ > 40 * 40:
        return False

    # Void pockets flanking the centre lane, mirrored on both halves.
    for pocketX in (-70, 70):
        for pocketZ in (-55, 55):
            distanceX = worldX - pocketX
            distanceZ = worldZ - pocketZ
            if distanceX * distanceX + distanceZ * distanceZ < 26 * 26:
                return False

    # A ragged centre channel so the outline is not a clean rectangle.
    wobble = 6 + 4 * math.sin(worldX / 18.0)
    if abs(worldZ) < wobble and abs(worldX) > 30:
        return False

    return True


def build_chunk_blocks(chunkX, chunkZ):
    """A 4096-byte section block array with the floor written at local y=0.
    Returns None when the chunk has no floor at all (skipped, like an autopruned
    world does)."""
    blocks = bytearray(4096)
    any_floor = False

    for localZ in range(16):
        worldZ = chunkZ * 16 + localZ
        for localX in range(16):
            worldX = chunkX * 16 + localX
            if is_floor(worldX, worldZ):
                blocks[localZ * 16 + localX] = BEDROCK
                any_floor = True

    return blocks if any_floor else None


def build_chunk_nbt(chunkX, chunkZ, blocks):
    section = tag_compound({
        "Y": tag_byte(0),
        "Blocks": tag_byte_array(blocks),
        "Data": tag_byte_array(bytes(2048)),
        "BlockLight": tag_byte_array(bytes(2048)),
        "SkyLight": tag_byte_array(b"\xff" * 2048),
    })

    level = tag_compound({
        "xPos": tag_int(chunkX),
        "zPos": tag_int(chunkZ),
        "LastUpdate": tag_long(0),
        "InhabitedTime": tag_long(0),
        "TerrainPopulated": tag_byte(1),
        "LightPopulated": tag_byte(1),
        "Biomes": tag_byte_array(b"\x01" * 256),
        "Sections": tag_list(10, [section[1]]),
    })

    root = tag_compound({"Level": level})
    return write_root(root[1])


# --- region files -----------------------------------------------------------

def write_region(path, chunks):
    """chunks: {(localChunkX, localChunkZ): nbtBytes}"""
    offsets = bytearray(SECTOR_BYTES)
    timestamps = bytearray(SECTOR_BYTES)
    body = b""
    nextSector = 2

    for (localChunkX, localChunkZ), nbt in sorted(chunks.items()):
        compressed = zlib.compress(nbt, 6)
        # 4-byte length (payload + compression byte), then the compression scheme.
        payload = struct.pack(">i", len(compressed) + 1) + b"\x02" + compressed
        padded = payload + bytes((-len(payload)) % SECTOR_BYTES)
        sectorCount = len(padded) // SECTOR_BYTES

        index = localChunkZ * 32 + localChunkX
        struct.pack_into(">i", offsets, index * 4, (nextSector << 8) | sectorCount)
        struct.pack_into(">i", timestamps, index * 4, 0)

        body += padded
        nextSector += sectorCount

    with open(path, "wb") as handle:
        handle.write(offsets)
        handle.write(timestamps)
        handle.write(body)


def write_level_dat(path, levelName):
    data = tag_compound({
        "version": tag_int(19133),
        "LevelName": tag_string(levelName),
        "SpawnX": tag_int(0),
        "SpawnY": tag_int(1),
        "SpawnZ": tag_int(0),
        "GameType": tag_int(1),
        "Time": tag_long(0),
        "LastPlayed": tag_long(0),
    })
    root = tag_compound({"Data": data})

    compressor = zlib.compressobj(6, zlib.DEFLATED, 16 + zlib.MAX_WBITS)
    gzipped = compressor.compress(write_root(root[1])) + compressor.flush()
    with open(path, "wb") as handle:
        handle.write(gzipped)


# --- map.xml ----------------------------------------------------------------

def write_map_xml(path, levelName):
    """A regions block at the scale a real map carries, so the region renderer
    gets a representative workload."""
    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<map proto="1.4.0">',
        f"  <name>{levelName}</name>",
        "  <version>1.0.0</version>",
        "  <objective>Capture the enemy wools.</objective>",
        "  <gamemode>ctw</gamemode>",
        "  <maxbuildheight>40</maxbuildheight>",
        "  <regions>",
    ]

    for teamIndex, sign in enumerate((-1, 1)):
        teamName = "red" if sign < 0 else "blue"
        lines.append(
            f'    <rectangle id="{teamName}-spawn" '
            f'min="{sign * 150},{sign * 110}" max="{sign * 120},{sign * 80}"/>'
        )
        for woolIndex, woolOffset in enumerate((-90, -30, 30, 90)):
            lines.append(
                f'    <block id="{teamName}-wool-{woolIndex}">'
                f"{woolOffset},9,{sign * 112}</block>"
            )
            lines.append(
                f'    <cylinder id="{teamName}-wool-{woolIndex}-approach" '
                f'base="{woolOffset},0,{sign * 100}" radius="12" height="8"/>'
            )
            lines.append(
                f'    <point id="{teamName}-wool-{woolIndex}-marker">'
                f"{woolOffset},10,{sign * 104}</point>"
            )
        for laneIndex in range(6):
            laneX = -140 + laneIndex * 56
            lines.append(
                f'    <rectangle id="{teamName}-lane-{laneIndex}" '
                f'min="{laneX},{sign * 70}" max="{laneX + 40},{sign * 20}"/>'
            )
        lines.append(f'    <union id="{teamName}-playable">')
        for pocketIndex, pocketX in enumerate((-70, 70)):
            lines.append(
                f'      <circle id="{teamName}-pocket-{pocketIndex}" '
                f'center="{pocketX},{sign * 55}" radius="26"/>'
            )
        lines.append("    </union>")

    lines.append('    <rectangle id="mid" min="-160,-10" max="160,10"/>')
    lines.append("  </regions>")
    lines.append("</map>")

    with open(path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")


def main():
    outputDir = sys.argv[1] if len(sys.argv) > 1 else "sample-world"
    levelName = "sample_ctw"
    os.makedirs(outputDir, exist_ok=True)

    chunkMinX = -(HALF_WIDTH // 16) - 1
    chunkMaxX = (HALF_WIDTH // 16) + 1
    chunkMinZ = -(HALF_DEPTH // 16) - 1
    chunkMaxZ = (HALF_DEPTH // 16) + 1

    regions = {}
    floorBlocks = 0

    for chunkZ in range(chunkMinZ, chunkMaxZ + 1):
        for chunkX in range(chunkMinX, chunkMaxX + 1):
            blocks = build_chunk_blocks(chunkX, chunkZ)
            if blocks is None:
                continue

            floorBlocks += sum(1 for value in blocks[:256] if value != 0)

            regionKey = (chunkX >> 5, chunkZ >> 5)
            localKey = (chunkX & 31, chunkZ & 31)
            regions.setdefault(regionKey, {})[localKey] = build_chunk_nbt(
                chunkX, chunkZ, blocks
            )

    for (regionX, regionZ), chunks in regions.items():
        write_region(os.path.join(outputDir, f"r.{regionX}.{regionZ}.mca"), chunks)

    write_level_dat(os.path.join(outputDir, "level.dat"), levelName)
    write_map_xml(os.path.join(outputDir, "map.xml"), levelName)

    print(f"{outputDir}: {len(regions)} region file(s), {floorBlocks} floor blocks")


if __name__ == "__main__":
    main()

import json

# Load project
with open(r"F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel projects\Tilemap\Tilemap.rtrxproject", "r") as f:
    project = json.load(f)

# Get tilemap data
tilemap_element = None
for scene in project["Scenes"]:
    for element in scene["elements"]:
        if element["moduleId"] == "tilemap":
            tilemap_element = element
            break

if tilemap_element:
    map_data = tilemap_element["moduleState"]["mapData"]
    
    # Count tile usage
    tile_usage = {}
    for tile_idx in map_data:
        tile_usage[tile_idx] = tile_usage.get(tile_idx, 0) + 1
    
    # Sort by usage
    sorted_tiles = sorted(tile_usage.items(), key=lambda x: x[1], reverse=True)
    
    print(f"Total tiles in tilemap: {len(map_data)}")
    print(f"Unique tiles used: {len(tile_usage)}")
    print(f"Max tile index: {max(map_data)}")
    print(f"\nTile usage (top 20):")
    for tile_idx, count in sorted_tiles[:20]:
        print(f"  Tile {tile_idx}: {count} times ({count/len(map_data)*100:.1f}%)")
    
    # Find unused tiles
    asset = next((a for a in project["Assets"] if a["Id"] == "livelink_tileset_optimized"), None)
    if asset:
        total_tiles = asset["TileCount"]
        used_tiles = set(map_data)
        unused_tiles = set(range(total_tiles)) - used_tiles
        
        print(f"\nTotal tiles in asset: {total_tiles}")
        print(f"Used tiles: {len(used_tiles)}")
        print(f"Unused tiles: {len(unused_tiles)}")
        if unused_tiles:
            print(f"Unused tile indices: {sorted(unused_tiles)}")

# Auto-Tiling, Metatiles, and Brushes Documentation

## Auto-Tiling
- Automatically selects appropriate edge/corner tiles based on painting direction
- Uses LDtk-style logic for seamless pattern continuation
- Configuration: Adjustable rules in `context/rules/memory-bank/auto-tiling-rules.md`

## Metatiles
- 2x2 and 4x2 block placement system
- User can stamp entire metatile at once
- Requires matching edge tiles for seamless connections

## Brushes
- Predefined tile collections for common patterns
- Supports rotation/flipping variations
- Accessible via new "Brush Palette" panel in TilemapEditor

## Integration
- All features work within existing painting workflow
- Undo/redo supports tile changes at both individual and metatile levels
- Brush selections persist between editing sessions

## Usage Patterns
1. Select a brush from the palette
2. Paint area using standard controls
3. Auto-tiling activates automatically at edges
4. Metatiles can be stamped by holding shift while painting

## Workflow
- Start with basic painting
- Use metatiles for large areas
- Switch to brushes for complex patterns
- Auto-tiling handles edge cases automatically

## Troubleshooting
- Ensure `context/rules/memory-bank/guidelines.md` is up-to-date
- Verify palette configurations in `Retruxel.Core/Services/PaletteToTilemapConnector.cs`

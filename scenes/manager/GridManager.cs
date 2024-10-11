using System;
using System.Collections.Generic;
using Godot;

namespace Game.Manager;

public partial class GridManager : Node
{
	private HashSet<Vector2I> occupiedCells = new();

	[Export]
	private TileMapLayer highlightTileMapLayer;
	[Export]
	private TileMapLayer baseTerrainTileMapLayer;

	public bool IsTilePositionValid(Vector2I tilePosition)
	{
		var customData = baseTerrainTileMapLayer.GetCellTileData(tilePosition);
		if (customData == null) return false;
		if (!(bool) customData.GetCustomData("buildable")) return false;
		return !occupiedCells.Contains(tilePosition);
	}

	public void MarkTileAsOccupied(Vector2I tilePosition)
	{
		occupiedCells.Add(tilePosition);
	}

	public void HighlightValidTilesInRadius(Vector2I rootCell, int radius)
	{
		ClearHighlightedTiles();

		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x, y);
				if (!IsTilePositionValid(tilePosition)) continue;
				highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
			}
		}
	}

	public void ClearHighlightedTiles()
	{
		highlightTileMapLayer.Clear();
	}

	public Vector2I GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();
		var gridPosition = mousePosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int) gridPosition.X, (int) gridPosition.Y);
	}
}

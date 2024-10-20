using System;
using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Godot;

namespace Game.Manager;

public partial class GridManager : Node
{
	private const string IS_BUILDABLE = "is_buildable";
	private const string IS_WOOD = "is_wood";
	private const string IS_IGNORED = "is_ignored";

	[Signal]
	public delegate void ResourceTilesUpdatedEventHandler(int collededTiles);
	[Signal]
	public delegate void GridStateUpdatedEventHandler();

	private HashSet<Vector2I> validBuildableTiles = new();
	private HashSet<Vector2I> collectedResourceTiles = new();
	private HashSet<Vector2I> occupiedTiles = new();

	[Export]
	private TileMapLayer highlightTileMapLayer;
	[Export]
	private TileMapLayer baseTerrainTileMapLayer;
	private List<TileMapLayer> allTileMapLayers = new();

	public override void _Ready()
	{
		GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
		GameEvents.Instance.BuildingDestroyed += OnBuildingDestroyed;
		allTileMapLayers = GetAllTileMapLayers(baseTerrainTileMapLayer);
	}

	public bool TileHasCustomData(Vector2I tilePosition, string dataName)
	{
		foreach (var layer in allTileMapLayers)
		{
			var customData = layer.GetCellTileData(tilePosition);
			if (customData == null || (bool) customData.GetCustomData(IS_IGNORED)) continue;
			return (bool)customData.GetCustomData(dataName);
		}

		return false;
	}

	public bool IsTilePositionBuildable(Vector2I tilePosition)
	{
		return validBuildableTiles.Contains(tilePosition);
	}

	public void HighlightBuildableTiles()
	{
		foreach (var tilePosition in validBuildableTiles)
		{
			highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
		}
	}

	public void HighlightExpandedBuildableTiles(Vector2I rootCell, int radius)
	{
		var validTiles = GetValidTilesInRadius(rootCell, radius).ToHashSet();
		var expandedTiles = validTiles.Except(validBuildableTiles).Except(occupiedTiles);
		var atlasCoords = new Vector2I(1, 0);
		foreach (var tilePosition in expandedTiles)
		{
			highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}

	public void HighlightResourceTiles(Vector2I rootCell, int radius)
	{
		var resourceTiles = GetResourceTilesInRadius(rootCell, radius);
		var atlasCoords = new Vector2I(1, 0);
		foreach (var tilePosition in resourceTiles)
		{
			highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}

	public void ClearHighlightedTiles()
	{
		highlightTileMapLayer.Clear();
	}

	public Vector2I GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();
		return ConvertWorldPositionToTilePosition(mousePosition);
	}

	public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition)
	{
		var gridPosition = worldPosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

	private List<TileMapLayer> GetAllTileMapLayers(TileMapLayer rootTileMapLayer)
	{
		var result = new List<TileMapLayer>();
		var children = rootTileMapLayer.GetChildren();
		children.Reverse();
		foreach (var child in children)
		{
			if (child is TileMapLayer childLayer)
			{
				result.AddRange(GetAllTileMapLayers(childLayer));
			}
		}

		result.Add(rootTileMapLayer);
		return result;
	}

	private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		occupiedTiles.Add(buildingComponent.GetGridCellPosition());
		var rootCell = buildingComponent.GetGridCellPosition();
		var validTiles = GetValidTilesInRadius(rootCell, buildingComponent.BuildingResource.BuildableRadius);
		validBuildableTiles.UnionWith(validTiles);
		validBuildableTiles.ExceptWith(occupiedTiles);
		EmitSignal(SignalName.GridStateUpdated);
	}

	private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
	{
		var rootCell = buildingComponent.GetGridCellPosition();
		var resourceTiles = GetResourceTilesInRadius(rootCell, buildingComponent.BuildingResource.ResourceRadius);

		var oldResoureTileCount = collectedResourceTiles.Count;
		collectedResourceTiles.UnionWith(resourceTiles);

		if (oldResoureTileCount != collectedResourceTiles.Count)
		{
			EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
		}
		EmitSignal(SignalName.GridStateUpdated);
	}

	private void RecalculateGrid(BuildingComponent excludeBuildingComponent)
	{
		occupiedTiles.Clear();
		validBuildableTiles.Clear();
		collectedResourceTiles.Clear();

		var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>()
			.Where((buildingComponent) => buildingComponent != excludeBuildingComponent);

		foreach (var buildingComponent in buildingComponents)
		{
			UpdateValidBuildableTiles(buildingComponent);
			UpdateCollectedResourceTiles(buildingComponent);
		}

		EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
		EmitSignal(SignalName.GridStateUpdated);
	}

	private List<Vector2I> GetTilesInRadius(Vector2I rootCell, int radius, Func<Vector2I, bool> filterFn)
	{
		var result = new List<Vector2I>();
		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x, y);
				if (!filterFn(tilePosition)) continue;
				result.Add(tilePosition);
			}
		}

		return result;
	}

	private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius)
	{
		return GetTilesInRadius(rootCell, radius, (tilePosition) => TileHasCustomData(tilePosition, IS_BUILDABLE));
	}

	private List<Vector2I> GetResourceTilesInRadius(Vector2I rootCell, int radius)
	{
		return GetTilesInRadius(rootCell, radius, (tilePosition) => TileHasCustomData(tilePosition, IS_WOOD));
	}

	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateValidBuildableTiles(buildingComponent);
		UpdateCollectedResourceTiles(buildingComponent);
	}

	private void OnBuildingDestroyed(BuildingComponent buildingComponent)
	{
		RecalculateGrid(buildingComponent);
	}
}

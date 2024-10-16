using Game.Building;
using Game.Resources.Building;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{
	private readonly StringName ACTION_LEFT_CLICK = "left_click";
	private readonly StringName ACTION_CANCEL = "cancel";

	[Export]
	private GridManager gridManager;
	[Export]
	private GameUI gameUI;
	[Export]
	private Node2D ySortRoot;
	[Export]
	private PackedScene buildingGhostScene;


	private int currentResourceCount;
	private int startingResourceCount = 100;
	private int currentlyUsedResouceCount;
	private BuildingResource toPlaceBuildingResource;
	private Vector2I? hoveredGridCell;
	private BuildingGhost buildingGhost;

	private int AvailableResourceCount => startingResourceCount + currentResourceCount - currentlyUsedResouceCount;

	public override void _Ready()
	{
		gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
		gameUI.BuildingResourceSelected += OnBuildingResourceSelected;
	}

	public override void _UnhandledInput(InputEvent evt)
	{
		if (evt.IsActionPressed(ACTION_CANCEL))
		{
			ClearBuildingGhost();
		}
		else if (
			hoveredGridCell.HasValue &&
			toPlaceBuildingResource != null &&
			evt.IsActionPressed(ACTION_LEFT_CLICK) &&
			isBuildingPlaceableAtTile(hoveredGridCell.Value)
			)
		{
			PlaceBuildingAtHoveredCellPosition();
		}
	}

	public override void _Process(double delta)
	{
		if (!IsInstanceValid(buildingGhost)) return;

		var gridPosition = gridManager.GetMouseGridCellPosition();
		buildingGhost.GlobalPosition = gridPosition * 64;
		if (toPlaceBuildingResource != null && (!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
		{
			hoveredGridCell = gridPosition;
			UpdateGridDisplay();
		}
	}

	private void UpdateGridDisplay()
	{
		if (hoveredGridCell == null) return;

		gridManager.ClearHighlightedTiles();
		gridManager.HighlightBuildableTiles();

		if (isBuildingPlaceableAtTile(hoveredGridCell.Value))
		{
			gridManager.HighlightExpandedBuildableTiles(hoveredGridCell.Value, toPlaceBuildingResource.BuildableRadius);
			gridManager.HighlightResourceTiles(hoveredGridCell.Value, toPlaceBuildingResource.ResourceRadius);
			buildingGhost.setValid();
		}
		else
		{
			buildingGhost.SetInvalid();
		}
	}

	private void PlaceBuildingAtHoveredCellPosition()
	{
		if (!hoveredGridCell.HasValue)
		{
			return;
		}

		var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
		ySortRoot.AddChild(building);

		building.GlobalPosition = hoveredGridCell.Value * 64;

		currentlyUsedResouceCount += toPlaceBuildingResource.ResourceCost;

		ClearBuildingGhost();
	}

	private void ClearBuildingGhost()
	{
		hoveredGridCell = null;
		gridManager.ClearHighlightedTiles();
		if (IsInstanceValid(buildingGhost))
		{
			buildingGhost.QueueFree();
		}
		buildingGhost = null;
	}

	private bool isBuildingPlaceableAtTile(Vector2I tilePosition)
	{
		return gridManager.IsTilePositionBuildable(tilePosition) && AvailableResourceCount >= toPlaceBuildingResource.ResourceCost;
	}

	private void OnResourceTilesUpdated(int resourceCount)
	{
		currentResourceCount += resourceCount;
	}

	private void OnBuildingResourceSelected(BuildingResource buildingResource)
	{
		if (IsInstanceValid(buildingGhost))
		{
			buildingGhost.QueueFree();
		}

		buildingGhost = buildingGhostScene.Instantiate<BuildingGhost>();
		ySortRoot.AddChild(buildingGhost);

		var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
		buildingGhost.AddChild(buildingSprite);

		toPlaceBuildingResource = buildingResource;
		UpdateGridDisplay();
	}
}

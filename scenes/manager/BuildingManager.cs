using Game.Resources.Building;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{
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
	private Node2D buildingGhost;

	private int AvailableResourceCount => startingResourceCount + currentResourceCount - currentlyUsedResouceCount;

	public override void _Ready()
	{
		gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
		gameUI.BuildingResourceSelected += OnBuildingResourceSelected;
	}

	public override void _UnhandledInput(InputEvent evt)
	{
		if (
			hoveredGridCell.HasValue &&
			toPlaceBuildingResource != null &&
			evt.IsActionPressed("left_click") &&
			gridManager.IsTilePositionBuildable(hoveredGridCell.Value) &&
			AvailableResourceCount >= toPlaceBuildingResource.ResourceCost
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
			gridManager.ClearHighlightedTiles();
			gridManager.HighlightExpandedBuildableTiles(hoveredGridCell.Value, toPlaceBuildingResource.BuildableRadius);
			gridManager.HighlightResourceTiles(hoveredGridCell.Value, toPlaceBuildingResource.ResourceRadius);
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

		hoveredGridCell = null;
		gridManager.ClearHighlightedTiles();
		currentlyUsedResouceCount += toPlaceBuildingResource.ResourceCost;
		buildingGhost.QueueFree();
		buildingGhost = null;
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

		buildingGhost = buildingGhostScene.Instantiate<Node2D>();
		ySortRoot.AddChild(buildingGhost);

		var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
		buildingGhost.AddChild(buildingSprite);

		toPlaceBuildingResource = buildingResource;
		gridManager.HighlightBuildableTiles();
	}
}

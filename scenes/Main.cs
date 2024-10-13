using Game.Manager;
using Godot;

namespace Game;

public partial class Main : Node
{
	private GridManager gridManager;
	private Sprite2D cursor;
	private PackedScene towerScene;
	private PackedScene villageScene;
	private Button placeTowerButton;
	private Button placeVillageButton;
	private Node2D ySortRoot;
	private Vector2I? hoveredGridCell;
	private PackedScene toPlaceBuildingScene;

	public override void _Ready()
	{
		towerScene = GD.Load<PackedScene>("res://scenes/building/Tower.tscn");
		villageScene = GD.Load<PackedScene>("res://scenes/building/Village.tscn");
		gridManager = GetNode<GridManager>("GridManager");
		cursor = GetNode<Sprite2D>("Cursor");
		placeTowerButton = GetNode<Button>("PlaceTowerButton");
		placeVillageButton = GetNode<Button>("PlaceVillageButton");
		ySortRoot = GetNode<Node2D>("YSortRoot");

		cursor.Visible = false;

		placeTowerButton.Pressed += OnPlaceBuildingButtonPressed;
		placeVillageButton.Pressed += OnPlaceVillageButtonPressed;
	}

	public override void _UnhandledInput(InputEvent evt)
	{
		if (hoveredGridCell.HasValue && evt.IsActionPressed("left_click") && gridManager.IsTilePositionBuildable(hoveredGridCell.Value))
		{
			PlaceBuildingAtHoveredCellPosition();
			cursor.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		var gridPosition = gridManager.GetMouseGridCellPosition();
		cursor.GlobalPosition = gridPosition * 64;
		if (cursor.Visible && (!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
		{
			hoveredGridCell = gridPosition;
			gridManager.HighlightExpandedBuildableTiles(hoveredGridCell.Value, 3);
		}
	}

	private void PlaceBuildingAtHoveredCellPosition()
	{
		if (!hoveredGridCell.HasValue)
		{
			return;
		}

		var building = toPlaceBuildingScene.Instantiate<Node2D>();
		ySortRoot.AddChild(building);

		building.GlobalPosition = hoveredGridCell.Value * 64;

		hoveredGridCell = null;
		gridManager.ClearHighlightedTiles();

	}

	private void OnPlaceBuildingButtonPressed()
	{
		toPlaceBuildingScene = towerScene;
		cursor.Visible = true;
		gridManager.HighlightBuildableTiles();
	}

	private void OnPlaceVillageButtonPressed()
	{
		toPlaceBuildingScene = villageScene;
		cursor.Visible = true;
		gridManager.HighlightBuildableTiles();
	}
}

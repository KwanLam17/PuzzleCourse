using Godot;
using System.Collections.Generic;

namespace Game;

public partial class Main : Node
{
	private Sprite2D cursor;
	private PackedScene buildingScene;
	private Button placeBuildingButton;
	private TileMapLayer highlightTileMapLayer;
	private Vector2? hoveredGridCell;
	private HashSet<Vector2> occupiedCells = new();

	public override void _Ready()
	{
		buildingScene = GD.Load<PackedScene>("res://scenes/building/Building.tscn");
		cursor = GetNode<Sprite2D>("Cursor");
		placeBuildingButton = GetNode<Button>("PlaceBuildingButton");
		highlightTileMapLayer = GetNode<TileMapLayer>("HighlightTileMapLayer");

		cursor.Visible = false;

		placeBuildingButton.Pressed += OnButtonPressed;
	}

	public override void _UnhandledInput(InputEvent evt)
	{
		if (hoveredGridCell.HasValue && evt.IsActionPressed("left_click") && !occupiedCells.Contains(hoveredGridCell.Value))
		{
			PlaceBuildingAtHoveredCellPosition();
			cursor.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		var gridPosition = GetMouseGridCellPosition();
		cursor.GlobalPosition = gridPosition * 64;
		if (cursor.Visible && (!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
		{
			hoveredGridCell = gridPosition;
			UpdateHighlightTileMapLayer();
		}
	}

	private Vector2 GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();
		var gridPosition = mousePosition / 64;
		gridPosition = gridPosition.Floor();
		return gridPosition;
	}

	private void PlaceBuildingAtHoveredCellPosition()
	{
		if (!hoveredGridCell.HasValue)
		{
			return;
		}

		var building = buildingScene.Instantiate<Node2D>();
		AddChild(building);

		building.GlobalPosition = hoveredGridCell.Value * 64;
		occupiedCells.Add(hoveredGridCell.Value);

		hoveredGridCell = null;
		UpdateHighlightTileMapLayer();

	}

	private void UpdateHighlightTileMapLayer()
	{
		highlightTileMapLayer.Clear();
		if (!hoveredGridCell.HasValue)
		{
			return;
		}

		for (var x = hoveredGridCell.Value.X - 3; x <= hoveredGridCell.Value.X + 3; x++)
		{
			for (var y = hoveredGridCell.Value.Y - 3; y <= hoveredGridCell.Value.Y + 3; y++)
			{
				highlightTileMapLayer.SetCell(new Vector2I((int)x, (int)y), 0, Vector2I.Zero);
			}
		}
	}

	private void OnButtonPressed()
	{
		cursor.Visible = true;
	}
}

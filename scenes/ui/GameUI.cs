using Game.Resources.Building;
using Godot;

namespace Game.UI;

public partial class GameUI : CanvasLayer
{
	[Signal]
	public delegate void BuildingResourceSelectedEventHandler(BuildingResource buildingResource);

	private HBoxContainer hBoxContainer;

	[Export]
	private BuildingResource[] buildingResources;

	public override void _Ready()
	{
		hBoxContainer = GetNode<HBoxContainer>("MarginContainer/HBoxContainer");
		CreateBuildingButtons();
	}

	private void CreateBuildingButtons()
	{
		foreach (var buildingResource in buildingResources)
		{
			var buildingButton = new Button();
			buildingButton.Text = $"Place {buildingResource.DisplayName}";
			hBoxContainer.AddChild(buildingButton);

			buildingButton.Pressed += () =>
			{
				EmitSignal(SignalName.BuildingResourceSelected, buildingResource);
			};
		}
	}
}

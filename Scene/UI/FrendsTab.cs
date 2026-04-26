using Godot;
using System;

public partial class FrendsTab : Control
{
	[Export] private Button JockerButton;
	[Export] private Button ERTENButton;
	[Export] private Button AhmedakButton;
	[Export] private Button FaksButton;
	public override void _Ready()
	{
		JockerButton.Pressed += () =>  GetTree().ChangeSceneToFile("res://Scene/Sobe/soba3.tscn");
		ERTENButton.Pressed += () =>  GetTree().ChangeSceneToFile("res://Scene/Sobe/soba_baza.tscn");
		AhmedakButton.Pressed += () =>  GetTree().ChangeSceneToFile("res://Scene/Sobe/soba4.tscn");
		FaksButton.Pressed += () =>  GetTree().ChangeSceneToFile("res://Scene/Sobe/odnik.tscn");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

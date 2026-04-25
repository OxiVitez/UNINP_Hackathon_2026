using Godot;
using System;

public partial class BBlioteka : Node2D
{
	[Export] private Area2D _StisniArea;
	[Export] private Panel _PRIMERknjige;
	public override void _Ready()
	{
		_PRIMERknjige.visible = false;
	}

	public override void _Process(double delta)
	{
	}
}

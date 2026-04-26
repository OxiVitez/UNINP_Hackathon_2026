using Godot;
using System;

public partial class Area2d : Area2D
{
	[Export] FrendsTab frendsTab;
	private bool isPlayerInside = false;
	public override void _Ready()
	{
		frendsTab.Visible = false;
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (isPlayerInside && Input.IsActionJustPressed("Interakcija"))
		{
			frendsTab.Visible = !frendsTab.Visible;
		}
	}
	private void OnBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			isPlayerInside = true;
		}
	}
	private void OnBodyExited(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			isPlayerInside = false;
		}
	}
}

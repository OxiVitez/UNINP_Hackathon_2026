using Godot;
using System;

public partial class UdjiUsobu : Area2D
{
	private bool Uareaje = false;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionJustPressed("Interakcija")&& Uareaje)
		{
			GetTree().ChangeSceneToFile("res://Scene/Sobe/soba2.tscn");
		}
	}
	private void OnBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			Uareaje = true;
		}
	}
}

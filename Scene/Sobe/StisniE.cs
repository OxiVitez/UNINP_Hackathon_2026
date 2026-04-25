using Godot;
using System;

public partial class StisniE : Area2D
{
	[Export] private CanvasLayer Canvas;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		Canvas.Visible = false;
	}
	private bool Uareaje = false;

	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionJustPressed("Interakcija")&& Uareaje)
		{
				Canvas.Visible = true;
		}
	}
	private void OnBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			Uareaje = true;
		}
	}
	private void OnBodyExited(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			Uareaje = false;
			Canvas.Visible = false;
			
		}
	}
}

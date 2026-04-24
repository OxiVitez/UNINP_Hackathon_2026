using Godot;
using System;

public partial class Karakterbaza : CharacterBody2D
{
	public const float Speed = 300.0f;
	[Export] private AnimatedSprite2D animacija;
	public override void _PhysicsProcess(double delta)
	{

	}
}

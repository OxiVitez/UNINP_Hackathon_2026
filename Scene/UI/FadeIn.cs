using Godot;
using System;

public partial class FadeIn : Control
{
	[Export] private AnimationPlayer _animacija;
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public void PokreniAnimaciju()
	{
		_animacija.Play("FadeIn");
	}
}

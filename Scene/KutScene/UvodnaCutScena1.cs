using Godot;
using System;

public partial class UvodnaCutScena1 : Node2D
{
	[Export] private Timer timer;
	[Export] private AnimationPlayer fideOut;
	public override void _Ready()
	{
		timer.Timeout += KrajVremena;
		timer.Start();
	}
	public override void _Process(double delta)
	{
	}
	private void KrajVremena()
	{
		fideOut.Play("FadeIn");
		GetTree().CreateTimer(1.7).Timeout += () => GetTree().ChangeSceneToFile("res://Scene/Sobe/odnik.tscn");  
	}
}

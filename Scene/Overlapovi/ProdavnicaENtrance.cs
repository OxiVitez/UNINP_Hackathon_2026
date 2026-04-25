using Godot;
using System;

public partial class ProdavnicaENtrance : Area2D
{
	[Export] private CanvasLayer ShopUI;
	[Export] private Button JabkuaButton;
	[Export] private Button KruskaButton;
	[Export] private Button BananaButton;
	[Export] private Button SljivaButton;
	[Export] private AnimationPlayer _tacanOdgovorAnimacija;	
	[Export] private AnimationPlayer _pogresanOdgovorAnimacija;
	private bool KvizJeresen = false;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		ShopUI.Visible = false;
		JabkuaButton.Pressed += TacanOdgovor;
		KruskaButton.Pressed += PogresanOdgovor;
		BananaButton.Pressed += PogresanOdgovor;
		SljivaButton.Pressed += PogresanOdgovor;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void OnBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D && !KvizJeresen)
		{
			ShopUI.Visible = true;
		}
	}
	private void OnBodyExited(Node2D body)
	{
		if (body is CharacterBody2D)
		{
			ShopUI.Visible = false;
		}
	}
	private void TacanOdgovor()
	{
		ShopUI.Visible = false;
		_tacanOdgovorAnimacija.Play("tt");
		KvizJeresen = true;
	}
	private void PogresanOdgovor()
	{
		ShopUI.Visible = false;
		_pogresanOdgovorAnimacija.Play("nn");
	}
}

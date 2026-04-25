using Godot;
using System;
using System.Collections.Generic;

public partial class BBlioteka : Node2D
{
	[Export] private Area2D _StisniArea;
	[Export] private Panel _meniPanel;
	[Export] private VBoxContainer _listaItema;
	[Export] private Panel _detaljiPanel;
	[Export] private Label _tekstTeza;
	[Export] private Button _nazadDugme;
	[Export] private Button[] _exitDugmici;
	[Export] private Button _debugTestDugme;
	[Export] private int _itemSpacing = 10;

	private bool _isPlayerNearby = false;

	private Dictionary<string, string> _mockPodaci = new()
	{
		{ "Knjiga o Programiranju", "• Osnove C#\n• Godot Engine 4\n• Arhitektura igara" },
		{ "Istorija Umetnosti", "• Renesansa\n• Barok\n• Modernizam" },
		{ "Matematika za Inženjere", "• Linearna algebra\n• Matematička analiza\n• Diskretna matematika" }
	};

	public override void _Ready()
	{
		// Sakrij panele na početku
		if (_meniPanel != null) _meniPanel.Visible = false;
		if (_detaljiPanel != null) _detaljiPanel.Visible = false;

		// Poveži signale za Area2D
		if (_StisniArea != null)
		{
			_StisniArea.BodyEntered += OnBodyEntered;
			_StisniArea.BodyExited += OnBodyExited;
			_StisniArea.InputEvent += OnAreaInputEvent;
		}

		if (_nazadDugme != null)
		{
			_nazadDugme.Pressed += OnNazadPressed;
		}

		if (_exitDugmici != null)
		{
			foreach (var dugme in _exitDugmici)
			{
				if (dugme != null) dugme.Pressed += ZatvoriSve;
			}
		}

		if (_debugTestDugme != null)
		{
			_debugTestDugme.Pressed += () => {
				GD.Print("Debug: Ručno otvaranje menija biblioteke.");
				OtvoriMeni();
			};
		}

		PopuniMeni();
	}

	private void OnBodyEntered(Node2D body)
	{
		// Proveri da li je igrač ušao u zonu
		if (body.Name.ToString().ToLower().Contains("player") || body is CharacterBody2D)
		{
			_isPlayerNearby = true;
			GD.Print("Igrač je blizu biblioteke. Klikni za interakciju.");
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body.Name.ToString().ToLower().Contains("player") || body is CharacterBody2D)
		{
			_isPlayerNearby = false;
			if (_meniPanel != null) _meniPanel.Visible = false;
			if (_detaljiPanel != null) _detaljiPanel.Visible = false;
			GD.Print("Igrač se udaljio od biblioteke.");
		}
	}

	private void OnAreaInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		if (_isPlayerNearby && @event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				OtvoriMeni();
			}
		}
	}

	private void OtvoriMeni()
	{
		if (_meniPanel != null)
		{
			_meniPanel.Visible = true;
			_detaljiPanel.Visible = false;
		}
	}

	private void PopuniMeni()
	{
		if (_listaItema == null) return;

		// Postavi razmak između dugmića
		_listaItema.AddThemeConstantOverride("separation", _itemSpacing);

		// Očisti listu
		foreach (Node child in _listaItema.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var stavka in _mockPodaci)
		{
			Button dugme = new Button();
			dugme.Text = stavka.Key;
			string teze = stavka.Value;
			
			dugme.Pressed += () => PrikažiDetalje(teze);
			_listaItema.AddChild(dugme);
		}
	}

	private void PrikažiDetalje(string teze)
	{
		if (_detaljiPanel != null && _tekstTeza != null)
		{
			_meniPanel.Visible = false;
			_detaljiPanel.Visible = true;
			_tekstTeza.Text = teze;
		}
	}

	private void OnNazadPressed()
	{
		if (_detaljiPanel != null)
		{
			_detaljiPanel.Visible = false;
			_meniPanel.Visible = true;
		}
	}

	private void ZatvoriSve()
	{
		if (_meniPanel != null) _meniPanel.Visible = false;
		if (_detaljiPanel != null) _detaljiPanel.Visible = false;
	}
}

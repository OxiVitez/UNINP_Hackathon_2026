using Godot;
using System;
using System.Collections.Generic;

public partial class BBlioteka : Node2D
{
	[Export] private Panel _meniPanel;
	[Export] private VBoxContainer _listaItema;
	[Export] private Panel _detaljiPanel;
	[Export] private Label _tekstTeza;
	[Export] private Button _nazadDugme;
	[Export] private Button[] _exitDugmici;
	[Export] private Button _debugTestDugme;
	[Export] private int _itemSpacing = 10;

	private string[] _imenaKurseva = { "Kurs Programiranja", "Kurs Ekonomije", "Kurs Umetnosti" };
	private string[] _tezeKurseva = 
	{ 
		"• Osnove C#\n• Godot Engine 4\n• Arhitektura igara",
		"• Mikroekonomija\n• Makroekonomija\n• Tržište kapitala",
		"• Renesansa\n• Barok\n• Modernizam"
	};

	public override void _Ready()
	{
		// Sakrij panele na početku
		ZatvoriSve();

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
			// Očistimo prethodne konekcije ako postoje i dodajemo novu
			_debugTestDugme.Pressed += () => {
				GD.Print("Dugme pritisnuto: Otvaram meni.");
				OtvoriMeni();
			};
		}

		PopuniMeni();
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

		// Prolazimo kroz nizove podataka
		for (int i = 0; i < _imenaKurseva.Length; i++)
		{
			if (i >= _tezeKurseva.Length) break;

			Button dugme = new Button();
			dugme.Text = _imenaKurseva[i];
			
			// Bela slova za dugmad u glavnom meniju
			dugme.AddThemeColorOverride("font_color", new Color(1, 1, 1)); 
			dugme.AddThemeColorOverride("font_hover_color", new Color(0.8f, 0.8f, 0.8f));
			
			string teze = _tezeKurseva[i];
			
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
			
			// Eksplicitno postavljamo crnu boju za tekst teza
			_tekstTeza.AddThemeColorOverride("font_color", new Color(0, 0, 0));
			_tekstTeza.Visible = true;
			
			GD.Print("Detalji uspešno postavljeni sa crnom bojom teksta.");
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

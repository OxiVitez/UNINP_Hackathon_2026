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

	private string[] _imenaKurseva = { 
    "Kurs Programiranja", 
    "Kurs Ekonomije", 
    "Kurs Umetnosti", 
    "Kurs Engleskog Jezika" 
};

private string[] _tezeKurseva = 
{ 
    "• Osnove C# jezika: Razumevanje objektno-orijentisanog programiranja, klasa i metoda za razvoj sistema.\n" +
    "• Godot Engine 4: Upravljanje čvorovima, rad sa signalima i optimizacija fizike u realnom vremenu.\n" +
    "• Arhitektura igara: Organizacija koda kroz kompoziciju i efikasno upravljanje scenama i stanjima.",

    "• Mikroekonomija: Analiza ponašanja učesnika na tržištu i optimizacija oskudnih resursa na farmi.\n" +
    "• Makroekonomija: Proučavanje globalnih trendova, inflacije i uloge centralnih banaka u stabilnosti.\n" +
    "• Tržište kapitala: Razumevanje instrumenata investiranja, rizika i dinamike berzanskog poslovanja.",

    "• Renesansa: Majstorstvo perspektive, proučavanje ljudske anatomije i povratak klasičnim idealima.\n" +
    "• Barok: Dinamične kompozicije, korišćenje dramatičnog kontrasta svetlosti i naglašena emocija.\n" +
    "• Modernizam: Prekid sa tradicijom kroz eksperimentisanje formom, bojom i novim medijima.",

    "• Akademsko pisanje: Usavršavanje strukture eseja, kritičkog osvrta i veština formalne korespondencije.\n" +
    "• Književna analiza: Istraživanje ključnih dela koja su oblikovala savremenu misao i kulturu.\n" +
    "• Poslovna komunikacija: Savladavanje stručne terminologije iz oblasti tehnologije i međunarodnog biznisa."
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

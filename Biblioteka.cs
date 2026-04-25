using Godot;
using System;
using System.Collections.Generic;

public partial class Biblioteka : CanvasLayer
{
	// Poveži ove nodove u Editoru (prevuci ih iz Scene stabla u Inspector slotove)
	[Export] private Control _meniLista;       // VBoxContainer ili GridContainer unutar ScrollContainer-a
	[Export] private Control _prikazSadrzaja;  // Control čvor koji drži RichTextLabel i Back dugme
	[Export] private RichTextLabel _tekstBeleske;

	public override void _Ready()
	{
		// Osiguraj da je sve sakriveno kada se igra pokrene
		Hide();
		if (_prikazSadrzaja != null) _prikazSadrzaja.Hide();
		if (_meniLista != null) _meniLista.Show();
	}

	// Ovu metodu pozivaš iz glavne skripte (npr. kad igrač klikne na sto/biblioteku)
	public void OtvoriBiblioteku(Dictionary<string, string> podaci)
	{
		Show();
		_prikazSadrzaja.Hide();
		_meniLista.Show();
		PripremiBeleske(podaci);
	}

	private void PripremiBeleske(Dictionary<string, string> podaci)
	{
		// Čišćenje stare liste
		foreach (Node child in _meniLista.GetChildren())
		{
			child.QueueFree();
		}

		if (podaci == null || podaci.Count == 0)
		{
			GD.Print("Biblioteka: Nema otključanih beleški.");
			return;
		}

		foreach (var stavka in podaci)
		{
			Button dugme = new Button();
			dugme.Text = stavka.Key; // Naziv kursa (npr. "Matematika 1")
			
			// Koristimo lambda sa lokalnom promenljivom da izbegnemo closure problem
			string tekstZaPrikaz = stavka.Value;
			dugme.Pressed += () => OtvoriSvesku(tekstZaPrikaz);
			
			_meniLista.AddChild(dugme);
		}
	}

	private void OtvoriSvesku(string tekst)
	{
		_meniLista.Hide();
		_prikazSadrzaja.Show();
		
		// BBCode omogućava da u beleškama koristiš [b]bold[/b], [i]italic[/i], itd.
		_tekstBeleske.BbcodeEnabled = true;
		_tekstBeleske.Text = tekst;
	}

	// Poveži "Nazad" dugme (unutar prikaza sadržaja) sa ovom metodom preko signala ili koda
	public void _on_back_button_pressed()
	{
		_prikazSadrzaja.Hide();
		_meniLista.Show();
	}

	// Poveži "X" dugme (za izlaz iz cele biblioteke) sa ovom metodom
	public void _on_close_button_pressed()
	{
		Hide();
	}
}

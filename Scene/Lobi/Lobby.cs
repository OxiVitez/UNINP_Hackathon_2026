using Godot;
using System;

public partial class Lobby : Node2D
{
    // Reference na nodove (koristi @onready za automatsko povezivanje)
    [Export] private TextureButton board1;
    [Export] private TextureButton board2;
    [Export] private TextureButton board3;
    [Export] private Panel uiPopup;
    [Export] private Label uiLabel;
    [Export] private Button closeButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Poveži signale od TextureButton-ova na funkcije
        board1.Pressed += () => ShowUI("Oglas 1: Kupite naš proizvod!");
        board2.Pressed += () => ShowUI("Oglas 2: Specijalna ponuda!");
        board3.Pressed += () => ShowUI("Oglas 3: Novi level dostupan!");

        // Poveži signal za zatvaranje UI-a
        closeButton.Pressed += HideUI;
    }

    // Funkcija za prikazivanje UI-a sa custom tekstom
    private void ShowUI(string message)
    {
        uiLabel.Text = message;
        uiPopup.Visible = true;
    }

    // Funkcija za skrivanje UI-a
    private void HideUI()
    {
        uiPopup.Visible = false;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Ovde možeš dodati animacije ili druge efekte ako treba
    }
}
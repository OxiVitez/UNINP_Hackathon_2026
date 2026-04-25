using Godot;
using System;

public partial class NpcBaza : CharacterBody2D
{
    [Export] private Area2D zonaInterakcije;

    // SADA koristimo custom tip
    [Export] public DialogLinija[] linije = Array.Empty<DialogLinija>();

    [Export] private NodePath dijalogUIPath;

    private bool igracUnutarZone = false;
    private int trenutnaLinija = 0;
    private bool dijalogjOtvoren = false;
    private DialogUi dijalogUI;

    public override void _Ready()
    {
        zonaInterakcije.BodyEntered += OnBodyEntered;
        zonaInterakcije.BodyExited += OnBodyExited;

        if (dijalogUIPath != null && !dijalogUIPath.IsEmpty)
        {
            dijalogUI = GetNode<DialogUi>(dijalogUIPath);
        }
        else
        {
            dijalogUI = GetTree().Root.FindChild("DijalogUI", true, false) as DialogUi;
        }

        if (dijalogUI == null)
            GD.PrintErr("DijalogUI nije pronađen!");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (igracUnutarZone && Input.IsActionJustPressed("Interakcija"))
        {
            if (linije.Length == 0)
            {
                GD.PrintErr("Niz linije je prazan!");
                return;
            }

            if (dijalogUI == null)
            {
                GD.PrintErr("DijalogUI nije povezan!");
                return;
            }

            if (!dijalogjOtvoren)
                ZapocniDijalog();
            else
                SledecaLinija();
        }
    }

    private void ZapocniDijalog()
    {
        if (linije.Length == 0) return;

        dijalogjOtvoren = true;
        trenutnaLinija = 0;
        dijalogUI?.PrikaziLiniju(linije[trenutnaLinija]);
    }

    private void SledecaLinija()
    {
        trenutnaLinija++;

        if (trenutnaLinija >= linije.Length)
        {
            ZatvoriDijalog();
            return;
        }

        dijalogUI?.PrikaziLiniju(linije[trenutnaLinija]);
    }

    private void ZatvoriDijalog()
    {
        dijalogjOtvoren = false;
        trenutnaLinija = 0;
        dijalogUI?.SakriDijalog();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Karakterbaza)
            igracUnutarZone = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is Karakterbaza)
        {
            igracUnutarZone = false;
            if (dijalogjOtvoren)
                ZatvoriDijalog();
        }
    }
}
using Godot;
using System;

public partial class LoadScenu : Area2D
{
    [Export] private string putanjaScene = "res://Scene/Sobe/odnik.tscn";

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Karakterbaza)
        {
            CallDeferred(MethodName.UcitajScenu);
        }
    }

    private void UcitajScenu()
    {
        if (string.IsNullOrEmpty(putanjaScene))
        {
            GD.PrintErr("Putanja scene nije postavljena!");
            return;
        }
        GetTree().ChangeSceneToFile(putanjaScene);
    }
}
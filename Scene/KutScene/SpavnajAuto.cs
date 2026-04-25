using Godot;
using System;

public partial class SpavnajAuto : Area2D
{
    [Export] private PackedScene AutoScene;
    [Export] private Marker2D SpawnMarker;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Karakterbaza)
        {
            // Spawna auto na poziciji markera
            if (AutoScene != null && SpawnMarker != null)
            {
                Node2D auto = AutoScene.Instantiate<Node2D>();
                GetTree().CurrentScene.AddChild(auto);
                auto.GlobalPosition = SpawnMarker.GlobalPosition;
            }

            // Obrisi ovu scenu nakon spawna
            QueueFree();
        }
    }
}
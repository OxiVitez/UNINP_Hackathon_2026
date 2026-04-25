using Godot;
using System;

public partial class AutoIdeUlevo : Sprite2D
{
    [Export] private float brzina = 200.0f;

    public override void _Process(double delta)
    {
        Position += Vector2.Left * brzina * (float)delta;

        // Obrisi auto kad izadje van ekrana
        if (Position.X < -500)
            QueueFree();
    }
}
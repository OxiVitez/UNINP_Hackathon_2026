using Godot;
using System;

public partial class PomeriKameru : Area2D
{
    [Export] private Camera2D kamera;
    [Export] private Marker2D pozicijaA;
    [Export] private Marker2D pozicijaB;
    [Export] private float brzinaPomeranja = 500.0f;

    private bool naPozcicijiA = true;
    private Vector2 trenutniOffset;
    private Vector2 targetOffset;
    private bool seKreceKamera = false;

    public override void _Ready()
    {
        this.BodyEntered += OnBodyEntered;
        // Izracunaj offset izmedju dve pozicije
        trenutniOffset = Vector2.Zero;
        targetOffset = Vector2.Zero;
        kamera.Offset = Vector2.Zero;
    }

    public override void _Process(double delta)
    {
        if (seKreceKamera)
        {
            trenutniOffset = trenutniOffset.MoveToward(targetOffset, brzinaPomeranja * (float)delta);
            kamera.Offset = trenutniOffset;

            if (trenutniOffset.DistanceTo(targetOffset) < 1.0f)
            {
                trenutniOffset = targetOffset;
                kamera.Offset = targetOffset;
                seKreceKamera = false;
            }
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Karakterbaza)
        {
            if (naPozcicijiA)
            {
                // Offset = razlika izmedju B i A pozicije
                targetOffset = pozicijaB.GlobalPosition - pozicijaA.GlobalPosition;
                naPozcicijiA = false;
            }
            else
            {
                targetOffset = Vector2.Zero;
                naPozcicijiA = true;
            }

            seKreceKamera = true;
        }
    }
}
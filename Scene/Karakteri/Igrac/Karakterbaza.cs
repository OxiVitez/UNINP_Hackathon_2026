using Godot;
using System;

public partial class Karakterbaza : CharacterBody2D
{
    [Export] public float Speed = 300.0f;
    [Export] private AnimatedSprite2D animacija;

    private Vector2 lastDirection = Vector2.Down;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 inputDir = Vector2.Zero;

        if (Input.IsActionPressed("ui_right")) inputDir.X += 1;
        if (Input.IsActionPressed("ui_left"))  inputDir.X -= 1;
        if (Input.IsActionPressed("ui_down"))  inputDir.Y += 1;
        if (Input.IsActionPressed("ui_up"))    inputDir.Y -= 1;

        bool isMoving = inputDir != Vector2.Zero;

        if (isMoving)
        {
            Velocity = inputDir.Normalized() * Speed;
            lastDirection = inputDir.Normalized();

            if (inputDir.X != 0)
                animacija.FlipH = inputDir.X > 0;

            PustiAnimacijuKretanja(inputDir);  // <-- FIX: bio razmak
        }
        else
        {
            Velocity = Vector2.Zero;
            PustiIdleAnimaciju(lastDirection);
        }

        MoveAndSlide();
    }

    private void PustiAnimacijuKretanja(Vector2 dir)
    {
        string naziv;

        bool ideDole   = dir.Y > 0;
        bool ideGore   = dir.Y < 0;
        bool ideStrana = dir.X != 0;

        if (ideStrana && ideDole)
            naziv = "SetaUgaoDole";
        else if (ideStrana && ideGore)
            naziv = "SetaUgaoGore";
        else if (ideStrana)
            naziv = "SetaUstranu";
        else if (ideDole)
            naziv = "SetaDole";
        else
            naziv = "SetaGore";

        if (animacija.Animation != naziv)
            animacija.Play(naziv);
    }

    private void PustiIdleAnimaciju(Vector2 dir)
    {
        string naziv;

        bool ideDole   = dir.Y > 0.1f;
        bool ideGore   = dir.Y < -0.1f;
        bool ideStrana = Mathf.Abs(dir.X) > 0.1f;

        if (ideStrana && ideDole)
            naziv = "IdleStrana dole";
        else if (ideStrana && ideGore)
            naziv = "IdleStranaGore";
        else if (ideStrana)
            naziv = "IdleLevo_Desno";
        else if (ideDole)
            naziv = "IdleUnapred";
        else
            naziv = "IdleUnazad";

        if (animacija.Animation != naziv)
            animacija.Play(naziv);
    }
}
using Godot;
using System;

public partial class OglasnaTabla : Control
{
    [Export] private Button Dugme1;
    [Export] private Button Dugme2;
    [Export] private Sprite2D OglasnaTablaSprite1;
    [Export] private Sprite2D OglasnaTablaSprite2;
    [Export] private Area2D OglasnaZona1;
    [Export] private Area2D OglasnaZona2;
    [Export] private string putanjaScene1 = "res://Scenes/NovaScena.tscn";
    [Export] private string putanjaScene2 = "";
    [Export] private float velicinaHover = 1.15f;
    [Export] private float brzinaTween = 0.2f;
	[Export] private FadeIn fadeIn;

    private Vector2 originalnaVelicina1;
    private Vector2 originalnaVelicina2;

    public override void _Ready()
    {
    originalnaVelicina1 = OglasnaTablaSprite1.Scale;
    originalnaVelicina2 = OglasnaTablaSprite2.Scale;

    // Omoguci pracenje misa na Area2D
    OglasnaZona1.InputPickable = true;
    OglasnaZona2.InputPickable = true;

    OglasnaZona1.MouseEntered += () => AnimirajSprite(OglasnaTablaSprite1, originalnaVelicina1 * velicinaHover);
    OglasnaZona1.MouseExited  += () => AnimirajSprite(OglasnaTablaSprite1, originalnaVelicina1);

    OglasnaZona2.MouseEntered += () => AnimirajSprite(OglasnaTablaSprite2, originalnaVelicina2 * velicinaHover);
    OglasnaZona2.MouseExited  += () => AnimirajSprite(OglasnaTablaSprite2, originalnaVelicina2);

    Dugme1.Pressed += () => UcitajScenu(putanjaScene1);
    Dugme2.Pressed += () => UcitajScenu(putanjaScene2);
    }

    private void AnimirajSprite(Sprite2D sprite, Vector2 targetScale)
    {
        var tween = CreateTween();
        tween.TweenProperty(sprite, "scale", targetScale, brzinaTween)
             .SetTrans(Tween.TransitionType.Sine)
             .SetEase(Tween.EaseType.Out);
    }

    private void UcitajScenu(string putanja)
    {
        if (string.IsNullOrEmpty(putanja))
        {
            GD.PrintErr("Putanja scene nije postavljena!");
            return;
        }
		fadeIn.PokreniAnimaciju();
		GetTree().CreateTimer(1.7).Timeout += () => GetTree().ChangeSceneToFile(putanja);  
    }
}
using Godot;
using System;

public partial class DialogUi : CanvasLayer
{
    [Export] private Panel panel;
    [Export] private Label tekstLinije;
    [Export] private Label napomenaPritisni;

    // NOVO
    [Export] private TextureRect slikaNPC;

    public override void _Ready()
    {
        SakriDijalog();
    }

    public void PrikaziLiniju(DialogLinija linija)
    {
        panel.Visible = true;
        tekstLinije.Text = linija.Tekst;
        napomenaPritisni.Text = "[E] Nastavi";

        if (linija.Sprite != null)
        {
            slikaNPC.Texture = linija.Sprite;
            slikaNPC.Visible = true;
        }
        else
        {
            slikaNPC.Visible = false;
        }
    }

    public void SakriDijalog()
    {
        panel.Visible = false;
        tekstLinije.Text = "";
        slikaNPC.Visible = false;
    }
}
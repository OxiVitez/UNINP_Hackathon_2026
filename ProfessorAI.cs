using Godot;
using System;

public partial class ProfessorAI : CharacterBody2D
{
	[Export]
	public Label textProfesor;

	public override void _PhysicsProcess(double delta)
	{
		textProfesor.Text = "Hola, soy el profesor de este juego. Mi función es ayudarte a entender cómo funciona el juego y darte consejos para que puedas avanzar. Si tienes alguna pregunta, no dudes en preguntarme.";
	}
}

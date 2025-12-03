using Godot;
using System;

public partial class Hud : Control
{
    [Export] private Label labelFPS;

    public override void _Process(double delta)
    {
        labelFPS.Text = "" + Engine.GetFramesPerSecond();
    }
}
using Godot;
using System;

public partial class Menu : Control
{
    //SELECTION
    private readonly int FPSMin = 5; //minimum framerate to run at
    private int FPSMaxUser = 0; //0 = auto; 1001 = uncapped

    //HARDWARE
    private double FPSAverageSlow = 0d;
    private double FPSAverageSlowPrevious = 0d;

    public override void _Ready()
    {
        //Set default update rate to the screen refresh rate - this method must be called once to display the user's refresh rate
        SetFramerateManually(0);

        //Set default mouse mode
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("escape"))
        {
            //Toggle menu
            Visible = !Visible;

            //Mouse mode
            if (!Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
        }
    }

    public override void _PhysicsProcess(double deltaDouble)
    {
        //Slow update
        if
        (
            (FPSMaxUser <= 0) //auto fps selected
            && Time.GetTicksMsec() > 6000f //wait until the physics engine is ready
            && (
                (int)Engine.GetPhysicsFrames() % Engine.PhysicsTicksPerSecond == 0 //check every 1 second
            )
        )
        {
            SetFramerateAutomatically();
        }
    }

    private void SetFramerateAutomatically()
    {
        //This method likely runs in a slow update

        //Get average fps
        if (FPSAverageSlowPrevious >= 0.0) //wait until initialized
        {
            FPSAverageSlow = (FPSAverageSlowPrevious + Engine.GetFramesPerSecond()) / 2.0;
        }
        FPSAverageSlowPrevious = Engine.GetFramesPerSecond();
    }

    private void SetFramerateManually(float val)
    {
        GD.Print($"SetFramerateManually({val})");

        FPSMaxUser = (int)val;

        //A value of 0 indicates to automatically set the update rate
        if (FPSMaxUser <= 0)
        {
            //Automatic framerate
            Engine.MaxFps = (int)Mathf.Max(FPSMin, DisplayServer.ScreenGetRefreshRate());
        }
        else if (FPSMaxUser >= 1001)
        {
            //Uncapped framerate
            Engine.MaxFps = 0;
        }
        else
        {
            //Manual framerate
            Engine.MaxFps = Math.Max(FPSMin, FPSMaxUser);
            Engine.PhysicsTicksPerSecond = Mathf.Max(FPSMin, FPSMaxUser);
        }
    }

    //Quit
    public void OnButtonQuitPressed()
    {
        GetTree().Quit();
    }
}
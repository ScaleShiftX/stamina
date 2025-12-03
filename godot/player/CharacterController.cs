using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterController : RigidBody3D
{
    //DIAGNOSTICS
    [Export] private VBoxContainer _diagnosticsVBoxContainer;
    [Export] private CsgBox3D _diagnosticsCsgVector;
    [Export] private CsgBox3D _diagnosticsCsgRotation;

    //ANIMATION
    private Vector3 _animationNormal = Vector3.Forward;

    //LOOK
    [Export] private Node3D _cameraPlayerPivot;
    private float _mouseSensitivity = 0.00075f;

    //MOVEMENT
    //Surface
    private enum Surface
    {
        Air,
        Slope,
        Flat
    }
    private Surface _surfaceOn = Surface.Air;
    private Surface _surfaceWishingInto = Surface.Air;
    private Vector3? _surfaceOnNormal = null;
    private Vector3? _surfaceWishingIntoNormal = null;
    public const float SlopeDotUp = 0.70710678118f; //~= 45 deg. What angle is a flat surface
                                                    //LESS THAN this values is a slope and engages climbing/wallrunning
                                                    //This is the dot product of the normal of the surface to global up
                                                    //-1 is down, 0 is toward the horizon, 1 is up
                                                    //cos(angle) = dot :. angle = cos^-1(dot)

    //Thrust
    private Vector3 _thrustDirection = Vector3.Zero;
    private const float ThrustMagnitude = 150f; //250f;
    private const float ThrustMagnitudeOnFlatCoefficient = 1f;
    private const float ThrustMagnitudeOnSlopeCoefficient = 1f;
    private const float ThrustMagnitudeInAirCoefficient = 0.002f;
    private const float ThrustMagnitudeCrouchedCoefficient = 0.05f;

    //Jump
    private const float JumpVSpeed = 10f;
    private bool _isJumpedAndStillOnSurface = false;
    private float _jumpedResetForcibly = 0f;
    private const float JumpedResetForciblyPeriod = 1f; //how long in seconds after a jump is the ability to jump re-enabled, even if the player never left the ground

    //Drag
    private const float Drag = 20f;
    private const float DragOnFlatCoefficient = 1f;
    private const float DragOnSlopeAndWishingIntoCoefficient = 0.25f; //deprecated - was for wall-climbing
    private const float DragOnSlopeCoefficient = 0.01f;
    private const float DragInAirCoefficient = 0f; //0.01f;
    private const float DragSlideCoefficient = 0.05f;
    private const float DragOnFlatStatic = 0.8f; //the speed below which the player's friction will be raised to very high levels to stop them from slipping,
                                                 //as long as they are on a flat and aren't trying to thrust
                                                 //0.68f was the last measured slipping speed when standing on something that's barely a flat rather than a slope
    
    public override void _Input(InputEvent @event)
    {
        //Look
        if (@event is InputEventMouseMotion mouseMotion)
        {
            //Yaw
            _cameraPlayerPivot.Rotation = new Vector3(
                _cameraPlayerPivot.Rotation.X,
                _cameraPlayerPivot.Rotation.Y - mouseMotion.Relative.X * _mouseSensitivity,
                _cameraPlayerPivot.Rotation.Z
            );

            //Pitch, clamp to straight up or down
            _cameraPlayerPivot.Rotation = new Vector3(
                Mathf.Clamp(_cameraPlayerPivot.Rotation.X - mouseMotion.Relative.Y * _mouseSensitivity,
                    -0.24f * Mathf.Tau,
                    0.24f * Mathf.Tau
                ),
                _cameraPlayerPivot.Rotation.Y,
                _cameraPlayerPivot.Rotation.Z
            );
        }
    }

    public override void _Process(double deltaDouble)
    {
        //DIAGNOSTICS
        //CSGs

        //Vector
        _diagnosticsCsgVector.GlobalPosition = GlobalPosition + (_thrustDirection * 3f);


        //Labels
        Label diagnosticsLabel1 = _diagnosticsVBoxContainer.GetChild<Label>(1);
        if (_surfaceOn == Surface.Flat)
        {
            diagnosticsLabel1.Text = "Surface on: Flat";
        }
        else if (_surfaceOn == Surface.Slope)
        {
            diagnosticsLabel1.Text = "Surface on: Slope";
        }
        else //if (_surfaceOn == Surface.Air)
        {
            diagnosticsLabel1.Text = "Surface on: Air";
        }

        _diagnosticsVBoxContainer.GetChild<Label>(2).Text = $"_surfaceWishingIntoNormal: {_surfaceWishingIntoNormal}";
        _diagnosticsVBoxContainer.GetChild<Label>(3).Text = $"_thrustDirection: {_thrustDirection}";
        _diagnosticsVBoxContainer.GetChild<Label>(4).Text = $"_animationNormal: {_animationNormal}";
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        float delta = state.Step;

        //THRUST
        //Thrust direction
        UpdateThrustDirection(state);

        //Thrust surface factor
        float thrustMagnitude = GetThrustPerSurface();

        //Thrust sum
        Vector3 thrustVector = _thrustDirection * thrustMagnitude;

        //Integrate drag and apply to velocity
        float drag = GetDrag();
        if (drag != 0f)
        {
            float decayFactor = Mathf.Exp(-drag * delta);

            //Update velocity
            LinearVelocity = (LinearVelocity * decayFactor) + (thrustVector / drag * (1f - decayFactor));
        }
        else
        {
            //Update velocity
            LinearVelocity += thrustVector;
        }

        //JUMP
        //Manage
        _jumpedResetForcibly = Mathf.Max(0f, _jumpedResetForcibly - delta);
        if ((_surfaceOn == Surface.Air && _surfaceWishingInto != Surface.Slope) || _jumpedResetForcibly == 0f)
        {
            _isJumpedAndStillOnSurface = false;
        }

        //Jump
        if (Input.IsActionPressed("move_jump") && !_isJumpedAndStillOnSurface //wish
            && (
                (_surfaceOn == Surface.Flat) //flat
                || (true //SlopeMovementTimeRemaining > 0f //slope //deprecated
                    && (
                        _surfaceOn == Surface.Slope
                        || (_surfaceOn == Surface.Air && _surfaceWishingInto == Surface.Slope)
                    )
                )
            )
        )
        {
            //Prevent geting extra height
            _isJumpedAndStillOnSurface = true;
            _jumpedResetForcibly = JumpedResetForciblyPeriod;

            //Deprecated
            //Stop sliding!
            //IsSliding = false;

            //Direction
            if (_surfaceOn != Surface.Flat)
            {
                //Walljump
                //Direction = slope normal
                Vector3 jumpDirection = (_surfaceWishingIntoNormal == null) ? (Vector3)_surfaceOnNormal : (Vector3)_surfaceWishingIntoNormal;

                //Direction += up
                jumpDirection = (jumpDirection + Vector3.Up).Normalized();

                //Wallrunning/climbing deprecated
                //Tire
                //SlopeMovementTimeRemaining = Mathf.Max(0f, SlopeMovementTimeRemaining - SlopeMovementTimeJumpPenalty);

                //Impulse
                LinearVelocity += jumpDirection * JumpVSpeed;

                GD.Print("Jump!");
            }
            else
            {
                //Set vertical speed directly for consistent bhopping
                LinearVelocity = new Vector3(
                    LinearVelocity.X,
                    Mathf.Max(LinearVelocity.Y + JumpVSpeed, JumpVSpeed), //VSpeed will reset if negative, otherwise add to it
                    LinearVelocity.Z
                );
            }
        }
    }

    private void UpdateThrustDirection(PhysicsDirectBodyState3D state)
    {
        Vector3 wishDirectionRaw = GetWishDirectionRaw();
        Vector3 wishDirectionTangentToUp = (wishDirectionRaw - (Vector3.Up * wishDirectionRaw.Dot(Vector3.Up))).Normalized();

        UpdateCollisionDetection(state, wishDirectionTangentToUp);

        //By default align to up
        _thrustDirection = wishDirectionTangentToUp;

        //Wishing into a surface?
        if (_surfaceWishingIntoNormal.HasValue)
        {
            //Align to surface
            _thrustDirection = (wishDirectionRaw - ((Vector3)_surfaceWishingIntoNormal * wishDirectionRaw.Dot((Vector3)_surfaceWishingIntoNormal))).Normalized();
        }
        else
        {
            //Not wishing into a surface
            if (_surfaceOn == Surface.Flat)
            {
                //On a flat surface
                //Redirect so we can walk downhill
                _thrustDirection = (wishDirectionTangentToUp - ((Vector3)_surfaceOnNormal * wishDirectionTangentToUp.Dot((Vector3)_surfaceOnNormal))).Normalized();
            }
        }
    }

    private Vector3 GetWishDirectionRaw()
    {
        Vector3 wishDirectionRaw = Vector3.Zero;
        if (Input.IsActionPressed("move_forward")) wishDirectionRaw -= _cameraPlayerPivot.GlobalBasis.Z;
        if (Input.IsActionPressed("move_left")) wishDirectionRaw -= _cameraPlayerPivot.GlobalBasis.X;
        if (Input.IsActionPressed("move_right")) wishDirectionRaw += _cameraPlayerPivot.GlobalBasis.X;
        if (Input.IsActionPressed("move_back")) wishDirectionRaw += _cameraPlayerPivot.GlobalBasis.Z;
        return wishDirectionRaw;
    }

    private void UpdateCollisionDetection(PhysicsDirectBodyState3D state, Vector3 wishDirectionTangentToUp)
    {
        float checkingSurfaceOnNormalDotWishSmallest = 1f;
        float checkingSurfaceWishingIntoNormalDotWishSmallest = 1f;

        //Defaults
        _surfaceOn = Surface.Air;
        _surfaceOnNormal = null;
        _surfaceWishingInto = Surface.Air;
        _surfaceWishingIntoNormal = null;
        _animationNormal = Vector3.Up;

        //Collision detection
        int contactCount = state.GetContactCount();
        if (contactCount > 0)
        {
            for (int i = 0; i < contactCount; i++)
            {
                Vector3 checkingNormal = state.GetContactLocalNormal(i);
                Basis checkingBasis = ((Node3D)state.GetContactColliderObject(i)).GlobalBasis;
                Vector3 checkingNormalGlobal = checkingNormal * checkingBasis; //diagnositc

                //Standing on
                //Prefer flattest
                float standOnDot = Vector3.Down.Dot(checkingNormal);
                if (standOnDot < 0f && standOnDot < checkingSurfaceOnNormalDotWishSmallest)
                {
                    //Update smallest dot so far
                    checkingSurfaceOnNormalDotWishSmallest = standOnDot;

                    //Normal
                    _surfaceOnNormal = checkingNormal;

                    //Surface
                    if (Vector3.Up.Dot(checkingNormal) < SlopeDotUp)
                    {
                        _surfaceOn = Surface.Slope;
                    }
                    else
                    {
                        _surfaceOn = Surface.Flat;
                    }
                }

                //Wishing into
                //Prefer aligned to wish
                float wishIntoDot = wishDirectionTangentToUp.Dot(checkingNormal);
                if (wishIntoDot < 0f && wishIntoDot < checkingSurfaceWishingIntoNormalDotWishSmallest)
                {
                    //Update smallest dot so far
                    checkingSurfaceWishingIntoNormalDotWishSmallest = wishIntoDot;

                    //Normal
                    _surfaceWishingIntoNormal = checkingNormal;
                    _animationNormal = checkingNormal;

                    //Surface
                    if (Vector3.Up.Dot(checkingNormal) < SlopeDotUp)
                    {
                        _surfaceWishingInto = Surface.Slope;
                    }
                    else
                    {
                        _surfaceWishingInto = Surface.Flat;
                    }
                }
            }
        }
    }

    private float GetThrustPerSurface()
    {
        float acceleration = ThrustMagnitude;

        //Surface
        if (_surfaceOn == Surface.Slope || (_surfaceOn == Surface.Air && _surfaceWishingInto == Surface.Slope))
        {
            //Slope movement
            acceleration *= ThrustMagnitudeOnSlopeCoefficient;
        }
        else if (_surfaceOn == Surface.Air)
        {
            //Aerial movement
            acceleration *= ThrustMagnitudeInAirCoefficient;
        }
        else if (_surfaceOn == Surface.Flat)
        {
            //Flat movement
            acceleration *= ThrustMagnitudeOnFlatCoefficient;
        }

        return acceleration;
    }

    private float GetDrag()
    {
        float drag = Drag;

        //Surface
        if (_surfaceOn == Surface.Slope || (_surfaceOn == Surface.Air && _surfaceWishingInto == Surface.Slope))
        {
            ////Deprecated wall-climbing
            //Slope
            //if (SurfaceWishingInto == Surface.Slope && SlopeMovementTimeRemaining > 0f)
            //{
            //    //Slope movement
            //    drag *= DragOnSlopeAndWishingIntoCoefficient;
            //}
            //else
            //{
            //Sliding along slope
            drag *= DragOnSlopeCoefficient;
            //}
        }
        else if (_surfaceOn == Surface.Flat && !_isJumpedAndStillOnSurface)
        {
            //Flat
            drag *= DragOnFlatCoefficient;
        }
        else if (_surfaceOn == Surface.Air)
        {
            //Aerial
            drag *= DragInAirCoefficient;
        }

        ////Deprecated Slide
        //if (IsSliding)
        //{
        //    drag *= DragSlideCoefficient;
        //}

        return drag;
    }

    private static void LookAtVector(ref Basis basisLocalToRotate, Basis basisGlobalToRotate, Vector3 startPosition, Vector3 targetPosition, float interpolation)
    {
        //Rotate in the direction of the target position
        //Get direction
        Vector3 direction = (targetPosition - startPosition).Normalized();
        //Choose forward vector
        Vector3 forward = -basisGlobalToRotate.Z;
        //Get rotation from current forward to target direction
        Quaternion rotation = new(forward, direction);
        //Interpolate to prevent snapping
        Quaternion interpolatedRotation = new Quaternion(basisLocalToRotate).Slerp(rotation, interpolation);
        //Apply rotation
        basisLocalToRotate = new(interpolatedRotation);
    }

    //
}
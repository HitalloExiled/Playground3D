using System.Collections.Generic;
using System.Linq;
using Godot;

public class DummyPlayer : CharacterController3D
{
    #region Constants
    const float FLOOR_DETECTION_LENGHT = 1f;
    #endregion Constants

    #region Fields
    private DebugDrawer debugDrawer;
    private bool    automove;
    private bool    automoveLeft;
    private bool    automoveRight;
    private bool    automoveUp;
    private bool    automoveDown;
    private bool    automoveInvert;
    private float   automoveTimer;
    private float   constantJumpTime;
    private float   jumpTime;
    private float   jumpVelocity;
    private Label   label;
    private float   targetJumpTime;
    private readonly List<string> messages = new List<string>();
    #endregion Fields

    #region Exports
    [Export]
    public float JumpHeight = 1f;

    [Export]
    public float Speed = 4f;
    #endregion Exports

    #region Hooks
    public override void _Ready()
    {
        this.label = this.GetNode<Label>("Control/Label");

        this.debugDrawer = ResourceLoader.Load<PackedScene>("res://Assets/DebugDrawer/DebugDrawer.tscn").Instance<DebugDrawer>();

        this.GetTree().Root.CallDeferred("add_child", this.debugDrawer);

        // this.TopCollisionShape    = this.GetNode<CollisionShape>("TopCollisionShape");
        // this.BottomCollisionShape = this.GetNode<CollisionShape>("BottomCollisionShape");
    }

    public override void _PhysicsProcess(float delta)
    {
        this.messages.Clear();

        this.LinearVelocity = this.HandleInputs(delta);

        // var collisions = new List<CollisionContact>(); //this.MoveAndSlide();
        var collisions = this.MoveAndSlide();
        // this.LinearVelocity = this.MoveAndSlideWithSnap(this.LinearVelocity + this.Gravity * this.GravityForce * delta, this.Snap, Vector3.Up);

        this.debugDrawer.Clear();

        var origin         = this.GlobalTransform.origin;
        var topPosition    = origin + -this.Gravity * this.BodySize / 2;
        var bottomPosition = origin + this.Gravity  * this.BodySize / 2;

        this.debugDrawer.DrawLine(topPosition,    topPosition    + Vector3.Up   * 0.5f, new Color(255, 0, 255));
        this.debugDrawer.DrawLine(bottomPosition, bottomPosition + Vector3.Down * 0.5f, new Color(255, 0, 255));

        foreach (var collision in collisions)
        {
            this.debugDrawer.DrawLine(collision.Position, collision.Position + collision.Normal, new Color(0, 0, 255));
        }

        this.Log($"Origin:         {this.GlobalTransform.origin}");
        this.Log($"Motion:         {this.LinearVelocity}");
        this.Log($"Floor Velocity: {Mathf.Round(this.FloorVelocity.Length())}ms, {this.FloorVelocity}");
        this.Log($"Floor Normal:   {Mathf.Round(Mathf.Rad2Deg(Vector3.Up.AngleTo(this.FloorNormal)))}°, {this.FloorNormal}");
        this.Log($"Velocity:       {Mathf.Round(this.LinearVelocity.Length())}ms, {this.LinearVelocity}");
        this.Log($"Collisions:     {collisions.Count()}");
        this.Log($"Is On Ceiling:  {this.CollisionState.HasFlag(CollisionState.Top)}");
        this.Log($"Is On Wall:     {this.CollisionState.HasFlag(CollisionState.Sides)}");
        this.Log($"Is On Floor:    {this.CollisionState.HasFlag(CollisionState.Bottom)}");
        this.Log($"Is Sliding:     {this.CollisionState.HasFlag(CollisionState.Sliding)}");

        this.label.Text = string.Join("\n", this.messages.Distinct().ToArray());
    }
    #endregion Hooks

    private void Log(string message)
    {
        this.messages.Add(message);
    }

    private Vector3 HandleInputs(float delta)
    {
        var verticalVelocity   = Vector3.Zero;
        var horizontalVelocity = Vector3.Zero;

        // if (Input.IsActionJustPressed("ui_up"))
        // {
        //     this.automoveLeft  = false;
        //     this.automoveRight = false;
        //     this.automoveUp    = false;
        //     this.automoveDown  = false;
        //     this.automove      = !this.automove;
        // }

        if (Input.IsActionJustPressed("ui_left"))
        {
            this.automove      = false;
            this.automoveRight = false;
            this.automoveLeft  = !this.automoveLeft;
        }

        if (Input.IsActionJustPressed("ui_right"))
        {
            this.automove      = false;
            this.automoveLeft  = false;
            this.automoveRight = !this.automoveRight;
        }

        if (Input.IsActionJustPressed("ui_up"))
        {
            this.automove     = false;
            this.automoveDown = false;
            this.automoveUp   = !this.automoveUp;
        }

        if (Input.IsActionJustPressed("ui_down"))
        {
            this.automove      = false;
            this.automoveUp  = false;
            this.automoveDown = !this.automoveDown;
        }

        if (this.automove)
        {
            if (automoveTimer >= 0.5f)
            {
                this.automoveInvert = !this.automoveInvert;
                this.automoveTimer = 0;
            }

            horizontalVelocity.x = (this.automoveInvert ? -1 : 1);

            this.automoveTimer += delta;
        }
        else if (this.automoveLeft)
        {
            horizontalVelocity.x = -1;
        }
        else if (this.automoveRight)
        {
            horizontalVelocity.x = 1;
        }

        if (this.automoveUp)
        {
            horizontalVelocity.z = -1;
        }
        else if (this.automoveDown)
        {
            horizontalVelocity.z = 1;
        }

        if (Input.IsActionPressed("left"))
        {
            horizontalVelocity.x = -1;
        }

        if (Input.IsActionPressed("right"))
        {
            horizontalVelocity.x = 1;
        }

        if (Input.IsActionPressed("up"))
        {
            horizontalVelocity.z = -1;
        }

        if (Input.IsActionPressed("down"))
        {
            horizontalVelocity.z = 1;
        }

        if (this.CollisionState != CollisionState.None)
        {
            this.Snap     = true;
            this.jumpTime = 0;
        }

        if (Input.IsActionJustPressed("jump"))
        {
            verticalVelocity.y = this.jumpVelocity = Mathf.Sqrt(-2 * -this.GravityForce * this.JumpHeight / 2);

            this.targetJumpTime   = verticalVelocity.y / -this.GravityForce;
            this.constantJumpTime = this.JumpHeight / 2 / this.jumpVelocity;
            this.jumpTime        += delta;

            this.Snap = false;
        }
        else if (this.jumpVelocity > 0)
        {
            if (Input.IsActionPressed("jump") && this.jumpTime < this.constantJumpTime)
            {
                verticalVelocity.y = this.jumpVelocity;

                this.jumpTime += delta;
            }
            else
            {
                var multiplier = (this.jumpTime / Mathf.Max(this.jumpTime, this.constantJumpTime));
                var height     = Mathf.Max(0.5f, (this.JumpHeight / 2) * multiplier);

                verticalVelocity.y = Mathf.Sqrt(-2 * -this.GravityForce * height);

                this.targetJumpTime = verticalVelocity.y / -this.GravityForce;
                this.jumpVelocity   = 0;
            }
        }
        else
        {
            this.jumpTime = 0;
        }

        return horizontalVelocity.Normalized() * this.Speed + (verticalVelocity != Vector3.Zero ? verticalVelocity : this.LinearVelocity * Vector3.Up);
    }
}
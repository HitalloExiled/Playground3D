using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Godot;

public class DummyPlayer : CharacterController3D
{
    #region Constants
    const float FLOOR_DETECTION_LENGHT = 1f;
    #endregion Constants

    #region Fields
    private bool                  automove;
    private bool                  automoveDown;
    private bool                  automoveInvert;
    private bool                  automoveLeft;
    private bool                  automoveRight;
    private bool                  automoveUp;
    private CameraController      camera;
    private DebugDrawer           debugDrawer;
    private float                 automoveTimer;
    private float                 constantJumpTime;
    private float                 jumpTime;
    private float                 jumpVelocity;
    private float                 targetJumpTime;
    private Label                 label;
    private readonly List<string> messages = new List<string>();
    #endregion Fields

    #region Exports
    [Export]
    public float JumpHeight = 1f;

    [Export]
    public float Speed = 4f;

    [Export]
    public NodePath CameraPath;
    #endregion Exports

    #region Hooks
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouse inputEvent)
        {
            if (inputEvent is InputEventMouseMotion inputEventMouseMotion)
            {
                var delta = inputEventMouseMotion.Relative * this.camera.Sensitivity;

                this.camera.Elevation  += delta.y;
                this.camera.Azimut += -delta.x;
            }
        }
    }

    public override void _Ready()
    {
        this.label = this.GetNode<Label>("Control/Label");

        this.debugDrawer = ResourceLoader.Load<PackedScene>("res://Assets/DebugDrawer/DebugDrawer.tscn").Instance<DebugDrawer>();
        this.camera      = this.GetNode<CameraController>(this.CameraPath);

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

        this.debugDrawer.DrawLine(topPosition,    topPosition    + -this.Gravity * 0.5f, new Color(255, 0, 255));
        this.debugDrawer.DrawLine(bottomPosition, bottomPosition + this.Gravity  * 0.5f, new Color(255, 0, 255));

        foreach (var collision in collisions)
        {
            this.debugDrawer.DrawLine(collision.Position, collision.Position + collision.Normal, new Color(0, 0, 255));
        }

        // this.debugDrawer.DrawLine(this.GlobalTransform.origin, this.GlobalTransform.origin + this.camera.previousForward, new Color(0, 0, 255));

        this.Log($"Origin:           {this.GlobalTransform.origin}");
        this.Log($"Motion:           {this.LinearVelocity}");
        this.Log($"Floor Velocity:   {Mathf.Round(this.FloorVelocity.Length())}ms, {this.FloorVelocity}");
        this.Log($"Floor Normal:     {Mathf.Round(Mathf.Rad2Deg(Vector3.Up.AngleTo(this.FloorNormal)))}??, {this.FloorNormal}");
        this.Log($"Velocity:         {Mathf.Round(this.LinearVelocity.Length())}ms, {this.LinearVelocity}");
        this.Log($"Collisions:       {collisions.Count()}");
        this.Log($"Is On Ceiling:    {this.CollisionState.HasFlag(CollisionState.Top)}");
        this.Log($"Is On Wall:       {this.CollisionState.HasFlag(CollisionState.Sides)}");
        this.Log($"Is On Floor:      {this.CollisionState.HasFlag(CollisionState.Bottom)}");
        this.Log($"Is Sliding:       {this.CollisionState.HasFlag(CollisionState.Sliding)}");
        this.Log($"Camera Azimut:    {Mathf.Round(this.camera.Azimut)}");
        this.Log($"Camera Elevation: {Mathf.Round(this.camera.Elevation)}");

        this.label.Text = string.Join("\n", this.messages.Distinct().ToArray());
    }
    #endregion Hooks

    private void Log(string message)
    {
        this.messages.Add(message);
    }

    private Vector3 HandleInputs(float delta)
    {
        var verticalInput   = Vector3.Zero;
        var horizontalInput = Vector3.Zero;

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
            this.automove     = false;
            this.automoveUp   = false;
            this.automoveDown = !this.automoveDown;
        }

        if (this.automove)
        {
            if (automoveTimer >= 0.5f)
            {
                this.automoveInvert = !this.automoveInvert;
                this.automoveTimer = 0;
            }

            horizontalInput.x = (this.automoveInvert ? -1 : 1);

            this.automoveTimer += delta;
        }
        else if (this.automoveLeft)
        {
            horizontalInput.x = -1;
        }
        else if (this.automoveRight)
        {
            horizontalInput.x = 1;
        }

        if (this.automoveUp)
        {
            horizontalInput.z = -1;
        }
        else if (this.automoveDown)
        {
            horizontalInput.z = 1;
        }

        if (Input.IsActionPressed("left"))
        {
            horizontalInput.x = -1;
        }

        if (Input.IsActionPressed("right"))
        {
            horizontalInput.x = 1;
        }

        if (Input.IsActionPressed("up"))
        {
            horizontalInput.z = -1;
        }

        if (Input.IsActionPressed("down"))
        {
            horizontalInput.z = 1;
        }

        if (Input.IsActionJustPressed("ui_cancel"))
        {
            var mode = Input.GetMouseMode() == Input.MouseMode.Captured
                ? Input.MouseMode.Visible
                : Input.MouseMode.Captured;

            Input.SetMouseMode(mode);
        }

        if (this.CollisionState != CollisionState.None)
        {
            this.Snap     = true;
            this.jumpTime = 0;
        }

        if (Input.IsActionJustPressed("jump"))
        {
            verticalInput.y = this.jumpVelocity = Mathf.Sqrt(-2 * -this.GravityForce * this.JumpHeight / 2);

            this.targetJumpTime   = verticalInput.y / -this.GravityForce;
            this.constantJumpTime = this.JumpHeight / 2 / this.jumpVelocity;
            this.jumpTime        += delta;

            this.Snap = false;
        }
        else if (this.jumpVelocity > 0)
        {
            if (Input.IsActionPressed("jump") && this.jumpTime < this.constantJumpTime)
            {
                verticalInput.y = this.jumpVelocity;

                this.jumpTime += delta;
            }
            else
            {
                var multiplier = (this.jumpTime / Mathf.Max(this.jumpTime, this.constantJumpTime));
                var height     = Mathf.Max(0.5f, (this.JumpHeight / 2) * multiplier);

                verticalInput.y = Mathf.Sqrt(-2 * -this.GravityForce * height);

                this.targetJumpTime = verticalInput.y / -this.GravityForce;
                this.jumpVelocity   = 0;
            }
        }
        else
        {
            this.jumpTime = 0;
        }

        horizontalInput = horizontalInput.Normalized();

        var isGrounded = this.CollisionState.HasFlag(CollisionState.Bottom);

        var up = this.camera.Up.LinearInterpolate(isGrounded ? this.FloorNormal : Vector3.Up, 0.2f);

        this.camera.Up = up;

        var transform = this.GlobalTransform;

        transform.basis.y = up;
        transform.basis.x = -transform.basis.z.Cross(up);
        transform.basis = transform.basis.Orthonormalized();

        this.GlobalTransform = transform;

        this.Gravity = isGrounded ? -this.FloorNormal : Vector3.Down;

        var forward = -this.camera.GlobalTransform.basis.z.Slide(-this.Gravity).Normalized() * -horizontalInput.z;
        var sides   = this.camera.GlobalTransform.basis.x * horizontalInput.x;

        var horizontalVelocity     = (forward + sides) * this.Speed;
        var linearVerticalVelocity = this.LinearVelocity.Project(this.Gravity);
        var verticalVelocity       = (verticalInput != Vector3.Zero ? -this.Gravity * verticalInput.Length() : linearVerticalVelocity);

        return horizontalVelocity + verticalVelocity;
    }
}
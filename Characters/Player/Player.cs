using System;
using Godot;

public class Player : RigidBody
{
    #region Constants
    const int MIN_CAMERA_ANGLE = 5;
    const int MAX_CAMERA_ANGLE = 140;
    #endregion Constants

    #region Fields
    private Spatial  camera;
    private float    cameraAngle = 60f;
    private float    slopeAngle;
    private float    currentCameraDistance;
    private Vector3  direction;
    private Vector3  onAirDirection;
    private DebugDrawer drawer3D;
    private bool     isGrounded;
    private bool     isNearGround;
    private bool     isJumping;
    private float    jumpTime;
    private Vector2  mouseDelta;
    private float    targetSpeed;
    private Vector3  velocity;
    private float    groundDistance;
    private float    airTime = 0;
    #endregion Fields

    #region Nodes
    private Empty  cameraTarget;
    private Empty  slopeDetector;
    private Empty  groundDetector;
    private Empty  footRange;
    private Empty  headRange;
    private Label  label;
    private Sprite cameraDirectionSprite;
    private Sprite cameraPositionSprite;
    private Sprite directionSprite;
    private Sprite veloctiSprite;
    #endregion Nodes

    #region Exports
    [Export]
    public float Speed = 5f;

    [Export]
    public float RunSpeed = 10f;

    [Export]
    public NodePath CameraPath;

    [Export]
    public float CameraSensitivity = 0.25f;

    [Export]
    public float CameraDistance = 5f;

    [Export]
    public Vector3 CameraOffset = Vector3.Zero;

    [Export]
    public float JumpHeight = 1f;

    [Export]
    public float MaxJumpTime = 1f;

    [Export]
    public float MaxSlopeAngle = 45f;

    [Export]
    public float TimeScale = 1;
    #endregion Exports

    private void ProcessCamera(float delta)
    {
        if (Input.GetMouseMode() != Input.MouseMode.Captured)
        {
            return;
        }

        var state          = this.GetWorld().DirectSpaceState;
        var direction      = Vector3.Zero;
        var right          = this.camera.GlobalTransform.basis.x;
        var up             = this.camera.GlobalTransform.basis.y;
        var forward        = this.camera.GlobalTransform.basis.z;
        var cameraPosition = this.camera.GlobalTransform.origin;
        var targetPosition = this.cameraTarget.GlobalTransform.origin + this.CameraOffset;
        var hasMoved       = this.mouseDelta != Vector2.Zero;

        if (hasMoved)
        {
            forward = (forward.Rotated(Vector3.Up, Mathf.Deg2Rad(-this.mouseDelta.x)) + forward.Rotated(right, Mathf.Deg2Rad(-this.mouseDelta.y))).Normalized();
            right   = forward.Cross(Vector3.Up).Normalized();
            up      = right.Cross(forward).Normalized();

            var offset = targetPosition + forward;

            direction = (offset - targetPosition).Normalized();
        }
        else
        {
            direction = forward;
            right     = forward.Cross(Vector3.Up).Normalized();
            up        = right.Cross(forward).Normalized();
        }

        var angle = Mathf.Rad2Deg(direction.AngleTo(Vector3.Up));

        if (!hasMoved && angle != this.cameraAngle)
        {
            direction = Vector3.Up.Rotated(-right, Mathf.Deg2Rad(this.cameraAngle));
        }

        if (angle < MIN_CAMERA_ANGLE)
        {
            direction = Vector3.Up.Rotated(-right, Mathf.Deg2Rad(MIN_CAMERA_ANGLE));
        }

        if (angle > MAX_CAMERA_ANGLE)
        {
            direction = Vector3.Up.Rotated(-right, Mathf.Deg2Rad(MAX_CAMERA_ANGLE));
        }

        this.cameraAngle = Mathf.Rad2Deg(direction.AngleTo(Vector3.Up));

        var result = state.IntersectRay(targetPosition, targetPosition + (direction * this.CameraDistance), new Godot.Collections.Array { this });

        var isColliding = result.Count > 0;

        var distance = isColliding
            ? ((Vector3)result["position"] - targetPosition).Length() - 0.5f
            : this.CameraDistance;

        this.currentCameraDistance = distance < this.CameraDistance
            ? distance :
            Mathf.Lerp(this.currentCameraDistance, distance, 0.1f);

        var position = targetPosition + (direction * this.currentCameraDistance);

        position = !hasMoved && new Vector3(direction.x, 0, direction.z).Normalized().Dot(this.velocity) > -1f
            ? cameraPosition.LinearInterpolate(position, 0.5f)
            : position;

        this.camera.LookAtFromPosition(position, targetPosition, up);

        this.mouseDelta = Vector2.Zero;
    }

    private void ProccessGroundCheck(float delta)
    {
        this.airTime += delta;
    }

    private void ProcessInput(float delta)
    {
        this.velocity = Vector3.Zero;

        if (Input.IsActionPressed("left"))
        {
            this.velocity.x -= 1;
        }

        if (Input.IsActionPressed("right"))
        {
            this.velocity.x += 1;
        }

        if (Input.IsActionPressed("up"))
        {
            this.velocity.z += 1;
        }

        if (Input.IsActionPressed("down"))
        {
            this.velocity.z -= 1;
        }

        this.isJumping = this.jumpTime > 0;

        this.velocity = this.velocity.Normalized();

        if (this.isGrounded && !this.isJumping && Input.IsActionJustPressed("jump") || this.isJumping && Input.IsActionPressed("jump"))
        {
            this.velocity.y = Mathf.Sqrt(this.JumpHeight * 2 * 9.8f);

            if (!this.isJumping)
            {
                this.isJumping = true;
                this.jumpTime  = this.MaxJumpTime;
            }
            else
            {
                this.jumpTime -= delta;
            }
        }
        else
        {
            this.jumpTime = 0;
        }

        if (Input.IsActionJustPressed("ui_cancel"))
        {
            var mode = Input.GetMouseMode() == Input.MouseMode.Captured
                ? Input.MouseMode.Visible
                : Input.MouseMode.Captured;

            Input.SetMouseMode(mode);
        }

        this.targetSpeed = Mathf.Lerp(this.targetSpeed, this.isGrounded && Input.IsActionPressed("run") ? this.RunSpeed : this.Speed, 0.5f);
    }

    private void ProcessUI()
    {
        var directionSize = this.directionSprite.Texture.GetWidth() / 2;
        var direction2D   = new Vector2(-this.direction.x, -this.direction.z).Normalized();

        if (direction2D != Vector2.Zero)
        {
            this.directionSprite.Transform = new Transform2D(direction2D.Angle(), direction2D * directionSize);
        }

        var cameraDirectionSize = this.cameraDirectionSprite.Texture.GetWidth() / 2;

        var targetPosition2D  = new Vector2(this.GlobalTransform.origin.x, this.GlobalTransform.origin.z);
        var cameraPosition2D  = new Vector2(this.camera.GlobalTransform.origin.x, this.camera.GlobalTransform.origin.z);
        var cameraDirection2D = new Vector2(-this.camera.GlobalTransform.basis.z.x, -this.camera.GlobalTransform.basis.z.z).Normalized();

        if (cameraPosition2D != Vector2.Zero)
        {
            this.cameraDirectionSprite.Transform = new Transform2D(cameraDirection2D.Angle(), cameraDirection2D * -cameraDirectionSize);
            this.cameraPositionSprite.Position = cameraDirection2D * -(cameraPosition2D - targetPosition2D).Length() * 50;
        }

        this.veloctiSprite.Position = (new Vector2(this.velocity.x, -this.velocity.z).Normalized() * this.targetSpeed) * 25;

        var velocity = this.velocity * this.targetSpeed;

        this.label.Text =
              $"Air Time: {Math.Round(this.airTime, 2)}s"
            + $"\nCamera Angle: {Mathf.Round(this.cameraAngle)}°"
            + $"\nGround Distance: {Math.Round(this.groundDistance, 2)}m"
            + $"\nInput Velocity: {Math.Round(velocity.Length(), 2)} ({Math.Round(velocity.x, 2)}, {Math.Round(velocity.y, 2)}, {Math.Round(velocity.z, 2)})"
            + $"\nIs Grounded: {this.isGrounded}"
            + $"\nIs Jumping: {this.isJumping}"
            + $"\nIs Near Ground: {this.isNearGround}"
            + $"\nJump Time: {Math.Round(this.jumpTime, 2)}s"
            + $"\nSlope Angle: {Math.Round(this.slopeAngle)}"
            + $"\nVelocity: {Math.Round(this.LinearVelocity.Length())} ({Math.Round(this.LinearVelocity.x, 2)}, {Math.Round(this.LinearVelocity.y, 2)}, {Math.Round(this.LinearVelocity.z, 2)})";
    }

    #region LifeCycle
    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion inputEventMouseMotion)
        {
            this.mouseDelta = inputEventMouseMotion.Relative * this.CameraSensitivity;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        var contactCount = state.GetContactCount();

        var forward = (-this.camera.GlobalTransform.basis.z * new Vector3(1, 0, 1)).Normalized();

        var horizontalVelocity = ((forward.Normalized() * this.velocity.z) + (this.camera.GlobalTransform.basis.x * this.velocity.x)).Normalized() * this.targetSpeed;
        var verticalVelocity   = this.velocity * new Vector3(0, 1, 0);

        var direction = horizontalVelocity != Vector3.Zero
            ? this.direction = this.direction.LinearInterpolate(-horizontalVelocity, 0.5f)
            : this.direction;

        direction = direction.Normalized();

        if (direction != Vector3.Zero)
        {
            this.LookAt(this.GlobalTransform.origin + direction, Vector3.Up);
        }

        this.slopeAngle   = 0;
        this.isGrounded   = false;
        this.isNearGround = false;

        var directSpaceState = this.GetWorld().DirectSpaceState;

        var footRangePosition  = this.footRange.GlobalTransform.origin;
        var footRangeDirection = this.footRange.GlobalTransform.basis.z;
        var footRangeRadius    = this.footRange.Size / 2;

        var groundNormal = Vector3.Up;
        var contactPoint = this.GlobalTransform.origin;

        if (contactCount > 0)
        {
            for (var i = 0; i < contactCount; i++)
            {
                var collider = (Spatial)state.GetContactColliderObject(i);
                var position = collider.GlobalTransform.origin + state.GetContactLocalPosition(i);

                groundNormal = state.GetContactLocalNormal(i);

                if ((footRangePosition - position).Length() <= footRangeRadius)
                {
                    this.drawer3D.DrawLine(footRangePosition, position, new Color(255, 0, 255));

                    contactPoint = position;

                    this.airTime = 0;

                    break;
                }
            }
        }
        else
        {
            var groundDetectorPosition  = this.groundDetector.GlobalTransform.origin;
            var groundDetectorDirection = this.groundDetector.GlobalTransform.basis.z;
            var groundDetectorSize      = this.groundDetector.Size;

            var footRay = directSpaceState.IntersectRay(groundDetectorPosition, groundDetectorPosition + (groundDetectorDirection * groundDetectorSize), new Godot.Collections.Array { this });

            if (footRay.Count > 0)
            {
                var position = (Vector3)footRay["position"];

                groundNormal = (Vector3)footRay["normal"];

                this.drawer3D.DrawLine(footRangePosition, position, new Color(255, 0, 255));

                if ((footRangePosition - position).Length() <= footRangeRadius)
                {
                    this.drawer3D.DrawLine(footRangePosition, position, new Color(255, 0, 255));

                    contactPoint = position;

                    this.airTime = 0;
                }
            }
        }

        this.isGrounded = this.airTime < 0.1f;

        if (this.isGrounded)
        {
            var aheadRay = directSpaceState.IntersectRay(this.slopeDetector.GlobalTransform.origin, this.slopeDetector.GlobalTransform.origin + (this.slopeDetector.GlobalTransform.basis.z * this.slopeDetector.Size), new Godot.Collections.Array { this });

            if (aheadRay.Count > 0)
            {
                var position = (Vector3)aheadRay["position"];

                var contactDirection = contactPoint - footRangePosition;
                var angle            = contactDirection.AngleTo(groundNormal);

                var hypotenuse = contactDirection.Length();
                var adjacent   = hypotenuse * Mathf.Cos(angle);

                groundNormal *= adjacent;

                this.drawer3D.DrawLine(footRangePosition, footRangePosition + groundNormal, new Color(0, 0, 255));

                angle = groundNormal.AngleTo(Vector3.Down);

                adjacent     = groundNormal.Length();
                var opposite = Mathf.Tan(angle) * adjacent;
                hypotenuse   = Mathf.Sqrt(Mathf.Pow(adjacent, 2) + Mathf.Pow(opposite, 2));

                this.groundDistance = hypotenuse;

                contactPoint = footRangePosition + (Vector3.Down * this.groundDistance);

                horizontalVelocity = (position - contactPoint).Normalized() * horizontalVelocity.Length();

                this.drawer3D.DrawLine(contactPoint, position, new Color(255, 255, 0));
            }

            this.slopeAngle = horizontalVelocity != Vector3.Zero ? 90 - Mathf.Rad2Deg(horizontalVelocity.AngleTo(Vector3.Up)) : 0;

            if (this.slopeAngle <= this.MaxSlopeAngle)
            {
                this.LinearVelocity = horizontalVelocity + verticalVelocity;
            }
        }
        else
        {
            var currentVerticalVelocity   = this.LinearVelocity * new Vector3(0, 1, 0);
            var currentHorizontalVelocity = this.LinearVelocity * new Vector3(1, 0, 1);

            var dot = Mathf.Max(this.onAirDirection.Normalized().Dot(horizontalVelocity.Normalized()), 0);

            if (this.isJumping && this.jumpTime > 0)
            {
                this.LinearVelocity = currentHorizontalVelocity.LinearInterpolate(horizontalVelocity * dot, dot) + verticalVelocity;
            }
            else
            {
                this.LinearVelocity = currentHorizontalVelocity.LinearInterpolate(horizontalVelocity * dot, dot) + currentVerticalVelocity;
            }
        }
    }

    public override void _Ready()
    {
        this.label                 = this.GetNode<Label>("UI/Sprite/ColorRect/Label");
        this.cameraDirectionSprite = this.GetNode<Sprite>("UI/Sprite/CameraDirection");
        this.cameraPositionSprite  = this.GetNode<Sprite>("UI/Sprite/CameraPosition");
        this.directionSprite       = this.GetNode<Sprite>("UI/Sprite/Direction");
        this.veloctiSprite         = this.GetNode<Sprite>("UI/Sprite/Velocity");
        this.cameraTarget          = this.GetNode<Empty>("CameraTarget");
        this.slopeDetector         = this.GetNode<Empty>("SlopeDetector");
        this.groundDetector        = this.GetNode<Empty>("GroundDetector");
        this.footRange             = this.GetNode<Empty>("FootRange");
        this.headRange             = this.GetNode<Empty>("HeadRange");
        this.camera                = this.GetNode<Spatial>(this.CameraPath);

        this.drawer3D = ResourceLoader.Load<PackedScene>("res://Assets/DebugDrawer/DebugDrawer.tscn").Instance<DebugDrawer>();

        this.GetTree().Root.CallDeferred("add_child", this.drawer3D);

        this.direction = -this.GlobalTransform.basis.z;

        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    public override void _PhysicsProcess(float delta)
    {
        Engine.TimeScale = this.TimeScale;
        this.drawer3D.Clear();

        this.ProccessGroundCheck(delta);
        this.ProcessInput(delta);
        this.ProcessCamera(delta);
        this.ProcessUI();
    }
    #endregion LifeCycle
}

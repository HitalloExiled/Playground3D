using System;
using Godot;

public class CameraController : Camera
{
    private Vector3 up = Vector3.Up;
    private bool hasMoved;
    private float currentDistance;
    private float latitude;
    private float longitude;

    public Spatial Target { get; set; }

    public float Latitude
    {
        get => this.latitude;
        set
        {
            this.hasMoved = value != this.latitude;

            this.latitude = Math.Min(Math.Max(value, -89.9f), 89.9f);
        }
    }

    public float Longitude
    {
        get => this.longitude;
        set
        {
            this.hasMoved = value != this.longitude;

            if (value > 360)
            {
                this.longitude = value - 360;
            }
            else if (value < 0)
            {
                this.longitude = value + 360;
            }
            else
            {
                this.longitude = value;
            }
        }
    }

    [Export]
    public bool Enabled { get; set; } = true;

    [Export]
    public bool Follow { get; set; } = true;

    [Export]
    public NodePath TargetPath { get; set; }

    [Export]
    public float Sensitivity { get; set; } = 0.25f;

    [Export]
    public float Distance { get; set; } = 10;

    [Export]
    public Vector3 Up
    {
        get => this.up;
        set => this.up = value.Normalized();
    }

    public override void _Ready()
    {
        this.Target = this.GetNode<Spatial>(this.TargetPath);

        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!this.Enabled || Input.GetMouseMode() != Input.MouseMode.Captured)
        {
            return;
        }

        var transform      = this.GlobalTransform;
        var targetPosition = this.Target.GlobalTransform.origin;
        var rotation       = Vector3.Zero;
        var forward        = Vector3.Zero;

        if (Vector3.Left.IsEqualApprox(this.Up) || Vector3.Right.IsEqualApprox(this.Up))
        {
            forward = -Vector3.Forward;
        }
        else
        {
            forward = Vector3.Right.Cross(this.Up);
        }

        if (!this.hasMoved && this.Follow)
        {
            var direction = (transform.origin - targetPosition).Slide(this.Up).Normalized();

            var cross   = forward.Cross(direction);
            var dot     = forward.Dot(direction);
            var radians = Mathf.Atan2(cross.Length(), dot);

            if (this.Up.Dot(cross) < 0.0)
            {
                radians = -radians;
            }

            var angle = Mathf.Rad2Deg(radians);

            this.Longitude = angle;
        }

        var latitudeRadians  = Mathf.Deg2Rad(this.Latitude);
        var longitudeRadians = Mathf.Deg2Rad(this.Longitude);

        var rotationX = forward.Rotated(this.Up, longitudeRadians).Normalized();

        var right = rotationX.Cross(this.Up).Normalized();

        rotation = rotationX.Rotated(right, latitudeRadians).Normalized();

        var result = this.GetWorld().DirectSpaceState.IntersectRay(targetPosition, targetPosition + (rotation * this.Distance), new Godot.Collections.Array { this, this.Target });

        var distance = result.Count > 0
            ? ((Vector3)result["position"] - targetPosition).Length() - 0.5f
            : this.Distance;

        this.currentDistance = distance < this.Distance
            ? distance :
            Mathf.Lerp(this.currentDistance, distance, 0.1f);

        var position = targetPosition + rotation * this.currentDistance;

        transform.origin = transform.origin.LinearInterpolate(position, this.hasMoved ? 1 : 0.5f);

        this.GlobalTransform = transform;

        this.LookAt(targetPosition, this.Up);

        this.hasMoved = false;
    }
}

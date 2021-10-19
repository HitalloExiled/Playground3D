using System;
using Godot;

public class CameraController : Camera
{
    private bool  hasMoved;
    private float azimut;
    private float currentDistance;
    private float elevation;
    private Vector3 previousReference = Vector3.Forward;
    private Vector3 up                = Vector3.Up;

    public Spatial Target { get; set; }

    public float Azimut
    {
        get => this.azimut;
        set
        {
            this.hasMoved = value != this.azimut;

            if (value > 360)
            {
                this.azimut = value - 360;
            }
            else if (value < 0)
            {
                this.azimut = value + 360;
            }
            else
            {
                this.azimut = value;
            }
        }
    }

    public float Elevation
    {
        get => this.elevation;
        set
        {
            this.hasMoved = value != this.elevation;

            this.elevation = Math.Min(Math.Max(value, -89.9f), 89.9f);
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

    private float AngleTo(Vector3 left, Vector3 right, Vector3 axis)
    {
        var cross   = left.Cross(right);
        var dot     = left.Dot(right);
        var radians = Mathf.Atan2(cross.Length(), dot);

        if (axis.Dot(cross) < 0.0)
        {
            radians = -radians;
        }

        return radians;
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
        var reference      = Vector3.Zero;

        if (Vector3.Left.IsEqualApprox(this.Up) || Vector3.Right.IsEqualApprox(this.Up))
        {
            reference = -Vector3.Forward;
        }
        else
        {
            reference = Vector3.Right.Cross(this.Up).Normalized();
        }

        if (!this.hasMoved && this.Follow)
        {
            var direction = (transform.origin - targetPosition).Slide(this.Up).Normalized();

            this.Azimut = Mathf.Rad2Deg(this.AngleTo(reference, direction, this.Up));
        }
        else if (reference != previousReference)
        {
            var angle = Mathf.Rad2Deg(this.AngleTo(reference.Slide(this.Up).Normalized(), previousReference.Slide(this.Up).Normalized(), this.Up));
            this.Azimut += angle;
        }

        var latitudeRadians  = Mathf.Deg2Rad(this.Elevation);
        var longitudeRadians = Mathf.Deg2Rad(this.Azimut);

        var rotationX = reference.Rotated(this.Up, longitudeRadians).Normalized();

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

        transform.origin = transform.origin.LinearInterpolate(position, !this.hasMoved && this.Follow ? 0.5f : 1);

        this.GlobalTransform = transform;

        this.LookAt(targetPosition, this.Up);

        this.hasMoved = false;

        this.previousReference = reference;
    }
}

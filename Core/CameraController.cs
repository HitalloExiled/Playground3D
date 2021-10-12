using System;
using Godot;

public class CameraController : Camera
{
    private bool hasMoved;
    private float currentDistance;
    private float latitude;
    private float longitude;

    public Spatial Target    { get; set; }
    public float   Latitude  { get => this.latitude; set => this.latitude   = Math.Min(Math.Max(value, -89.9f), 89.9f); }
    public float   Longitude
    {
        get => this.longitude;
        set
        {
            if (value > 360)
            {
                this.longitude = 0;
            }
            else if (value < 0)
            {
                this.longitude = 360;
            }
            else
            {
                this.longitude = value;
            }
        }
    }

    [Export]
    public NodePath TargetPath;

    [Export]
    public float Sensitivity = 0.25f;

    [Export]
    public float Distance = 10;

    public override void _Ready()
    {
        this.Target = this.GetNode<Spatial>(this.TargetPath);

        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    public override void _Input(InputEvent @event)
    {
        this.hasMoved = false;
        if (@event is InputEventMouse inputEvent)
        {
            if (inputEvent is InputEventMouseMotion inputEventMouseMotion)
            {
                var delta = inputEventMouseMotion.Relative * this.Sensitivity;

                this.Latitude  += delta.y;
                this.Longitude += -delta.x;

                this.hasMoved = true;
            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        // if (Input.GetMouseMode() != Input.MouseMode.Captured)
        // {
        //     return;
        // }

        var transform      = this.GlobalTransform;
        var targetPosition = this.Target.GlobalTransform.origin;
        var rotation       = Vector3.Zero;

        if (this.hasMoved)
        {
            var latitudeRadians  = Mathf.Deg2Rad(this.Latitude);
            var longitudeRadians = Mathf.Deg2Rad(this.Longitude);

            var x = Mathf.Cos(latitudeRadians) * Mathf.Cos(longitudeRadians);
            var y = Mathf.Sin(latitudeRadians);
            var z = -Mathf.Cos(latitudeRadians) * Mathf.Sin(longitudeRadians);

            rotation = new Vector3(x, y, z);
        }
        else
        {
            rotation = (transform.origin - targetPosition).Normalized();
        }

        var result = this.GetWorld().DirectSpaceState.IntersectRay(targetPosition, targetPosition + (rotation * this.Distance), new Godot.Collections.Array { this, this.Target });

        var distance = result.Count > 0
            ? ((Vector3)result["position"] - targetPosition).Length() - 0.5f
            : this.Distance;

        this.currentDistance = distance < this.Distance
            ? distance :
            Mathf.Lerp(this.currentDistance, distance, 0.1f);

        var position = targetPosition + rotation * this.currentDistance;

        if (!this.hasMoved)
        {
            position.y = transform.origin.y;
        }

        transform.origin = transform.origin.LinearInterpolate(position, this.hasMoved ? 1 : 0.5f);

        this.GlobalTransform = transform;

        this.LookAt(targetPosition, Vector3.Up);
    }
}

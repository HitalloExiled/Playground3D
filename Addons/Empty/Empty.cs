using Godot;

public enum EmptyType
{
    Arrow,
    Axis,
    Box,
    Circle,
    Plane,
    Sphere,
}

public enum EmptyAxisOrientation
{
    X,
    Y,
    Z,
}

public class EmptyAxis : Object
{
    public readonly Vector3 X;
    public readonly Vector3 Y;
    public readonly Vector3 Z;

    public EmptyAxis(EmptyAxisOrientation direction)
    {
        switch (direction)
        {
            case EmptyAxisOrientation.X:
                this.X = new Vector3(0, 0, 1);
                this.Y = new Vector3(0, 1, 0);
                this.Z = new Vector3(-1, 0, 0);

                break;
            case EmptyAxisOrientation.Y:
                this.X = new Vector3(1, 0, 0);
                this.Y = new Vector3(0, 0, 1);
                this.Z = new Vector3(0, -1, 0);

                break;
            default:
            case EmptyAxisOrientation.Z:
                this.X = new Vector3(1, 0, 0);
                this.Y = new Vector3(0, 1, 0);
                this.Z = new Vector3(0, 0, 1);

                break;
        }
    }
}

[Tool]
public class Empty : ImmediateGeometry
{
    const float ANGLE = 22.5f;

    private EmptyAxisOrientation orientation = EmptyAxisOrientation.Z;

    public EmptyAxis Axis { get; private set; } = new EmptyAxis(EmptyAxisOrientation.Z);

    private EmptyType type = EmptyType.Axis;
    private Color     color = new Color(255, 255, 255);
    private float     size  = 1;
    private bool      xRay  = false;

    [Export]
    public EmptyType Type
    {
        get => this.type;
        set
        {
            this.type = value;
            this.ReDraw();
        }
    }

    [Export]
    public EmptyAxisOrientation Orientation
    {
        get => this.orientation;
        set
        {
            this.orientation = value;
            this.Axis        = new EmptyAxis(value);

            this.ReDraw();
        }
    }

    [Export]
    public float Size
    {
        get => this.size;
        set
        {
            this.size = value;

            this.ReDraw();
        }
    }

    [Export]
    public Color Color
    {
        get => this.color;
        set => (this.MaterialOverride as SpatialMaterial).AlbedoColor = this.color = value;
    }

    [Export]
    public bool XRay
    {
        get => this.xRay;
        set => (this.MaterialOverride as SpatialMaterial).FlagsNoDepthTest = this.xRay = value;
    }

    public Empty()
    {
        var material = new SpatialMaterial();

        material.FlagsUnshaded = true;
        material.VertexColorUseAsAlbedo = true;

        this.MaterialOverride = material;
    }

    private void DrawLine(Vector3 from, Vector3 to)
    {
        this.Begin(Mesh.PrimitiveType.Lines);
        this.AddVertex(from);
        this.AddVertex(to);
        this.End();
    }

    private void DrawCircle(Vector3 direction, Vector3 axis, float radius)
    {
        this.Begin(Mesh.PrimitiveType.LineLoop);

        for (var i = 0; i < 16; i++)
        {
            var vertex = direction.Rotated(axis, Mathf.Deg2Rad(ANGLE * i)).Normalized() * radius;

            this.AddVertex(vertex);
        }

        this.End();
    }

    private void DrawCircle()
    {
        this.DrawCircle(this.Axis.Y, this.Axis.Z, this.Size / 2);
    }

    private void DrawPlane(Vector3 position, Vector3 x, Vector3 y)
    {
        this.Begin(Mesh.PrimitiveType.LineLoop);

        this.AddVertex(position + ( x / 2) + ( y / 2));
        this.AddVertex(position + ( x / 2) + (-y / 2));
        this.AddVertex(position + (-x / 2) + (-y / 2));
        this.AddVertex(position + (-x / 2) + ( y / 2));

        this.End();
    }

    private void DrawPlane()
    {
        this.DrawPlane(Vector3.Zero, this.Axis.Y * this.Size, this.Axis.X * this.Size);
    }

    private void DrawSphere() => this.DrawSphere(this.Size / 2);

    private void DrawSphere(float radius)
    {
        this.DrawCircle(Vector3.Forward, Vector3.Up, radius);
        this.DrawCircle(Vector3.Forward, Vector3.Right, radius);
        this.DrawCircle(Vector3.Right,   Vector3.Forward, radius);
    }

    private void DrawAxis()
    {
        this.DrawLine(Vector3.Zero, (Vector3.Right    * this.Size) / 2);
        this.DrawLine(Vector3.Zero, (-Vector3.Right   * this.Size) / 2);
        this.DrawLine(Vector3.Zero, (Vector3.Up       * this.Size) / 2);
        this.DrawLine(Vector3.Zero, (-Vector3.Up      * this.Size) / 2);
        this.DrawLine(Vector3.Zero, (Vector3.Forward  * this.Size) / 2);
        this.DrawLine(Vector3.Zero, (-Vector3.Forward * this.Size) / 2);
    }

    private void DrawArrow()
    {
        var head  = -Vector3.Forward * this.Size;
        var head1 = (Vector3.Forward).Rotated(this.Axis.Z, Mathf.Deg2Rad(30));
        var head2 = (Vector3.Forward).Rotated(this.Axis.Z, Mathf.Deg2Rad(-30));

        this.DrawLine(Vector3.Zero, head);
        this.DrawLine(head, head + head1 * 0.1f);
        this.DrawLine(head, head + head2 * 0.1f);
    }

    private void DrawBox()
    {
        this.DrawPlane(( this.Axis.X * this.Size) / 2, this.Axis.Z * this.size, this.Axis.Y * this.size);
        this.DrawPlane((-this.Axis.X * this.Size) / 2, this.Axis.Z * this.size, this.Axis.Y * this.size);
        this.DrawPlane(( this.Axis.Y * this.Size) / 2, this.Axis.X * this.size, this.Axis.Z * this.size);
        this.DrawPlane((-this.Axis.Y * this.Size) / 2, this.Axis.X * this.size, this.Axis.Z * this.size);
        this.DrawPlane(( this.Axis.Z * this.Size) / 2, this.Axis.X * this.size, this.Axis.Y * this.size);
        this.DrawPlane((-this.Axis.Z * this.Size) / 2, this.Axis.X * this.size, this.Axis.Y * this.size);
    }

    public void ReDraw()
    {
        this.Clear();

        switch (this.type)
        {
            case EmptyType.Circle:
                this.DrawCircle();
                break;
            case EmptyType.Sphere:
                this.DrawSphere();
                break;
            case EmptyType.Axis:
                this.DrawAxis();
                break;
            case EmptyType.Arrow:
                this.DrawArrow();
                break;
            case EmptyType.Plane:
                this.DrawPlane();
                break;
            case EmptyType.Box:
                this.DrawBox();
                break;
        }
    }

    public override void _Ready()
    {
        (this.MaterialOverride as SpatialMaterial).AlbedoColor      = this.Color;
        (this.MaterialOverride as SpatialMaterial).FlagsNoDepthTest = this.XRay;

        this.ReDraw();
    }
}

using Godot;

class DebugDrawer : ImmediateGeometry
{
    const float ANGLE = 22.5f;

    public void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        this.Begin(Mesh.PrimitiveType.Lines);

        this.SetColor(color);

        this.AddVertex(from);
        this.AddVertex(to);

        this.End();
    }

    public void DrawCircle(Vector3 position, Vector3 direction, Vector3 axis, float radius, Color color)
    {
        this.Begin(Mesh.PrimitiveType.LineLoop);
        this.SetColor(color);

        for (var i = 0; i < 16; i++)
        {
            var vertex = position + direction.Rotated(axis, Mathf.Deg2Rad(ANGLE * i)).Normalized() * radius;

            this.AddVertex(vertex);
        }

        this.End();
    }

    public void DrawSphere(Vector3 position, float radius, Color color)
    {
        this.DrawCircle(position, Vector3.Forward, Vector3.Up,      radius, color);
        this.DrawCircle(position, Vector3.Forward, Vector3.Right,   radius, color);
        this.DrawCircle(position, Vector3.Right,   Vector3.Forward, radius, color);
    }
}

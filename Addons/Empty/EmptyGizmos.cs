using System.Collections.Generic;
using Godot;

class EmptyGizmos : EditorSpatialGizmoPlugin
{
    const float ANGLE = 22.5f;

    public override string GetName() => nameof(EmptyGizmos);

    public override bool HasGizmo(Spatial spatial)
    {
        return spatial is Empty;
    }

    public EmptyGizmos()
    {
        this.CreateHandleMaterial("handles");
    }

    public override void Redraw(EditorSpatialGizmo gizmo)
    {
        gizmo.Clear();

        var empty = gizmo.GetSpatialNode() as Empty;

        var handles = new List<Vector3>();

        handles.Add(-Vector3.Forward * empty.Size / 2);

        gizmo.AddHandles(handles.ToArray(), this.GetMaterial("handles", gizmo));
    }

    public override void SetHandle(EditorSpatialGizmo gizmo, int index, Camera camera, Vector2 point)
    {
        this.Redraw(gizmo);

        var empty     = gizmo.GetSpatialNode() as Empty;
        var normal    = camera.ProjectRayNormal(point);
        // var forward   = empty.GlobalTransform.basis.x;
        var right     = -(-camera.GlobalTransform.basis.z).Cross(empty.GlobalTransform.basis.z);
        var forward   = empty.GlobalTransform.basis.z.Cross(right);
        var direction = empty.GlobalTransform.origin - camera.GlobalTransform.origin;
        var projected = direction.Project(forward);
        var angle     = normal.AngleTo(projected);
        var length    = (projected.Length() / Mathf.Cos(angle));
        var position  = (normal * length) + camera.ProjectRayOrigin(point);

        empty.Size = (position - empty.GlobalTransform.origin).Length() * 2;

        GD.Print($"Radius: {empty.Size}");
    }
}

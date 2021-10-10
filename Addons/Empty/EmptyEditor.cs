#if TOOLS
using System;
using Godot;

[Tool]
public class EmptyEditor : EditorPlugin
{
    private Lazy<EmptyGizmos> Gizmos;

    public EmptyEditor()
    {
        this.Gizmos = new Lazy<EmptyGizmos>(() => GD.Load<CSharpScript>("res://Addons/Empty/EmptyGizmos.cs").New() as EmptyGizmos);
    }

    public override void _EnterTree()
    {
        var script = GD.Load<CSharpScript>("res://Addons/Empty/Empty.cs");
        this.AddCustomType("Empty", "ImmediateGeometry", script, null);
        this.AddSpatialGizmoPlugin(this.Gizmos.Value);
    }

    public override void _ExitTree()
    {
        this.RemoveSpatialGizmoPlugin(this.Gizmos.Value);
    }

    public override void EnablePlugin()
    {
        this.RemoveSpatialGizmoPlugin(this.Gizmos.Value);
        this.AddSpatialGizmoPlugin(this.Gizmos.Value);
    }
}

#endif
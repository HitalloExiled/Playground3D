using System;
using System.Collections.Generic;
using Godot;
using Object = Godot.Object;

[Flags]
public enum CollisionState
{
    None    = 0x0,
    Top     = 0x2,
    Sides   = 0x4,
    Bottom  = 0x8,
    Sliding = 0x10,
}

public enum DetectionMode
{
    Angle,
    Offset,
    Collider
}

public struct CollisionContact
{
    public Object  Collider           { get; private set; }
    public ulong   ColliderId         { get; private set; }
    public object  ColliderMetadata   { get; private set; }
    public Object  ColliderShape      { get; private set; }
    public int     ColliderShapeIndex { get; private set; }
    public Vector3 ColliderVelocity   { get; private set; }
    public Object  LocalShape         { get; private set; }
    public Vector3 Normal             { get; private set; }
    public Vector3 Position           { get; private set; }
    public Vector3 Remainder          { get; private set; }
    public Vector3 Travel             { get; private set; }

    public static implicit operator CollisionContact(KinematicCollision kinematicCollision) =>
        new CollisionContact
        {
            Collider           = kinematicCollision.Collider,
            ColliderId         = kinematicCollision.ColliderId,
            ColliderMetadata   = kinematicCollision.ColliderMetadata,
            ColliderShape      = kinematicCollision.ColliderShape,
            ColliderShapeIndex = kinematicCollision.ColliderShapeIndex,
            ColliderVelocity   = kinematicCollision.ColliderVelocity,
            LocalShape         = kinematicCollision.LocalShape,
            Normal             = kinematicCollision.Normal,
            Position           = kinematicCollision.Position,
            Remainder          = kinematicCollision.Remainder,
            Travel             = kinematicCollision.Travel,
        };

}

public abstract class CharacterController3D : KinematicBody
{
    const float BODY_SIZE_MARGIN = 0.001f;
    const float ANGLE_THRESHOLD  = 0.01f;

    private float mass          = 1;
    private float maxSlopeAngle = 0.785398f;
    private float wallAngle     = 1.39626f;
    private float weight        = 9.8f;

    public CollisionState CollisionState { get; private set; }
    public Vector3        FloorNormal    { get; private set; }
    public Vector3        FloorVelocity  { get; private set; }

    public CollisionShape BottomCollisionShape { get; set; }
    public Vector3        LinearVelocity       { get; set; }
    public int            MaxSlides            { get; set; } = 4;
    public bool           Snap                 { get; set; }
    public float          SnapDetectionLength  { get; set; } = 0.5f;
    public CollisionShape TopCollisionShape    { get; set; }

    [Export]
    public Vector3 Gravity { get; set; } = Vector3.Down;

    [Export]
    public float GravityForce { get; set; } = 9.8f;

    [Export]
    public DetectionMode DetectionMode { get; set; } = DetectionMode.Angle;

    [Export]
    public float BodySize { get; set; } = 0.5f;

    [Export]
    public bool CanPush { get; set; }

    [Export]
    public bool CanSlide { get; set; }

    [Export]
    public float SlideVelocity { get; set; } = 4f;

    [Export]
    public bool StopOnSlopes { get; set; }

    [Export]
    public float StepOffset { get; set; } = 0.5f;

    [Export(PropertyHint.Range, "0,90")]
    public float MaxSlopeAngle
    {
        get => Mathf.Rad2Deg(this.maxSlopeAngle);
        set => this.maxSlopeAngle = Mathf.Deg2Rad(value);
    }

    [Export(PropertyHint.Range, "0,90")]
    public float WallAngle
    {
        get => Mathf.Rad2Deg(this.wallAngle);
        set => this.wallAngle = Mathf.Deg2Rad(value);
    }

    [Export]
    public float Mass
    {
        get => this.mass;
        set
        {
            this.mass   = value;
            this.weight = value / this.GravityForce;
        }
    }

    [Export]
    public float Weight
    {
        get => this.weight;
        set
        {
            this.weight = value;
            this.mass   = value / this.GravityForce;
        }
    }

    private bool ShapeHasRoundedCorners(CollisionShape collisionShape) =>
        collisionShape.Shape is CapsuleShape || collisionShape.Shape is SphereShape;

    private bool IsValidSlopeAngle(Vector3 normal) =>
        (-this.Gravity).AngleTo(normal) <= this.maxSlopeAngle + ANGLE_THRESHOLD;

    private bool IsValidSlopeAngle(KinematicCollision collision)
    {
        if (this.IsValidSlopeAngle(collision.Normal))
        {
            return true;
        }

        var state  = this.GetWorld().DirectSpaceState;
        var origin = collision.Position + this.Gravity * -0.001f;

        var ray = state.IntersectRay(origin, origin + -collision.Normal, new Godot.Collections.Array { this });

        if (ray.Count > 0)
        {
            return this.IsValidSlopeAngle((Vector3)ray["normal"]);
        }

        return false;
    }

    private CollisionState GetCollisionState(KinematicCollision collision)
    {
        if (this.DetectionMode == DetectionMode.Collider)
        {
            if (collision.LocalShape == this.BottomCollisionShape)
            {
                return CollisionState.Bottom;
            }
            else if (collision.LocalShape == this.TopCollisionShape)
            {
                return CollisionState.Top;
            }

            return CollisionState.Sides;
        }
        else if (this.DetectionMode == DetectionMode.Offset)
        {
            var bodyOffset = this.BodySize / 2 + BODY_SIZE_MARGIN;

            var origin         = this.GlobalTransform.origin + collision.Travel;
            var topPosition    = origin + -this.Gravity * bodyOffset;
            var bottomPosition = origin + this.Gravity  * bodyOffset;

            if ((collision.Position - bottomPosition).Dot(this.Gravity) > 0)
            {
                return CollisionState.Bottom;
            }
            else if ((collision.Position - topPosition).Dot(-this.Gravity) > 0)
            {
                return CollisionState.Top;
            }

            return CollisionState.Sides;
        }
        else
        {
            if ((-this.Gravity).AngleTo(collision.Normal) <= this.wallAngle + ANGLE_THRESHOLD)
            {
                return CollisionState.Bottom;
            }
            else if ((-this.Gravity).AngleTo(-collision.Normal) <= this.wallAngle + ANGLE_THRESHOLD)
            {
                return CollisionState.Top;
            }

            return CollisionState.Sides;
        }
    }

    private bool TryMoveStep(KinematicCollision collision, Vector3 horizontalMotion)
    {
        if (!this.ShapeHasRoundedCorners(collision.LocalShape as CollisionShape))
        {
            var tranform = this.GlobalTransform;

            var previous = tranform.origin;

            tranform.origin = tranform.origin + -this.Gravity * this.StepOffset;

            this.GlobalTransform = tranform;

            var stepCollision = this.MoveAndCollide(horizontalMotion, testOnly: true);

            if (stepCollision == null)
            {
                tranform.origin += horizontalMotion;

                this.GlobalTransform = tranform;

                var descendMotion = this.Gravity * this.StepOffset;

                var groundCollision = this.MoveAndCollide(this.Gravity * this.StepOffset, testOnly: true);

                if (groundCollision != null)
                {
                    var collisionState = this.GetCollisionState(groundCollision);

                    if (collisionState == CollisionState.Bottom && this.IsValidSlopeAngle(groundCollision.Normal))
                    {
                        tranform.origin += groundCollision.Travel;

                        this.GlobalTransform = tranform;

                        return true;
                    }
                }
            }

            tranform.origin = previous;

            this.GlobalTransform = tranform;
        }

        return false;
    }

    public IEnumerable<CollisionContact> MoveAndSlide()
    {
        var tranform = this.GlobalTransform;
        var delta    = Engine.IsInPhysicsFrame() ? this.GetPhysicsProcessDeltaTime() : this.GetProcessDeltaTime();

        this.LinearVelocity += this.Gravity * this.GravityForce * delta;

        var velocity           = this.LinearVelocity;
        var velocityNormalized = velocity.Normalized();
        var verticalVelocity   = velocity * -this.Gravity;
        var horizontalVelocity = velocity - verticalVelocity;
        var motion             = (this.LinearVelocity + this.FloorVelocity) * delta;
        var slides             = this.MaxSlides;
        var collisions         = new HashSet<CollisionContact>();
        var wasOnFloor         = this.CollisionState.HasFlag(CollisionState.Bottom);

        this.CollisionState = default;
        this.FloorNormal    = default;
        this.FloorVelocity  = default;

        while (slides > 0)
        {
            var collision = this.MoveAndCollide(motion, testOnly: true);

            var displacement = motion;

            motion = Vector3.Zero;

            if (collision != null)
            {
                displacement  = collision.Travel;
                motion        = collision.Remainder;

                var horizontalMotion = motion - motion * -this.Gravity;

                collisions.Add(collision);

                var collisionState = this.GetCollisionState(collision);

                var pushed = false;

                if (this.CanPush && collision.Collider is RigidBody rigidBody)
                {
                    var contactVelocity = ((2 * this.Mass) / (this.Mass + rigidBody.Mass)) * velocity + ((rigidBody.Mass - this.Mass) / (this.Mass + rigidBody.Mass)) * collision.ColliderVelocity;

                    pushed = true;

                    var surface = contactVelocity.Slide(collision.Normal).Normalized();

                    rigidBody.ApplyImpulse(collision.Position - rigidBody.GlobalTransform.origin, contactVelocity.Slide(surface));

                    this.FloorVelocity = Vector3.Zero;
                }

                motion   = motion.Slide(collision.Normal);
                velocity = velocity.Slide(collision.Normal);

                if (collisionState == CollisionState.Bottom)
                {
                    this.FloorNormal   = collision.Normal;
                    this.FloorVelocity = pushed ? Vector3.Zero : collision.ColliderVelocity;

                    if (this.IsValidSlopeAngle(collision))
                    {
                        if (this.CollisionState.HasFlag(CollisionState.Sides))
                        {
                            velocity = velocity.Slide(-this.Gravity);
                            motion   = motion.Slide(-this.Gravity);
                        }
                        else if (this.CollisionState.HasFlag(CollisionState.Sliding) || (this.StopOnSlopes && (horizontalVelocity + this.FloorVelocity) == Vector3.Zero))
                        {
                            displacement = motion = velocity = Vector3.Zero;
                        }
                    }
                    else if (!pushed && !this.CollisionState.HasFlag(CollisionState.Sliding))
                    {
                        if (!this.TryMoveStep(collision, horizontalMotion))
                        {
                            if (this.CanSlide && (horizontalVelocity == Vector3.Zero || horizontalVelocity.Dot(collision.Normal) < 0))
                            {
                                tranform.origin += displacement;

                                this.GlobalTransform = tranform;

                                velocity = (this.Gravity * this.SlideVelocity);

                                var slideCollision = this.MoveAndCollide(velocity * delta, testOnly: true);

                                if (slideCollision != null)
                                {
                                    displacement = slideCollision.Travel;
                                    velocity     = velocity.Slide(slideCollision.Normal);
                                    motion       = slideCollision.Remainder;
                                }

                                this.CollisionState |= CollisionState.Sliding;
                            }
                        }
                        else
                        {
                            tranform = this.GlobalTransform;

                            displacement = motion = velocity = Vector3.Zero;
                        }
                    }
                }
                else if (collisionState == CollisionState.Sides)
                {
                    if (this.CollisionState.HasFlag(CollisionState.Bottom))
                    {
                        displacement = motion = velocity = Vector3.Zero;
                    }
                    else if (wasOnFloor && this.TryMoveStep(collision, horizontalMotion))
                    {
                        tranform = this.GlobalTransform;

                        displacement = motion = velocity = Vector3.Zero;
                    }
                }

                this.CollisionState |= collisionState;
            }

            tranform.origin += displacement;

            this.GlobalTransform = tranform;

            if (motion == Vector3.Zero)
            {
                break;
            }

            slides--;

            if (slides == 0)
            {
                velocity = Vector3.Zero;
            }
        }

        if (this.Snap && wasOnFloor && !this.CollisionState.HasFlag(CollisionState.Bottom))
        {
            var collision = this.MoveAndCollide(this.Gravity * this.SnapDetectionLength, testOnly: true);

            if (collision != null)
            {
                collisions.Add(collision);

                this.FloorNormal   = collision.Normal;
                this.FloorVelocity = collision.ColliderVelocity;

                velocity = velocity.Slide(collision.Normal);

                this.CollisionState |= this.GetCollisionState(collision);

                if (this.CollisionState.HasFlag(CollisionState.Bottom) && !this.IsValidSlopeAngle(collision.Normal))
                {
                    this.CollisionState |= CollisionState.Sliding;
                }

                tranform.origin += collision.Travel;

                this.GlobalTransform = tranform;
            }
        }

        this.LinearVelocity = velocity;

        return collisions;
    }
}
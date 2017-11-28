using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Navigation : Module
        {
            public bool UseInertialTensor { get; } = false;
            public Navigation(Ship ship) : base(ship)
            {

            }
            public override void SetEnabled(bool active)
            {
                ship.Rotation.SetEnabled(active);
                ship.Propulsion.SetEnabled(active);
                base.SetEnabled(active);
            }
            public override bool CanInitialize
            {
                get
                {
                    return ship.Propulsion.Initialized && ship.Rotation.Initialized;
                }
            }
            public void Initialize()
            {
                if (CanInitialize)
                {
                    ship.Logger.Log("Navigation Module Initialized!");
                    Initialized = true;
                }
            }
            public void MaintainVelocity(Vector3D vel,bool UseOnlyGravityForDown = false, float multiplier = 30f, double maxaccel = 0)
            {
                if (IsReady && ship.Propulsion.IsReady)
                {
                    var velocity = ship.ShipData.Velocity.LinearVelocity;
                    var mass = ship.ShipData.Mass.PhysicalMass;
                    var grav = ship.ShipData.NaturalGravity;
                    var vellength = vel.Length();
                    if (vellength > Ship.speedLimit)
                    {
                        vel.Normalize();
                        vel = vel * Ship.speedLimit;
                    }
                    var force = (vel - velocity) * mass * multiplier;
                    if (maxaccel > 0 && force.LengthSquared() != 0)
                    {
                        force = Vector3D.Normalize(force) * Math.Min(maxaccel * mass, force.Length());
                    }
                    var maxforce = ship.Propulsion.GetMaxThrustTowardsAxis(force, true,UseOnlyGravityForDown);
                    if (force.LengthSquared() > maxforce*maxforce)
                    {

                        force.Normalize();
                        force = force * maxforce;
                    }

                    force = Vector3D.TransformNormal(force - grav * mass, MatrixD.Invert(ship.ControllerBlock.WorldMatrix.GetOrientation()));

                    ship.Propulsion.thrust(force);
                }

            }
            public bool TranslateToVector(Vector3D pos, float dampening = 1f, float multiplier = 1f, float maxspeed = 0)
            {
                if (IsReady && ship.Propulsion.IsReady)
                {

                    var mass = ship.ShipData.Mass.PhysicalMass;
                    var speeds = ship.ShipData.Velocity;
                    var linvel = speeds.LinearVelocity;
                    var grav = ship.ControllerBlock.GetNaturalGravity();
                    var vec = pos - ship.ControllerBlock.GetPosition();
                    // Get maxthrusttowardsaxis simply finds out how much force the ship can apply in a certain direction 
                    var maxaccelaway = ship.Propulsion.GetMaxThrustTowardsAxis(-vec, true) / mass * .9;

                    var targspeed = Math.Sqrt((vec + linvel * 1 / 60).Length() * 2 * maxaccelaway);
                    if (maxspeed > 0) targspeed = targspeed > maxspeed ? maxspeed : targspeed;
                    Vector3D targvel = Vector3D.Normalize(vec) * targspeed;
                    if (vec.Length() < 0.05)
                    {
                        //MaintainVelocity simply maintains a certain velocity vector with magic 
                        MaintainVelocity(Vector3D.Zero);
                        return true;
                    }
                    MaintainVelocity(targvel);

                }
                return false;
            }

            public bool OrientMatrixTowardsMatrix(Matrix Dir, Matrix localVector)
            {
                if (IsReady && ship.Rotation.IsReady)
                {
                    Matrix orientation;
                    ship.ControllerBlock.Orientation.GetMatrix(out orientation);
                    Dir = Dir * Matrix.Transpose(ship.ControllerBlock.WorldMatrix.GetOrientation());
                    Dir = Dir * (Matrix.Invert(localVector));
                    var qerr = Quaternion.CreateFromRotationMatrix(Dir.GetOrientation());
                    Vector3 crossvector;
                    float ang;
                    qerr.GetAxisAngle(out crossvector, out ang);

                    crossvector = Vector3.Transform(crossvector, localVector);

                    var angvector = (Vector3D)(crossvector) * (double)ang;
                    angvector = Vector3D.Transform(angvector, orientation);
                    angvector = UseInertialTensor ? -ship.Rotation.CalculateTargetAngularVelocity(angvector) : -angvector;
                    foreach (IMyGyro gyro in ship.Rotation.gyros)
                    {
                        Matrix orient;

                        gyro.Orientation.GetMatrix(out orient);
                        MatrixD invGyroMatrix = MatrixD.Invert(orient);
                        Vector3D angle = Vector3D.Transform(angvector, invGyroMatrix);
                        gyro.Pitch=((float)angle.X);
                        gyro.Yaw = ((float)angle.Y);
                        gyro.Roll=((float)angle.Z);
                    }

                    return ang < 0.01;
                }
                return false;
            }
            public bool OrientVectorTowardsDirection(Vector3D Dir, Vector3D localVector)
            {
                if (IsReady && ship.Rotation.IsReady)
                {
                    Matrix orientation;
                    ship.ControllerBlock.Orientation.GetMatrix(out orientation);
                    Dir = Vector3D.Transform(Dir, Matrix.Invert(ship.ControllerBlock.WorldMatrix.GetOrientation()));
                    var qerr = Quaternion.CreateFromTwoVectors(localVector, Dir);

                    Vector3 crossvector;
                    float ang;
                    qerr.GetAxisAngle(out crossvector, out ang);


                    var angvector = (Vector3D)(crossvector) * (double)ang;
                    angvector = Vector3D.Transform(angvector, orientation);
                    angvector = UseInertialTensor ? -ship.Rotation.CalculateTargetAngularVelocity(angvector) : -angvector;
                    foreach (IMyGyro gyro in ship.Rotation.gyros)
                    {
                        Matrix orient;

                        gyro.Orientation.GetMatrix(out orient);
                        MatrixD invGyroMatrix = MatrixD.Invert(orient);
                        Vector3D angle = Vector3D.Transform(angvector, invGyroMatrix);
                        gyro.Pitch = ((float)angle.X);
                        gyro.Yaw = ((float)angle.Y);
                        gyro.Roll = ((float)angle.Z);
                    }
                    return ang < 0.01;
                }
                return false;
            }
        }
    }
}

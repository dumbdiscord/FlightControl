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
        public class Rotation : Module
        {
            public static double LargeGyroTorque = 33600000;
            public static double SmallGyroTorque = 448000;
            public double MaxTorque { get; private set; }
            public List<IMyGyro> gyros { get; private set; }
            public MatrixD InertialTensor { get; private set; }
            public bool UseOldMethod { get; } = false;
            int? m_overrideAccelerationRampFrames = null;
            public override void SetEnabled(bool active)
            {
                if (Initialized)
                {
                    foreach (var i in gyros)
                    {
                        i.GyroOverride = active;
                        if (active)
                        {
                            i.SetValueFloat("Pitch", 0f);
                            i.SetValueFloat("Yaw", 0f);
                            i.SetValueFloat("Roll", 0f);
                        }
                    }

                }
                
                base.SetEnabled(active);
            }
            public void Initialize()
            {
                if (CanInitialize)
                {
                    PopulateGyros();
                    InertialTensor = CalculateInertialTensor();
                    ship.Logger.Log("Gyro Module Initialized!");
                    Initialized = true;
                }

            }
            public Rotation(Ship ship) : base(ship) { }
            public void PopulateGyros()
            {
                List<IMyGyro> gyros = new List<IMyGyro>();
                ship.GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros, x =>
                {
                    if (x.BlockDefinition.SubtypeId.Contains("Small"))
                    {
                        MaxTorque += SmallGyroTorque;
                    }
                    else if (x.BlockDefinition.SubtypeId.Contains("Large"))
                    {
                        MaxTorque += LargeGyroTorque;
                    }
                    return true;

                });
                this.gyros = gyros;
            }
            public MatrixD CalculateInertialTensor()
            {
                MatrixD finalTensor = MatrixD.Zero;
                double mass = ship.ControllerBlock.CalculateShipMass().BaseMass;
                BoundingBoxD box = new BoundingBoxD(((Vector3)ship.ControllerBlock.CubeGrid.Min - 0.5f) * ship.ControllerBlock.CubeGrid.GridSize, ((Vector3)ship.ControllerBlock.CubeGrid.Max + .5f) * ship.ControllerBlock.CubeGrid.GridSize);
                var corners = box.GetCorners();
                Vector3D localCom = Vector3D.Transform(ship.ControllerBlock.CenterOfMass, Matrix.Invert(ship.ControllerBlock.CubeGrid.WorldMatrix));
                foreach (var c in corners)
                {
                    Vector3D d = localCom - c;
                    MatrixD tensor = new MatrixD(
                    d.Z * d.Z + d.Y * d.Y, 0, 0,
                    0, d.Z * d.Z + d.X * d.X, 0,
                    0, 0, d.X * d.X + d.Y * d.Y);
                    tensor *= mass / 96;
                    d /= 2;
                    finalTensor += tensor + (mass / 8) * (d.LengthSquared() * MatrixD.Identity - MathUtils.OuterProduct(d, d));
                }

                var blocks = new List<IMyTerminalBlock>();
                ship.GridTerminalSystem.GetBlocks(blocks);
                MatrixD inversegrid = Matrix.Invert(ship.ControllerBlock.CubeGrid.WorldMatrix);
                foreach (var block in blocks)
                {
                    if (block.HasInventory)
                    {
                        double totalmass = 0;
                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            totalmass += block.GetInventory(i).CurrentMass.RawValue/1e6;
                        }
                        if (totalmass > 0)
                        {
                            Vector3D localPos = Vector3D.Transform(block.GetPosition(), inversegrid) - localCom;
                            finalTensor += totalmass*(MatrixD.Identity * localPos.LengthSquared() - MathUtils.OuterProduct(localPos, localPos));
                        }
                    }
                }
                finalTensor.M44 = 1;
                return finalTensor;
            }
            private void UpdateOverrideAccelerationRampFrames(Vector3 velocityDiff)
            {
                if (m_overrideAccelerationRampFrames == null)
                {
                    float squaredSpeed = velocityDiff.LengthSquared();
                    float MIN_ROTATION_SPEED_SQ_TH = (float)((Math.PI / 2) * (Math.PI / 2));
                    int ACCELARION_RAMP_FRAMES = 60 * 2;
                    if (squaredSpeed > MIN_ROTATION_SPEED_SQ_TH)
                    {
                        m_overrideAccelerationRampFrames = ACCELARION_RAMP_FRAMES;
                    }
                    else
                    {
                        float K_PROP_ACCEL = (ACCELARION_RAMP_FRAMES - 1) / MIN_ROTATION_SPEED_SQ_TH;
                        m_overrideAccelerationRampFrames = (int)(squaredSpeed * K_PROP_ACCEL) + 1;
                    }   
                }
                else if (m_overrideAccelerationRampFrames > 1)
                {
                    m_overrideAccelerationRampFrames--;
                }
            }

            public Vector3D CalculateTargetAngularVelocity(Vector3D angdist)
            {
                Matrix orient;
                ship.ControllerBlock.Orientation.GetMatrix(out orient);
                MatrixD worldtogrid = MatrixD.Transpose(ship.ControllerBlock.WorldMatrix.GetOrientation())*orient;
                Vector3D angularvel = Vector3D.TransformNormal(ship.ControllerBlock.GetShipVelocities().AngularVelocity, worldtogrid);
                
                double inertia = Vector3D.Dot(Vector3D.Transform(Vector3D.Normalize(angdist), InertialTensor), Vector3D.Normalize(angdist));
                double angularacceleration = (MaxTorque / inertia);

                var targvel= Vector3D.Normalize(angdist) * Math.Sqrt(2 * angdist.Length() * angularacceleration * .9);
                if (UseOldMethod) return targvel;
                angularacceleration *= (m_overrideAccelerationRampFrames.GetValueOrDefault() / 60f);
                Vector3D veldiff = targvel - angularvel;
                m_overrideAccelerationRampFrames = null;
                UpdateOverrideAccelerationRampFrames(veldiff);
                return Vector3D.Normalize(veldiff) * angularacceleration + angularvel;

            }
        }

    }
}

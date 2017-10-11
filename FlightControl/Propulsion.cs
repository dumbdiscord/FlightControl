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
        public class Propulsion : Module
        {
            List<IMyTerminalBlock> upThrusts;
            public List<IMyTerminalBlock> downThrusts;
            List<IMyTerminalBlock> leftThrusts;
            List<IMyTerminalBlock> rightThrusts;
            List<IMyTerminalBlock> forThrusts;
            List<IMyTerminalBlock> backThrusts;
            public override void SetEnabled(bool active)
            {
                if (Initialized && !active)
                {
                    thrust(Vector3D.Zero);
                }
                ship.ControllerBlock.DampenersOverride = !active;
                base.SetEnabled(active);
            }
            public void Initialize()
            {
                if (CanInitialize)
                {
                    GetThrusters();
                    ship.Logger.Log("Propulsion Module Initialized!");
                    Initialized = true;
                }
            }
            void GetThrusters()
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                ship.GridTerminalSystem.GetBlocksOfType<IMyThrust>(blocks);
                upThrusts = blocks.Where(thrust => { return thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Up; }).ToList();
                downThrusts = blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Down).ToList();
                leftThrusts = blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Left).ToList();
                rightThrusts = blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Right).ToList();
                forThrusts = blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Forward).ToList();
                backThrusts = blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Backward).ToList();

            }
            public Propulsion(Ship ship) : base(ship)
            {
                upThrusts = new List<IMyTerminalBlock>();
                downThrusts = new List<IMyTerminalBlock>();
                leftThrusts = new List<IMyTerminalBlock>();
                rightThrusts = new List<IMyTerminalBlock>();
                forThrusts = new List<IMyTerminalBlock>();
                backThrusts = new List<IMyTerminalBlock>();
            }
            // thanks to fakuctagroil#3564 on discord for letting my use his code
            public List<List<IMyTerminalBlock>> GetThrustersSetsTowardAxis(Vector3D axis)
            {
                axis = Vector3D.Normalize(axis);
                List<List<IMyTerminalBlock>> output = new List<List<IMyTerminalBlock>>();
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Up, axis) > 0)
                {
                    output.Add(downThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Down, axis) > 0)
                {
                    output.Add(upThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Left, axis) > 0)
                {
                    output.Add(rightThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Right, axis) > 0)
                {
                    output.Add(leftThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Forward, axis) > 0)
                {
                    output.Add(backThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Backward, axis) > 0)
                {
                    output.Add(forThrusts);
                }
                return output;
            }
            public List<IMyTerminalBlock> GetThrustersTowardAxis(Vector3D axis)
            {
                axis = Vector3D.Normalize(axis);
                List<IMyTerminalBlock> output = new List<IMyTerminalBlock>();
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Up, axis) > 0)
                {
                    output.AddRange(downThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Down, axis) > 0)
                {
                    output.AddRange(upThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Left, axis) > 0)
                {
                    output.AddRange(rightThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Right, axis) > 0)
                {
                    output.AddRange(leftThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Forward, axis) > 0)
                {
                    output.AddRange(backThrusts);
                }
                if (Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Backward, axis) > 0)
                {
                    output.AddRange(forThrusts);
                }
                return output;
            }
            public enum ThrusterType
            {
                Hydrogen,
                Atmo,
                Ion,
            }
            public ThrusterType? GetThrusterType(IMyTerminalBlock block)
            {
                if (block.BlockDefinition.SubtypeId.Contains("Hydro"))
                {
                    return ThrusterType.Hydrogen;
                }
                else if (block.BlockDefinition.SubtypeId.Contains("Atmo"))
                {
                    return ThrusterType.Atmo;
                }
                else if (block.BlockDefinition.SubtypeId.Contains("Ion"))
                {
                    return ThrusterType.Ion;
                }
                else { return null; }
            }
            public Vector3D GetThrusterDirectionFromList(List<IMyTerminalBlock> thrust)
            {
                if (thrust == downThrusts)
                {
                    return ship.ControllerBlock.WorldMatrix.Up;
                }
                else if (thrust == upThrusts)
                {
                    return ship.ControllerBlock.WorldMatrix.Down;
                }
                else if (thrust == rightThrusts)
                {
                    return ship.ControllerBlock.WorldMatrix.Left;
                }
                else if (thrust == leftThrusts)
                {
                    return ship.ControllerBlock.WorldMatrix.Right;
                }
                else if (thrust == forThrusts)
                {
                    return ship.ControllerBlock.WorldMatrix.Backward;
                }
                else if (thrust == backThrusts)
                {
                    return ship.ControllerBlock.WorldMatrix.Forward;
                }
                return Vector3D.Zero;
            }
            public void thrust(Vector3D dir)
            {
                if (dir.X < 0)
                {
                    ThrustNewtons(rightThrusts, (float)-dir.X);
                    ThrustNewtons(leftThrusts, 0);
                }
                else if (dir.X > 0)
                {
                    ThrustNewtons(leftThrusts, (float)dir.X);
                    ThrustNewtons(rightThrusts, 0);
                }
                else
                {
                    ThrustNewtons(leftThrusts, 0);
                    ThrustNewtons(rightThrusts, 0);
                }
                if (dir.Y < 0)
                {
                    ThrustNewtons(upThrusts, (float)-dir.Y);
                    ThrustNewtons(downThrusts, 0);
                }
                else if (dir.Y > 0)
                {
                    ThrustNewtons(downThrusts, (float)dir.Y);
                    ThrustNewtons(upThrusts, 0);
                }
                else
                {
                    ThrustNewtons(upThrusts, 0);
                    ThrustNewtons(downThrusts, 0);
                }
                if (dir.Z < 0)
                {
                    ThrustNewtons(backThrusts, (float)-dir.Z);
                    ThrustNewtons(forThrusts, 0);
                }
                else if (dir.Z > 0)
                {
                    ThrustNewtons(forThrusts, (float)dir.Z);
                    ThrustNewtons(backThrusts, 0);
                }
                else
                {
                    ThrustNewtons(forThrusts, 0);
                    ThrustNewtons(backThrusts, 0);
                }
            }
            public float CalculateMaxThrust(List<IMyTerminalBlock> thrusters)
            {
                float maxthrust = 0;
                foreach (IMyTerminalBlock block in thrusters)
                {
                    if ((block as IMyFunctionalBlock).Enabled)
                    {
                        maxthrust += (block as IMyThrust).MaxEffectiveThrust;
                    }
                }
                return maxthrust;

            }
            public void ThrustNewtons(List<IMyTerminalBlock> thrusters, float newtons)
            {
                float maxthrust = CalculateMaxThrust(thrusters);
                foreach (IMyTerminalBlock block in thrusters)
                {
                    block.SetValueFloat("Override", newtons / maxthrust * 100 > 100 ? 100 : newtons / maxthrust * 100);
                }
            }

            // Thanks to Equinox from the KSH discord for making this work with gravity properly 
            public double GetMaxThrustTowardsAxis(Vector3D axis, bool withGravity = false,bool useonlygravityfordown=false, Func<IMyTerminalBlock, bool> condition = null)
            {
                if (axis == Vector3D.Zero)
                {
                    return 0;
                }

                axis = Vector3D.Normalize(axis);
                var activethrusters = GetThrustersSetsTowardAxis(axis);
                //if (condition != null) 
                //{ 
                //    activethrusters = activethrusters.SelectMany<List<IMyTerminalBlock>>().Where(condition) as List<IMyTerminalBlock>; 
                //} 
                var grav = ship.ControllerBlock.GetNaturalGravity();
                var mass = ship.ControllerBlock.CalculateShipMass().PhysicalMass;
                double maxthrust = 0;
                List<double> maxthrusts = new List<double>();
                foreach (List<IMyTerminalBlock> thruster in activethrusters)
                {
                    var d = GetThrusterDirectionFromList(thruster);
                    double thrust = CalculateMaxThrust(thruster);
                    double angbetween = Vector3D.Dot(axis, Vector3D.Normalize(grav));
                    if (useonlygravityfordown&& angbetween> 0)
                    {
                        thrust = thrust-thrust * angbetween;
                        d = Vector3D.Reject(d, Vector3D.Normalize(grav));
                    }

                    var gravlocal = withGravity && !double.IsNaN(grav.Length()) ? Vector3D.Dot(grav * mass, d) : 0;
                    var mthrust = (thrust + gravlocal) / Vector3D.Dot(d, axis);

                    maxthrusts.Add(mthrust);

                }
                if (maxthrusts.Count == 0) { return 0; }
                return maxthrust + maxthrusts.Min();
            }

        }
    }
}

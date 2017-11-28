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
            HashSet<IMyTerminalBlock> upThrusts;
            HashSet<IMyTerminalBlock> downThrusts;
            HashSet<IMyTerminalBlock> leftThrusts;
            HashSet<IMyTerminalBlock> rightThrusts;
            HashSet<IMyTerminalBlock> forThrusts;
            HashSet<IMyTerminalBlock> backThrusts;
            public HashSet<IMyTerminalBlock> Thrusters { get; private set; }
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
                Thrusters = new HashSet<IMyTerminalBlock>(blocks);
                upThrusts = new HashSet<IMyTerminalBlock>(blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Up));
                downThrusts = new HashSet<IMyTerminalBlock>(blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Down));
                leftThrusts = new HashSet<IMyTerminalBlock>(blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Left));
                rightThrusts = new HashSet<IMyTerminalBlock>(blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Right));
                forThrusts = new HashSet<IMyTerminalBlock>(blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Forward));
                backThrusts = new HashSet<IMyTerminalBlock>(blocks.Where(thrust => thrust.WorldMatrix.Forward == ship.ControllerBlock.WorldMatrix.Backward));
                
               
            }
            public Propulsion(Ship ship) : base(ship)
            {
            }
            // thanks to fakuctagroil#3564 on discord for letting my use his code
            public List<HashSet<IMyTerminalBlock>> GetThrustersSetsTowardAxis(Vector3D axis)
            {
                axis = Vector3D.Normalize(axis);
                List<HashSet<IMyTerminalBlock>> output = new List<HashSet<IMyTerminalBlock>>();
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
                double dot = Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Up, axis);
                if (dot > 0)
                {
                    output.AddRange(downThrusts);
                }
                else if (dot!=0)
                {
                    output.AddRange(upThrusts);
                }
                dot = Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Left, axis);
                if ( dot> 0)
                {
                    output.AddRange(rightThrusts);
                }
                else if (dot!=0)
                {
                    output.AddRange(leftThrusts);
                }
                dot = Vector3D.Dot(ship.ControllerBlock.WorldMatrix.Forward, axis);
                if (dot> 0)
                {
                    output.AddRange(backThrusts);
                }
                else if (dot !=0)
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
            public Vector3D GetThrusterDirectionFromList(HashSet<IMyTerminalBlock> thrust)
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
            public float CalculateMaxThrust(HashSet<IMyTerminalBlock> thrusters, bool shouldbeenabled = true,Func<IMyThrust,bool> predicate=null)
            {
                float maxthrust = 0;
                foreach (IMyThrust block in thrusters)
                {
                    if (!shouldbeenabled|| (block as IMyFunctionalBlock).Enabled)
                    {
                        if (predicate != null)
                        {
                            if(predicate(block)) maxthrust += block.MaxEffectiveThrust;
                            continue;
                        }
                        maxthrust += block.MaxEffectiveThrust;
                    }
                }
                return maxthrust;

            }
            public void ThrustNewtons(HashSet<IMyTerminalBlock> thrusters, float newtons)
            {
                float maxthrust = CalculateMaxThrust(thrusters);
                foreach (IMyThrust block in thrusters)
                {
                    block.ThrustOverridePercentage=(newtons / maxthrust > 1 ? 1 : newtons / maxthrust);
                }
            }
            public double GetMaxThrustTowardsAxis(Vector3D axis, List<IMyThrust> thrusts,bool withGravity = false,bool useonlygravityfordown = false,bool shouldbeenabled = true)
            {
                if (axis == Vector3D.Zero)
                {
                    return 0;
                }
                
                axis = Vector3D.Normalize(axis);
                //if (condition != null) 
                //{ 
                //    activethrusters = activethrusters.SelectMany<List<IMyTerminalBlock>>().Where(condition) as List<IMyTerminalBlock>; 
                //} 
                var grav = ship.ShipData.NaturalGravity;
                var mass = ship.ShipData.Mass.PhysicalMass;
                var gravforce = grav * mass;
                double maxthrust = 0;
                List<double> maxthrusts = new List<double>();
                foreach (var thruster in thrusts)
                {

                    var d = thruster.WorldMatrix.Forward;


                    var gravlocal = withGravity && !double.IsNaN(grav.Length()) ? Vector3D.Dot(gravforce, d) : 0;
                    var mthrust = (thruster.MaxEffectiveThrust+ gravlocal) / Vector3D.Dot(d, axis);

                    maxthrusts.Add(mthrust);

                }

                if (maxthrusts.Count == 0) { return 0; }
                maxthrust = maxthrust + maxthrusts.Min();
                if (useonlygravityfordown)
                {
                    var gravdot = Vector3D.Dot(axis * maxthrust, Vector3D.Normalize(grav));
                    if (gravdot * gravdot > gravforce.LengthSquared())
                    {
                        maxthrust *= gravforce.Length() / gravdot;
                    }
                }
                return maxthrust;
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
                var grav = ship.ShipData.NaturalGravity;
                var mass = ship.ShipData.Mass.PhysicalMass;
                var gravforce = grav * mass;
                double maxthrust = 0;
                List<double> maxthrusts = new List<double>();
                foreach (HashSet<IMyTerminalBlock> thruster in activethrusters)
                {
                    var d = GetThrusterDirectionFromList(thruster);
                    

                    var gravlocal = withGravity && !double.IsNaN(grav.Length()) ? Vector3D.Dot(gravforce, d) : 0;
                    var mthrust = (CalculateMaxThrust(thruster) + gravlocal) / Vector3D.Dot(d, axis);

                    maxthrusts.Add(mthrust);

                }

                if (maxthrusts.Count == 0) { return 0; }
                maxthrust= maxthrust + maxthrusts.Min();
                if (useonlygravityfordown)
                {
                    var gravdot = Vector3D.Dot(axis*maxthrust, Vector3D.Normalize(grav));
                    if (gravdot*gravdot > gravforce.LengthSquared())
                    {
                        maxthrust *= gravforce.Length() / gravdot;
                    }
                }
                return maxthrust;
            }

        }
    }
}

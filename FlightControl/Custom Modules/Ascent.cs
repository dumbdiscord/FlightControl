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
        public class Ascent : Module
        {
            bool Ascending = false;
            bool HydrosOnYet = false;
            List<IMyThrust> Hydros;
            int stage = 0;
            float escapespeed=0;
            public void Initialize()
            {
                if (CanInitialize)
                {
                    GetHydroList();
                    Initialized = true;
                }
            }
            public override void Tick()
            {
                if (IsReady)
                {
                    if (Ascending&&ship.Planets.CurrentPlanet!=null)
                    {

                        Vector3D grav = ship.ControllerBlock.GetNaturalGravity();
                        
                        if (!double.IsNaN(grav.Length()))
                        {
                            //ship.Navigation.orientVectorTowardsDirection(Vector3D.Normalize(grav), Vector3D.Down);
                            if (stage == 0)
                            {
                                ship.Navigation.MaintainVelocity(-Vector3D.Normalize(grav) * Ship.speedLimit);

                                if (ship.Propulsion.GetMaxThrustTowardsAxis(-Vector3D.Normalize(grav)) / ship.ControllerBlock.CalculateShipMass().PhysicalMass <= grav.Length())
                                {
                                    Hydros.ForEach((x) => x.Enabled = true);
                                    HydrosOnYet = true;
                                }
                                double exitenergy = ship.Planets.CurrentPlanet.GetGravitationalPotential(ship.Planets.CurrentPlanet.GetGravityEdge()) + escapespeed * escapespeed / 2;
                                double curenergy = ship.Planets.CurrentPlanet.GetGravitationalPotential(Vector3D.Distance(ship.Planets.CurrentPlanet.Center, ship.ControllerBlock.GetPosition()));//Math.Pow(-ship.Planets.CurrentPlanet.GravityMathNumber / (6 * Vector3D.Distance(ship.Planets.CurrentPlanet.Center, ship.ControllerBlock.CenterOfMass)), 1 / (ship.Planets.CurrentPlanet.GravityExponent - 1));
                                double targetvelocity = Math.Pow((exitenergy - curenergy)/2, .5);
                                if (double.IsNaN(targetvelocity) || targetvelocity <= ship.ShipData.Velocity.LinearVelocity.Length())
                                {
                                    stage++;
                                }
                            }
                            else
                            {
                                var targthrust = -Vector3D.Transform(Vector3D.Reject(ship.ShipData.Velocity.LinearVelocity, Vector3D.Normalize(grav)),MatrixD.Transpose(ship.ControllerBlock.WorldMatrix.GetOrientation()))*ship.ShipData.Mass.PhysicalMass;
                                ship.Propulsion.thrust(targthrust);
                            }
                        }
                        else
                        {
                            StopAscent();
                        }
                    }
                }
            }
            public Ascent(Ship ship) : base(ship)
            {
                Hydros = new List<IMyThrust>();
            }
            public void StartAscent(float escapespeed = 0)
            {
                if (IsReady)
                {
                    this.escapespeed = escapespeed;
                    HydrosOnYet = false;
                    Ascending = true;
                    stage = 0;
                    Hydros.ForEach((x) => x.Enabled = false);
                    ship.Navigation.SetEnabled(true);
                    ship.Rotation.SetEnabled(false);
                }
            }
            void GetHydroList()
            {

                ship.GridTerminalSystem.GetBlocksOfType<IMyThrust>(Hydros);
                Hydros = Hydros.Where((x) => ship.Propulsion.GetThrusterType(x) == Propulsion.ThrusterType.Hydrogen).ToList();
            }
            public void StopAscent()
            {
                if (IsReady)
                {
                    HydrosOnYet = false;
                    Ascending = false;
                    Hydros.ForEach((x) => x.Enabled = true);
                    ship.Navigation.SetEnabled(false);
                }
            }
        }
    }
}

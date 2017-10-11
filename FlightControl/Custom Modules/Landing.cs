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
        public class Landing : Module
        {
            float targetelevation;
            bool IsLanding;
            public override void Tick()
            {
                
                if (IsReady)
                {
                    if (IsLanding)
                    {
                        if (ship.Planets.CurrentPlanet != null)
                        {
                            ship.Navigation.OrientVectorTowardsDirection(Vector3D.Down, Vector3D.Normalize(ship.ShipData.NaturalGravity));
                            if (ship.Planets.CurrentPlanet.HasAtmo)
                            {

                            }
                            else
                            {
                                   
                            }
                        }
                        else
                        {
                            StopLanding();
                        }
                    }
                }
            }
            public Landing(Ship ship) : base(ship)
            {
                Initialized = true;
            }
            public void StartLanding(float elevation=-1)
            {
                if (IsReady) { 
                    IsLanding = true;
                    targetelevation = elevation;
                    ship.Navigation.SetEnabled(true);
                    List<IMyThrust> blocks = new List<IMyThrust>();
                    ship.GridTerminalSystem.GetBlocksOfType<IMyThrust>(blocks);
                    blocks.ForEach((x) => x.Enabled = true);
                }
            }
            public void StopLanding()
            {
                IsLanding = false;
                ship.Navigation.SetEnabled(false);
            }
        }
    }
}

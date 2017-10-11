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
        public class ShipData 
        {
            public Ship ship { get; private set; }
            public MyShipMass Mass { get; private set; }
            public Vector3D NaturalGravity { get; private set; }
            public MyShipVelocities Velocity { get; private set; }
            
            public ShipData(Ship ship)
            {
                this.ship = ship;
            }
            public void Update()
            {
                Mass = ship.ControllerBlock.CalculateShipMass();
                NaturalGravity = ship.ControllerBlock.GetNaturalGravity();
                Velocity = ship.ControllerBlock.GetShipVelocities();
            }
        }
    }
}

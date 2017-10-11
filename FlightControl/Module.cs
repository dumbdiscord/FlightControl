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
        public abstract class Module
        {
            protected Ship ship { get; }
            public virtual bool Initialized { get; protected set; }
            public virtual bool Enabled { get; private set; } = true;
            public virtual bool HasSaveData { get; private set; } = false;
            public virtual void SetEnabled(bool active)
            {
                Enabled = active;
            }
            public virtual bool IsReady
            {
                get
                {
                    return Initialized && Enabled;
                }
            }
            public virtual void ProcessCommand(string command) { }
            public virtual bool CanInitialize { get { return !Initialized; } }
            public Module(Ship ship)
            {
                this.ship = ship;
            }
            public virtual void SaveState(BinarySerializer buf)
            {

            }
            public virtual void LoadState(BinarySerializer buf)
            {

            }
            public virtual void Tick()
            {

            }
        }
    }
}

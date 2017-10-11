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
        public class Ship
        {
            public static float speedLimit = 199f;
            public IMyGridTerminalSystem GridTerminalSystem;
            public IMyShipController ControllerBlock { get; private set; }
            public MyGridProgram Me { get; private set; }
            public Logger Logger { get; private set; }
            public Navigation Navigation { get; private set; }
            public Propulsion Propulsion { get; private set; }
            public Rotation Rotation { get; private set; }
            public SaveState SaveState { get; private set; }
            public CommManager Communications { get; private set; }
            public Planets Planets { get; set; }
            public EventScheduler EventScheduler { get; private set; }
            public List<Module> CustomModules { get; private set; }
            public ShipData ShipData { get; private set; }
            public bool Initialized { get; private set; }
            public bool DebugMode = true;
            public void Initialize()
            {
                List<Module> AllModules = new List<Module>() { Logger, Propulsion, Rotation, Navigation, Communications,Planets };
                AllModules.AddRange(CustomModules);
                SaveState.Initialize(AllModules);
                Logger.Initialize(null);
                
                Propulsion.Initialize();
                Rotation.Initialize();
                Navigation.Initialize();
                Communications.Initialize(null);

                Logger.Log("Initialization Complete!");
                Initialized = true;
            }
            public Ship(IMyGridTerminalSystem gts, IMyShipController ControllerBlock, MyGridProgram Me)
            {
                if (ControllerBlock == null)
                {
                    throw new ArgumentNullException("ControllerBlock");
                }

                GridTerminalSystem = gts;
                this.ControllerBlock = ControllerBlock;
                this.Me = Me;
                ShipData = new ShipData(this);
                Logger = new Logger(this);
                Propulsion = new Propulsion(this);
                Rotation = new Rotation(this);
                Planets = new Planets(this);
                Navigation = new Navigation(this);
                Communications = new CommManager(this);
                SaveState = new SaveState(this);
                EventScheduler = new EventScheduler();
                CustomModules = new List<Module>();
            }

            public void Tick(string args, Action TickAction = null)
            {

                if (args != "" && args != " ")
                {
                    CustomModules.ForEach(x => x.ProcessCommand(args));
                    Communications.ProcessRawInputString(args);
                }
                else
                {
                    ShipData.Update();
                    EventScheduler.Tick();
                    Communications.TickStart();
                    
                    switch (args)
                    {
                        default:

                            break;
                    }
                    Planets.Tick();
                    CustomModules.ForEach(x => x.Tick());
                    if (TickAction != null) TickAction();
                    Communications.TickEnd();
                }
            }
        }
    }
}

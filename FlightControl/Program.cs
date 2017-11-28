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
    partial class Program : MyGridProgram
    {
        #region script
        public string timername = "Timer";
        public string controllerblockname = "Cockpit";
        public string commantenna = "CommAntenna";
        Ascent h;
        bool gospaceplat = false;
        bool gosilver = false;
        Vector3D spaceplat = new Vector3D(35980.44, -9401.37, -4125.48);
        Vector3D silver = new Vector3D(43437.61, -7122.02, -6404.50);   
        //public string dockingconnectorname = "Connector";
        public Ship ship;
        public void Tick()
        {
            
        }
        public void Init()
        {
            ship = new Ship(GridTerminalSystem, GridTerminalSystem.GetBlockWithName(controllerblockname) as IMyShipController, this);
            var comms = GridTerminalSystem.GetBlockWithName(commantenna) as IMyRadioAntenna;
            if (comms != null)
            {
                ship.Communications.Initialize(comms);
            }
            h = new Ascent(ship);
            h.Initialize();
            // h.Initialize(GridTerminalSystem.GetBlockWithName(dockingconnectorname) as IMyShipConnector);
            ship.CustomModules.Add(h);
            ship.Initialize();
        }
        void Main(string args,UpdateType type)
        {
            switch (args)
            {
                case "Ascend":
                    h.SetEnabled(true);
                    h.StartAscent();

                    break;
                case "Stop":
                    ship.Navigation.SetEnabled(false);
                    gospaceplat = false;
                    h.StopAscent();
                    break;
                case "GoSpaceBase":
                    gospaceplat = !gospaceplat;
                    break;
                case "GoSilver":
                    gosilver = !gosilver;
                    break;
                default:
                    if (gospaceplat)
                    {
                        h.SetEnabled(false);
                        ship.Navigation.SetEnabled(true);
                        ship.Rotation.SetEnabled(false);
                        ship.Navigation.TranslateToVector(spaceplat);
                    }
                    if (gosilver)
                    {
                        h.SetEnabled(false);
                        ship.Navigation.SetEnabled(true);
                        ship.Rotation.SetEnabled(false);
                        ship.Navigation.TranslateToVector(silver);
                    }
                    break;
            }
            ship.Tick(args, type,Tick);
            ProfilerGraph();

            
        }
        //Whip's Profiler Graph Code
        int count = 1;
        int maxSeconds = 15;
        StringBuilder profile = new StringBuilder();
        bool hasWritten = false;
        void ProfilerGraph()
        {
            if (count <= maxSeconds * 60)
            {
                double timeToRunCode = Runtime.LastRunTimeMs;

                profile.Append(timeToRunCode.ToString()).Append("\n");
                count++;
            }
            else if (!hasWritten)
            {
                var screen = GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextPanel;
                screen?.WritePublicText(profile.ToString());
                screen?.ShowPublicTextOnScreen();
                if (screen != null)
                    hasWritten = true;
            }
        }
        private IMyTimerBlock timer;
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            Init();
        }
        
        
       
        
        
        
       

        #endregion
    }
}
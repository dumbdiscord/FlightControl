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
        public class Logger : Module
        {
            Action<string> logfunction;
            Action clearlog;
            public Logger(Ship ship) : base(ship) { }
            public IMyTerminalBlock LogBlock { get; private set; }
            public override bool IsReady
            {
                get
                {
                    return Initialized && Enabled;
                }
            }
            public void Initialize(IMyTerminalBlock block)
            {
                logfunction = ship.Me.Echo;
                clearlog = () => { };
                LogBlock = block;
                if (block != null)
                {
                    if (block is IMyTextPanel)
                    {
                        logfunction = (x) => (LogBlock as IMyTextPanel).WritePublicText(x + '\n', true);
                        clearlog = () => (LogBlock as IMyTextPanel).WritePublicText("");
                    }
                    else
                    {
                        logfunction = (x) => LogBlock.CustomData += x + '\n';
                        clearlog = () => LogBlock.CustomData = "";
                    }
                }
                clearlog();
                Initialized = true;
                Log("Logging Module Initialized!");
            }
            public void Debug(object value)
            {
                if (IsReady && ship.DebugMode)
                {
                    Log(value);
                }
            }
            public void Log(object value)
            {
                if (IsReady)
                {
                    logfunction(value.ToString());
                }
            }
            public void Clear()
            {
                if (IsReady)
                {
                    clearlog();
                }
            }
            public void LogException(Exception e)
            {
                if (IsReady)
                {
                    Log($"Encountered Exception {e}!\nAttempting Normal Execution...");

                }
            }

        }
    }
}

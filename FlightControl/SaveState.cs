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
        public class SaveState : Module
        {
            Dictionary<Module, byte[]> ModuleData;
            public void Initialize(List<Module> SaveModules)
            {
                if (CanInitialize)
                {
                    using (var buf = new BinarySerializer(Convert.FromBase64String(ship.Me.Storage)))
                    {
                        while (buf.Position < buf.buffer.Length)
                        {
                            string modulename = buf.ReadString();
                            var module = SaveModules.FirstOrDefault(x => x.GetType().Name == modulename);
                            long temp = buf.Position;
                            if (module != null)
                            {
                                module.LoadState(buf);
                                long temp2 = buf.Position;
                                buf.Position = temp;
                                ModuleData.Add(module, buf.Read((int)(temp2 - temp)));
                                buf.Position = temp2;
                                SaveModules.Remove(module);
                            }
                        }

                    }
                    foreach (var module in SaveModules)
                    {
                        if (module.HasSaveData)
                        {
                            using (var buf = new BinarySerializer(new byte[0]))
                            {
                                module.SaveState(buf);
                                ModuleData.Add(module, buf.buffer);
                            }
                        }
                    }
                }
                SaveData();
            }
            public void SaveData()
            {
                using (var buf = new BinarySerializer(new byte[0]))
                {
                    foreach (var module in ModuleData)
                    {
                        buf.WriteString(module.Key.GetType().Name);
                        buf.Write(module.Value);
                    }
                    (ship.Me as Program).Storage = Convert.ToBase64String(buf.buffer);
                }
            }
            public SaveState(Ship ship) : base(ship)
            {
                ModuleData = new Dictionary<Module, byte[]>();
            }

        }
    }
}

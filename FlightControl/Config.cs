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
        public class Config : Module
        {
            public Dictionary<string, ConfigNode> ConfigNodes { get; private set; }
            IMyTerminalBlock ConfigBlock;
            public override bool IsReady
            {
                get
                {
                    return base.IsReady && ConfigBlock != null;
                }
            }
            public Config(Ship ship) : base(ship)
            {
                ConfigNodes = new Dictionary<string, ConfigNode>();
            }
            String[] QuoteIgnoreSplit(string source, char splitchar)
            {
                List<string> output = new List<string>();
                var chars = source.ToCharArray();
                bool quoted = false;
                int lastsplitindex = 0;
                for (int i = 0; i < chars.Length; i++)
                {
                    var c = chars[i];
                    if (c == '"')
                    {
                        quoted = !quoted;
                    }
                    else if (c == '\n')
                    {
                        if (!quoted)
                        {
                            if (i != 0)
                            {
                                output.Add(source.Substring(lastsplitindex, i - lastsplitindex));
                            }
                            lastsplitindex = i + 1;
                        }
                    }

                }
                if (lastsplitindex < source.Length)
                {
                    output.Add(source.Substring(lastsplitindex, source.Length - lastsplitindex));
                }
                return output.ToArray();
            }
            public void SaveConfig()
            {
                if (IsReady)
                {
                    string output = "";

                }
            }
            public void LoadConfig()
            {
                if (IsReady)
                {
                    string data = ConfigBlock.CustomData;
                    var configs = QuoteIgnoreSplit(data, '\n');
                    foreach (string node in configs)
                    {
                        var nodedata = node.Split('|');
                        Type type;
                        object value;
                        switch (nodedata[0].ToLower())
                        {
                            case "string":
                                type = typeof(string);
                                value = node.Substring(5 + nodedata[1].Length);
                                break;
                            case "vector3d":
                                type = typeof(Vector3D);
                                Vector3D vector;
                                Vector3D.TryParse(nodedata[2], out vector);
                                value = vector;
                                break;
                            case "double":
                                type = typeof(double);
                                double doublevalue;
                                double.TryParse(nodedata[2], out doublevalue);
                                value = doublevalue;
                                break;
                            case "int":
                                type = typeof(int);
                                int intvalue;
                                int.TryParse(nodedata[2], out intvalue);
                                value = intvalue;
                                break;
                            default:
                                continue;
                        }
                        ConfigNodes.Add(nodedata[1], new ConfigNode(value, type));
                    }
                }
            }

            public struct ConfigNode
            {
                public Type Type;
                public Object Value;
                public ConfigNode(Object Value, Type type)
                {
                    this.Value = Value;
                    this.Type = type;
                }
            }
        }
    }
}

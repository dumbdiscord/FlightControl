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
        public static class MathUtils
        {
            public static MatrixD OuterProduct(Vector3D a, Vector3D b)
            {
                return new MatrixD(a.X * b.X, a.X * b.Y, a.X * b.Z, a.Y * b.X, a.Y * b.Y, a.Y * b.Z, a.Z * b.X, a.Z * b.Y, a.Z * b.Z);
            }
        }
    }
}

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
        public class Planets : Module
        {
            List<Planet> SavedPlanets;
            public Planet CurrentPlanet { get; set; }
            public override bool HasSaveData
            {
                get
                {
                    return true;
                }
            }
            public Planets(Ship ship):base(ship)
            {
                
                SavedPlanets = new List<Planet>();
            }
            public override void LoadState(BinarySerializer buf)
            {
                SavedPlanets.Clear();
                int length = buf.ReadInt();
                for(int i = 0; i < length; i++)
                {
                    SavedPlanets.Add(Planet.Read(buf));
                }
            }
            public override void SaveState(BinarySerializer buf)
            {
                buf.WriteInt(SavedPlanets.Count);
                foreach(var p in SavedPlanets)
                {
                    p.Write(buf);
                }
            }
            public override void Tick()
            {
                if (CurrentPlanet == null)
                {
                    if (!double.IsNaN(ship.ControllerBlock.GetNaturalGravity().Length()))
                    {
                        Vector3D planetcenter;
                        ship.ControllerBlock.TryGetPlanetPosition(out planetcenter);
                        Planet p = SavedPlanets.FirstOrDefault((x) => x.Center == planetcenter);
                        if (p != null)
                        {
                            CurrentPlanet = p;
                        }
                        else
                        {

                        }
                        {
                            p = Planet.GetBestGuessFromInitialData(ship.ControllerBlock);
                            SavedPlanets.Add(p);
                            CurrentPlanet = p;
                            ship.SaveState.SaveData();
                        }
                    }
                }
                else
                {
                    if (double.IsNaN(ship.ControllerBlock.GetNaturalGravity().Length()))
                    {
                        CurrentPlanet = null;
                    }
                }
            }
        }
        
        public class Planet
        {
            public Vector3D Center { get; set; }
            public double Radius { get; set; }
            public double HillParameter { get; set; } = .12f;

            public double SurfaceGravity { get; set; }
            public int GravityExponent { get; set; } = 7;
            public static int DefaultGravityExponent { get; } = 7;
            public bool HasAtmo { get; private set; }
            public double GravityMathNumber { get; private set; }

            public AtmosphericInfo AtmoInfo { get; private set; }
            public Planet() { }
            public static Planet Read(BinarySerializer buf)
            {
                Planet p = new Planet();
                p.Center = buf.ReadVector3D();
                p.Radius = buf.ReadDouble();
                p.HillParameter = buf.ReadDouble();
                p.SurfaceGravity = buf.ReadDouble();
                p.GravityMathNumber = buf.ReadDouble();
                p.HasAtmo = buf.ReadBool();
                if (p.HasAtmo)
                {
                    p.AtmoInfo = AtmosphericInfo.Read(buf);
                }
                return p;
            }
            public void Write(BinarySerializer buf)
            {
                buf.WriteVector3D(Center);
                buf.WriteDouble(Radius);
                buf.WriteDouble(HillParameter);
                buf.WriteDouble(SurfaceGravity);
                buf.WriteDouble(GravityMathNumber);
                buf.WriteBool(HasAtmo);
                if (HasAtmo)
                {
                    AtmoInfo.Write(buf);
                }
            }
            public double GetGravityEdge()
            {
                return Radius * (1 + HillParameter) * Math.Pow((.05), -1 / GravityExponent);
            }
            public double GetInverseExponentGravity(double dist)
            {
                return GravityMathNumber / Math.Pow(dist, GravityExponent);   
            }
            public double GetActualGravity(double dist)
            {
                if (dist > Radius * (1 + HillParameter))
                {
                    if (dist > GetGravityEdge())
                    {
                        return 0;
                    }
                    return GetInverseExponentGravity(dist);
                }
                if (dist > Radius)
                {
                    return SurfaceGravity;
                }
                return dist / Radius * SurfaceGravity;
            }
            public double GetGravitationalPotential(double dist)
            {
                return -GravityMathNumber / ((GravityExponent - 1) * Math.Pow(dist, GravityExponent - 1));
            }
            public double GetAtmosphericEfficiency(double elevation)
            {
                return MathHelperD.Clamp(AtmoInfo.SeaLevelPressure*(1 - (elevation) / (Radius * HillParameter * AtmoInfo.LimitAltitude)), 0, AtmoInfo.SeaLevelPressure);
            }
            public double GetIonEfficiency(double elevation)
            {
                return MathHelperD.Clamp( (.3* (elevation) / (Radius * HillParameter * AtmoInfo.LimitAltitude)), .3, 1);
            }
            public static Planet GetBestGuessFromInitialData(IMyShipController remote)
            {
                if (!double.IsNaN(remote.GetNaturalGravity().Length()))
                {
                    Planet p = new Planet();
                    Vector3D center;
                    remote.TryGetPlanetPosition(out center);
                    p.Center = center;
                    double curdist = Vector3D.Distance(center, remote.GetPosition());
                    double sealevelelevation;
                    double curgrav = remote.GetNaturalGravity().Length();
                    remote.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out sealevelelevation);
                    p.Radius = curdist - sealevelelevation;
                    p.GravityMathNumber = curgrav * Math.Pow(curdist, p.GravityExponent);
                    if (curdist> p.Radius * (1 + p.HillParameter)) { 
                        double mathvalue = curgrav * Math.Pow(curdist, p.GravityExponent);
                        p.SurfaceGravity = mathvalue / Math.Pow(p.Radius * p.HillParameter + p.Radius, p.GravityExponent);
                    }
                    else
                    {
                        p.SurfaceGravity = curgrav;
                    }
                    return p;
                }
                return null;
            }
        }
        public struct AtmosphericInfo
        {
            public float SeaLevelPressure { get; }
            public bool OxygenPresent { get; }
            public float LimitAltitude { get; }
            public AtmosphericInfo(float SeaLevelPressure, bool Oxygen, float LimitAltitude = 2)
            {
                this.SeaLevelPressure = SeaLevelPressure;
                OxygenPresent = Oxygen;
                this.LimitAltitude = LimitAltitude;
            }
            public static AtmosphericInfo Read(BinarySerializer buf)
            {
                return new AtmosphericInfo(buf.ReadFloat(), buf.ReadBool(),buf.ReadFloat());
            }
            public void Write(BinarySerializer buf)
            {
                buf.WriteFloat(SeaLevelPressure);
                buf.WriteBool(OxygenPresent);
                buf.WriteFloat(LimitAltitude);
            }
        }
    }
}

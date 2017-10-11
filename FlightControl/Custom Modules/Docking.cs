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
        // Example Custom Module
        public class Docking : Module
        {
            public IMyShipConnector Connector { get; private set; }
            MatrixD TargetMatrix;
            Client TargetClient;
            int dockingmode = 0;
            float finalapproachdistance=10;
            float maxapproachspeed = 1;
            Vector3D curtargpos;
            MatrixD offset;
            public override bool IsReady
            {
                get
                {
                    return base.IsReady&&ship.Navigation.IsReady;
                }
            }
            public void Initialize(IMyShipConnector connector)
            {
                if (CanInitialize)
                {
                    ship.Communications.RegisterCustomMessageHandler("DOCKINGINFOREQUEST", (x, c) => {
                        using (var buf = new BinarySerializer(new byte[0]))
                        {
                            buf.WriteMatrixD(connector.WorldMatrix);
                            ship.Communications.SendCustomMessage("DOCKINGINFORESPONSE", buf.buffer, c);
                       
                        }
                    });
                    ship.Communications.RegisterCustomMessageHandler("DOCKINGINFORESPONSE", (x, c) =>
                    {
                        using (var buf = new BinarySerializer(x))
                        {
                            if (TargetClient != null)
                            {
                                if (c == TargetClient)
                                {
                                    TargetMatrix = buf.ReadMatrixD();
                                }
                            }
                        }
                    });

                    Initialized = true;
                }
            }
            public override void Tick()
            {
                if (IsReady) {
                    if (TargetClient != null && TargetMatrix != MatrixD.Zero)
                    {
                        offset = Connector.WorldMatrix.GetOrientation() * MatrixD.Transpose(ship.ControllerBlock.WorldMatrix.GetOrientation());
                        offset.Right *= -1;
                        offset.Forward *= -1;
                        ship.Navigation.OrientMatrixTowardsMatrix(TargetMatrix.GetOrientation(), offset);
                        switch (dockingmode)
                        {
                            case 1:
                                curtargpos = TargetMatrix.Forward * finalapproachdistance + TargetMatrix.Translation + ship.ControllerBlock.GetPosition() - Connector.GetPosition();


                                if (ship.Navigation.TranslateToVector(curtargpos,1,100)) {
                                    dockingmode = 2;
                                }
                                break;
                            case 2:
                                curtargpos = TargetMatrix.Forward * .25 + TargetMatrix.Translation + ship.ControllerBlock.GetPosition() - Connector.GetPosition();
                                if (ship.Navigation.TranslateToVector(curtargpos,1,100,maxapproachspeed))
                                {
                                    dockingmode = 3;
                                }
                                break;
                            case 3:
                                Connector.Connect();
                                if (Connector.Status == MyShipConnectorStatus.Connected)
                                {
                                    StopDocking();
                                }
                                break;
                        }

                    }
                }
            }
            public Docking(Ship ship) : base(ship)
            {

            }
            public void StartDocking(Client Target)
            {
                if (IsReady)
                {
                    dockingmode = 1;
                    TargetClient = Target;
                    ship.Communications.SendCustomMessage("DOCKINGINFOREQUEST", new byte[0], TargetClient);
                }
            }
            public void StopDocking()
            {
                dockingmode = 0;
                TargetClient = null;
                ship.Navigation.SetEnabled(false);
                TargetMatrix = MatrixD.Zero;
            }
        }
    }
}

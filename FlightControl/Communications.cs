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
        // fakuctagroils comm system converted into module form
        // TODO: rewrite this to look good, the concept is sound but the code is sloppy.
        public class CommManager : Module
        {
            int netid;
            public new Ship ship;
            public string Base64NetID { get; private set; } = "AAAAAA==";
            public int NetworkID
            {
                get
                {
                    return netid;
                }
                set
                {
                    Base64NetID = Convert.ToBase64String(BitConverter.GetBytes(value));
                    netid = value;
                }
            }
            public List<Client> ActiveClients { get; private set; }
            public Client myClient { get; private set; }
            MessageHandler messageHandler;
            public IMyRadioAntenna CommsAntenna { get; private set; }
            public void Initialize(IMyRadioAntenna antenna)
            {
                if (CanInitialize)
                {

                    CommsAntenna = antenna;
                    myClient = Client.GenerateMyClient(this);
                    if (CommsAntenna != null)
                    {

                        ship.Logger.Log("Communications Module Initialized!");
                        Initialized = true;
                    }
                    else
                    {
                        ship.Logger.Log("Could not initialize the Communications Module, no valid antenna found!");
                    }
                }
            }

            public CommManager(Ship ship) : base(ship)
            {
                this.ship = ship;
                ActiveClients = new List<Client>();
                messageHandler = new MessageHandler(this);
            }

            public void TickStart()
            {
                if (IsReady)
                {
                    if (messageHandler.IncomingMessages.Count > 0)
                        messageHandler.ProcessIncomingQueue();
                }
            }
            public void TickEnd()
            {
                if (IsReady)
                {
                    if (messageHandler.OutgoingMessages.Count > 0)
                        messageHandler.ProcessOutgoingQueue();
                }
            }
            public void SendCustomMessage(string name, byte[] data, Client Receiver)
            {
                messageHandler.OutgoingMessages.Add(messageHandler.customMessageHandler.MakeCustomMessage(name, data, Receiver));
            }
            public void RegisterCustomMessageHandler(string name, Action<byte[], Client> OnMessageReceived = null, bool shouldstoredata = false, int datastorelimit = 1)
            {
                messageHandler.customMessageHandler.RegisterCustomMessage(name, OnMessageReceived, shouldstoredata, datastorelimit);
            }
            public void GetStoredCustomMessageData(string name, byte[] buffer)
            {
                messageHandler.customMessageHandler.GetMessageData(name, buffer);
            }
            public void ProcessRawInputString(string str)
            {
                messageHandler.ProcessRawInputString(str);
            }
            public void Reset()
            {
                ActiveClients.Clear();
            }
            public bool IsMyId(long val)
            {
                return val == myClient.ID || val == myClient.GridID;
            }
            public bool ContainsMyId(string val)
            {
                return val.Contains(myClient.GridID.ToString()) | val.Contains(myClient.ID.ToString());
            }
            public bool ContainsMyIdOrGlobal(string val)
            {
                return val.Contains(myClient.GridID.ToString()) | val.Contains(myClient.ID.ToString()) | val.Contains("GLOBAL");
            }
            public bool ContainsMyIdOrGlobalBase64(string val)
            {
                return val.Contains(myClient.GridIDBase64) | val.Contains(myClient.IDBase64) | val.Contains("GLOBAL");
            }
            public void Disconnect()
            {
                messageHandler.SendDisconnectMessage();
                Reset();
            }
            public bool TryGetClientFromID(long id, out Client client)
            {

                var c = (ActiveClients.FirstOrDefault(x => x.GridID == id | x.ID == id));
                if (c != null)
                {
                    client = c;
                    return true;
                }
                else
                {
                    client = null;
                    return false;
                }
            }
            public bool TryGetClientFromName(string id, out Client client)
            {

                var c = (ActiveClients.FirstOrDefault(x => x.Name == id));
                if (c != null)
                {
                    client = c;
                    return true;
                }
                else
                {
                    client = null;
                    return false;
                }
            }
            public void ConnectToNetwork(int networkID)
            {
                if (IsReady)
                {
                    Disconnect();
                    this.NetworkID = networkID;

                    messageHandler.SendConnectMessage();
                }
            }
            public bool UpdateUser(Client client)
            {

                var c = ActiveClients.FirstOrDefault(x => x.GridID == client.GridID | x.ID == client.ID);
                if (c != default(Client))
                {
                    ActiveClients[ActiveClients.IndexOf(c)] = client;

                }
                else
                {
                    ActiveClients.Add(client);
                }
                return true;
            }
            public void RemoveUser(Client client)
            {
                var c = ActiveClients.FirstOrDefault(x => x.GridID == client.GridID | x.ID == client.ID);
                if (c != default(Client))
                {

                    ActiveClients.Remove(c);

                }

            }
            public void TransmitString(string s)
            {
                if (IsReady)
                    CommsAntenna.TransmitMessage(s);
            }
        }
        public class MessageHandler
        {
            public List<RawMessage> IncomingMessages { get; private set; }
            public List<RawMessage> OutgoingMessages { get; private set; }
            public CustomMessageHandler customMessageHandler { get; private set; }
            public CommManager Comms { get; private set; }

            public MessageHandler(CommManager comms)
            {
                Comms = comms;
                IncomingMessages = new List<RawMessage>();
                OutgoingMessages = new List<RawMessage>();
                customMessageHandler = new CustomMessageHandler();
            }
            #region Message Processing
            public void ProcessRawInputString(string input)
            {
                if (Comms.ContainsMyIdOrGlobalBase64(input))
                {
                    if (Comms.NetworkID != -1)
                    {
                        if (!input.StartsWith(Comms.Base64NetID))
                        {
                            return;
                        }
                    }
                    else
                    {
                        int netid;
                        netid = BitConverter.ToInt32(Convert.FromBase64String((input.Substring(0, 8))), 0);
                        Comms.NetworkID = netid;
                    }

                    long senderid;
                    var split = input.Split('?');
                    senderid = BitConverter.ToInt64(Convert.FromBase64String(split[0].Substring(8, 12)), 0);
                    ProcessIncomingPacketBody(split[1], senderid);
                }
            }

            public void ProcessIncomingPacketBody(string messages, long senderId)
            {
                var messagelist = messages.Split('\n');
                foreach (string s in messagelist)
                {
                    if (s.StartsWith(Comms.myClient.GridIDBase64) | s.StartsWith(Comms.myClient.IDBase64))
                    {
                        ProcessMessageString(s.Substring(12), senderId, false);
                    }
                    else if (s.StartsWith("GLOBAL"))
                    {
                        ProcessMessageString(s.Substring(6), senderId, true);
                    }
                }

            }
            public void ProcessMessageString(string message, long senderId, bool isglobal)
            {
                RawMessage rawMessage = new RawMessage();
                rawMessage.isGlobal = isglobal;
                rawMessage.SenderReceiver = senderId;

                rawMessage.Type = (MessageType)BitConverter.ToInt32(Convert.FromBase64String(message.Substring(0, 8)), 0);
                rawMessage.Args = Convert.FromBase64String(message.Substring(8));
                IncomingMessages.Add(rawMessage);
            }
            public void ProcessIncomingQueue()
            {
                foreach (RawMessage m in IncomingMessages)
                {
                    ProcessIncomingMessage(m);
                }
                IncomingMessages.Clear();
            }
            public string FormatMessageToString(RawMessage m)
            {

                return ((m.isGlobal ? "GLOBAL" : (m.SenderReceiverBase64)) + (m.TypeBase64) + Convert.ToBase64String(m.Args));
            }
            public void SendHeartbeatToAll()
            {
                RawMessage m = new RawMessage(Comms.myClient.ID, true, MessageType.HEARTBEAT, new byte[0]);
                OutgoingMessages.Add(m);
            }
            public void ProcessOutgoingQueue()
            {
                StringBuilder s = new StringBuilder();
                s.Append(Comms.Base64NetID + Comms.myClient.IDBase64 + '?');
                foreach (RawMessage m in OutgoingMessages)
                {
                    s.Append(FormatMessageToString(m) + '\n');
                }
                Comms.TransmitString(s.ToString());
                OutgoingMessages.Clear();
            }
            public void ProcessIncomingMessage(RawMessage m)
            {
                try
                {
                    switch (m.Type)
                    {
                        case MessageType.HEARTBEAT:
                            Comms.ship.Logger.Log("Heartbeat received from " + m.SenderReceiver);
                            break;
                        case MessageType.CONNECT:
                            ProcessConnectMessage(m);
                            break;
                        case MessageType.UPDATESTATUS:
                            ProcessClientStatusUpdate(m);
                            break;
                        case MessageType.DISCONNECT:
                            ProcessDisconnectMessage(m);
                            break;
                        case MessageType.CUSTOMMESSAGE:
                            ProcessCustomMessage(m);
                            break;
                        case MessageType.SENDMETADATA:
                            ProcessIncomingMetaData(m);
                            break;
                        default:
                            Comms.ship.Logger.Log("Malformed Message Received, Ignoring...");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Comms.ship.Logger.LogException(e);
                }
            }
            #endregion

            public void ProcessIncomingMetaData(RawMessage m)
            {
                using (var buf = new BinarySerializer(m.Args))
                {
                    var data = Client.ClientMetaData.Deserialize(buf);
                    Client client;
                    if (Comms.TryGetClientFromID(m.SenderReceiver, out client))
                    {
                        client.MetaData = data;
                    }
                }
            }
            public void ProcessCustomMessage(RawMessage m)
            {
                Client client;
                if (m.isGlobal)
                {
                    client = null;
                }
                else
                {
                    Comms.TryGetClientFromID(m.SenderReceiver, out client);
                }
                customMessageHandler.ProcessIncomingCustomMessage(m.Args, client);
            }
            public void SendMyMetaData(Client receiver)
            {
                RawMessage m = new RawMessage();
                if (receiver == null)
                {
                    m.isGlobal = true;
                }
                else
                {
                    m.SenderReceiver = receiver.AvailableID;
                }
                m.Type = MessageType.SENDMETADATA;
            }
            public void SendDisconnectMessage()
            {
                OutgoingMessages.Add(new RawMessage(0, true, MessageType.DISCONNECT, new byte[0]));
            }
            public void ProcessDisconnectMessage(RawMessage m)
            {
                Client client;
                if (Comms.TryGetClientFromID(m.SenderReceiver, out client))
                {
                    Comms.RemoveUser(client);
                }
            }
            public void ProcessClientStatusUpdate(RawMessage m)
            {
                using (BinarySerializer buf = new BinarySerializer(m.Args))
                {
                    Client newclient = Client.GetFromSerializer(buf, Comms);
                    Comms.UpdateUser(newclient);
                }
            }

            public void ProcessConnectMessage(RawMessage m)
            {
                using (BinarySerializer buf = new BinarySerializer(m.Args))
                {
                    Client newclient = Client.GetFromSerializer(buf, Comms);
                    if (Comms.UpdateUser(newclient))
                    {
                        SendClientStatusUpdate(newclient);
                    }
                }

            }
            public void SendClientStatusUpdate(Client receiver)
            {
                RawMessage m = new RawMessage(receiver.AvailableID, false, MessageType.UPDATESTATUS, Comms.myClient.SerializeClientData());
                OutgoingMessages.Add(m);
            }
            public void SendConnectMessage()
            {
                RawMessage m = new RawMessage(0, true, MessageType.CONNECT, Comms.myClient.SerializeClientData());
                OutgoingMessages.Add(m);
            }
            public class CustomMessageHandler
            {
                public List<CustomMessageData> CustomMessages { get; }
                public CustomMessageHandler()
                {
                    CustomMessages = new List<CustomMessageData>();
                }
                public void RegisterCustomMessage(string name, Action<byte[], Client> OnReceive = null, bool shouldstoredata = false, int Queuelimit = 1)
                {
                    if (!CustomMessages.Any(x => x.Name == name))
                    {
                        CustomMessages.Add(new CustomMessageData(name, OnReceive, shouldstoredata, Queuelimit));
                    }
                }
                public void ProcessIncomingCustomMessage(byte[] bytes, Client client)
                {
                    using (var buf = new BinarySerializer(bytes))
                    {
                        string str = buf.ReadString();
                        var data = CustomMessages.FirstOrDefault(x => x.Name == str);
                        if (data != null)
                        {
                            var ExtraData = buf.ReadToEnd();
                            if (data.OnMessageReceived != null) data.OnMessageReceived(ExtraData, client);
                            if (data.ShouldStoreData) data.StoredData.Enqueue(ExtraData);
                        }
                    }
                }
                public RawMessage MakeCustomMessage(string name, byte[] Data, Client receiver)
                {
                    RawMessage m = new RawMessage();
                    if (receiver == null)
                    {
                        m.isGlobal = true;

                    }
                    else
                    {
                        m.SenderReceiver = receiver.AvailableID;
                    }
                    using (var h = new BinarySerializer(new byte[0]))
                    {
                        h.WriteString(name);
                        h.Write(Data);
                        m.Args = h.buffer;
                    }
                    m.Type = MessageType.CUSTOMMESSAGE;
                    return m;
                }
                public void GetMessageData(string name, byte[] buffer)
                {
                    var data = CustomMessages.FirstOrDefault(x => x.Name == name);
                    if (data != null)
                    {
                        if (data.ShouldStoreData)
                        {
                            buffer = data.StoredData.Dequeue();
                        }
                    }
                }
                public class CustomMessageData
                {

                    public string Name { get; private set; }
                    public Action<byte[], Client> OnMessageReceived { get; private set; }
                    public LimitedQueue<byte[]> StoredData { get; private set; }
                    public bool ShouldStoreData { get; private set; }
                    public CustomMessageData(string Name, Action<byte[], Client> OnMessageReceived, bool ShouldStoreData, int QueueLimit = 1)
                    {
                        this.ShouldStoreData = ShouldStoreData;
                        this.Name = Name;
                        this.OnMessageReceived = OnMessageReceived;
                        if (ShouldStoreData)
                        {
                            StoredData = new LimitedQueue<byte[]>(QueueLimit);
                        }
                        else
                        {
                            StoredData = null;
                        }
                    }
                }
            }

        }
        public class LimitedQueue<T> : Queue<T>
        {
            public int Limit { get; set; }

            public LimitedQueue(int limit) : base(limit)
            {
                Limit = limit;
            }

            public new void Enqueue(T item)
            {
                while (Count >= Limit)
                {
                    Dequeue();
                }
                base.Enqueue(item);
            }
        }

        public class RawMessage
        {
            // is sender for incoming messages and receiver for outgoing
            long sendreceiver;
            public string SenderReceiverBase64;
            public long SenderReceiver
            {
                get
                {
                    return sendreceiver;
                }
                set
                {
                    sendreceiver = value;
                    SenderReceiverBase64 = Convert.ToBase64String(BitConverter.GetBytes(value));
                }
            }
            public bool isGlobal;
            MessageType type;
            public string TypeBase64;
            public MessageType Type
            {
                get
                {
                    return type;
                }
                set
                {
                    type = value;
                    TypeBase64 = Convert.ToBase64String(BitConverter.GetBytes((int)value));
                }
            }
            public byte[] Args;
            public RawMessage(long senderreceiver, bool isglobal, MessageType type, byte[] args)
            {
                SenderReceiver = senderreceiver;
                isGlobal = isglobal;
                Type = type;
                Args = args;
            }
            public RawMessage() { }
        }

        public enum MessageType
        {
            HEARTBEAT = 0,
            CONNECT = 1,
            UPDATESTATUS = 2,
            DISCONNECT = 3,
            SENDMETADATA = 4,
            CUSTOMMESSAGE = 5
        }
        public class Client
        {
            long id;
            public string IDBase64;
            public long ID
            {
                get
                {
                    return id;
                }
                set
                {
                    id = value;
                    IDBase64 = Convert.ToBase64String(BitConverter.GetBytes(id));
                }
            }
            long gridid;
            public string GridIDBase64;
            public long GridID
            {
                get
                {
                    return gridid;
                }
                set
                {
                    gridid = value;
                    GridIDBase64 = Convert.ToBase64String(BitConverter.GetBytes(gridid));
                }
            }
            public CommManager Comms;
            public ClientMetaData MetaData { get; set; }
            public long AvailableID
            {
                get
                {
                    if (ID == 0)
                    {
                        if (GridID == 0)
                        {
                            return 0;
                        }
                        return GridID;
                    }
                    return ID;
                }
            }
            public string Name;

            public Client(long id, long gridid, CommManager comms)
            {
                ID = id;
                GridID = gridid;
                Comms = comms;
            }
            public static Client GenerateMyClient(CommManager comms)
            {
                long ID = comms.ship.Me.Me.EntityId;
                long GridID = comms.ship.Me.Me.CubeGrid.EntityId;

                var cl = new Client(ID, GridID, comms);
                cl.Name = comms.ship.ControllerBlock.CubeGrid.CustomName;
                return cl;
            }
            public byte[] SerializeClientData()
            {
                using (BinarySerializer s = new BinarySerializer(16))
                {
                    s.WriteLong(this.ID);
                    s.WriteLong(GridID);
                    s.WriteString(Name);
                    return s.buffer;
                }

            }

            public static Client GetFromSerializer(BinarySerializer buf, CommManager comms)
            {
                long id = buf.ReadLong();
                long gridid = buf.ReadLong();
                string name = buf.ReadString();
                return new Client(id, gridid, comms) { Name = name };
            }
            public struct ClientMetaData
            {
                public string ShipName { get; private set; }
                public ClientMetaData(string shipname)
                {
                    this.ShipName = shipname;
                }
                public void Serialize(BinarySerializer buf)
                {
                    buf.WriteString(ShipName);
                }
                public static ClientMetaData Deserialize(BinarySerializer buf)
                {
                    return new ClientMetaData(buf.ReadString());
                }
            }
        }
        public class TypeTree
        {
            public static Type[] typeTable = new Type[] { typeof(int), typeof(long), typeof(string), typeof(Double), typeof(Vector3D), typeof(MatrixD) };
            public object DeserializeFromType(BinarySerializer buf, int type)
            {
                return buf.Read(typeTable[type]);
            }
        }
        
    }
}

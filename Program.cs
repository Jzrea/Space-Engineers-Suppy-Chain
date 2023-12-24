using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //Configs
        const string INI_SECTION = "Logistics-Managment",
                CHANNELSTATION = INI_SECTION + "-Station",
                CHANNELSHIP = INI_SECTION + "-Ship",
                DISTPORT= "distribution-port";

        ////////SAM Configs
        // Change the tag used to identify blocks        
        public static string TAG = "SAM";

        ////////No Touchy

        //PROPS
        Ship ship;
        Station station;

        IMyUnicastListener myUnicastListener;
        IMyBroadcastListener myBroadcastListener;

        static Program _p;
        static MyIni myDataIni = new MyIni();
        static bool isShip = true;
        static int _runCount = 0;

        void updatePriority()
        {

        }

        void updateRegular()
        {
            if (isShip)
            {
                //getData();
                ship.main();
            }
            else
            {
                //station.panelWrite();
            }
        }


        public void Save()
        {            
            if (isShip)
                ship.updateShipData();
            else
                station.updateData();

            Me.CustomData = myDataIni.ToString();
        }
        public Program()
        {

            _p = this;            
            Me.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
            Me.GetSurface(0).BackgroundColor = Color.Black;
            Me.GetSurface(0).FontColor = Color.Green;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            isShip = !(Me.CubeGrid.IsStatic);
            if (isShip) 
            {
                ship = new Ship(getGridBlocks());
                ship.getShipData();
                myUnicastListener = IGC.UnicastListener;
                myUnicastListener.SetMessageCallback(CHANNELSHIP);
                myBroadcastListener = IGC.RegisterBroadcastListener(CHANNELSHIP);
                myBroadcastListener.SetMessageCallback(CHANNELSHIP);
            }
            else
            {
                station = new Station(getGridBlocks());
                myUnicastListener = IGC.UnicastListener;
                myUnicastListener.SetMessageCallback(CHANNELSTATION);
                myBroadcastListener = IGC.RegisterBroadcastListener(CHANNELSTATION);
                myBroadcastListener.SetMessageCallback(CHANNELSTATION);
            }
        }
        public void Main(string argument, UpdateType updateSource)
        {
            if((updateSource & UpdateType.Terminal)>0) argument = argument.ToLower();
            #region main
            if ((updateSource & UpdateType.IGC) > 0) communications(argument);
            if (isShip)
            {
                //ship.SamMain(argument, updateSource);
                switch (argument)
                {
                    case "next":
                        ship.nextPort(); break;
                    case "update":
                        ship.getShipData();break;
                    case "unload":
                        ship.unloadCargo(); break;
                }
                if ((updateSource & UpdateType.Update10) != 0)
                {
                    updatePriority();

                    ++_runCount;
                    if (_runCount == 60)
                    {
                        ship.manangerTransactions();
                        _runCount = 0;
                    }else if (_runCount == 30)
                        ship.deliver(); 
                    else if (_runCount % 10 == 0){                        
                        updateRegular();
                        Echo(ship.debug());
                    }
                    
                }
            }
            else
            {
                switch (argument)
                {
                    case "scan":
                        station.scanItems(argument);
                        break;
                    case "clear":
                        station.clear();
                        break;
                    case "update":
                        station.getData();
                        break;
                    case "save":
                        station.updateData();
                        Me.CustomData = myDataIni.ToString();
                        break;
                }                
                if ((updateSource & UpdateType.Update10) != 0)
                {
                    updatePriority();

                    ++_runCount;
                    if (_runCount == 60){
                        station.manageLogistics();
                        station.panelWrite();
                        _runCount = 0;
                    }else if (_runCount % 5 == 0 && _runCount % 10 != 0){
                        station.panelRead();
                    }else if (_runCount % 10 == 0){
                        station.scanItems("update");
                    }    
                    Echo(station.debug() + "runCount: " + _runCount);                    
                }                

            }
            #endregion
            //#region COMMUNICATIONS
            //#endregion
            #region TEST
            if (argument.Contains(Station.Response.item_request.ToString()))
            {
                station.checkSupply();                
                station.panelCommsPrint("\nto all: " + argument);
            }
            else if (argument.Contains("echo"))
            {
                long address = long.Parse(argument.Substring(argument.IndexOf(' ') + 1));
                IGC.SendUnicastMessage(address, CHANNELSTATION, "echo");
                station.panelCommsPrint("\n\tto all: " + "echo");
            }
            #endregion
        }

        private bool subscribe(MyIGCMessage payload)
        {
            string gridName = _p.Me.CubeGrid.CustomName;
            //myBroadcastListener.
            _p.IGC.SendUnicastMessage(payload.Source, CHANNELSTATION, $"{Station.Response.register}\n{gridName},{Me.CubeGrid.GetPosition()}");
            return true;
        }

        private bool register(MyIGCMessage payload)
        {
            string[] props = payload.Data.ToString().Replace($"{Station.Response.register}\n", "").Split(',');
            Vector3D position;
            Vector3D.TryParse(props[1], out position);
            NetworkClient client = new NetworkClient(payload.Source, props[0], position);
            subscribers[payload.Source] = client;
            subscribers.OrderBy(subs => subs.Key);
            return true;
        }

        static Dictionary<long, NetworkClient> subscribers = new Dictionary<long, NetworkClient>();
        List<MyIGCMessage> unprocessedMessages = new List<MyIGCMessage>();

        bool processMessages(string argument, MyIGCMessage payload)
        {
            string data = payload.Data.ToString();
            //_p.station.panelCommsPrint("\npayload:"+data +"\n");
            NetworkClient subcriber;
            if (data.Contains($"{Station.Response.subscribe}")) subscribe(payload);
            else if (data.Contains($"{Station.Response.register}")) register(payload);


            //bool isSubscriber = ?;
            if (!subscribers.TryGetValue(payload.Source, out subcriber))
            {
                _p.IGC.SendUnicastMessage(payload.Source, CHANNELSTATION, $"{Station.Response.subscribe}");
                return false;
            }

            if (isShip) ship.dispatcher(subcriber, data);
            else station.dispatcher(subcriber, data);
            //else if (data.Contains($"{Response.transaction}")) addTransaction(data.Replace($"{Response.transaction}\n", ""));

            return true;
        }

        void communications(string argument)
        {
            //_p.station.panelCommsPrint("\nArgs: " + argument + "\n");
            try
            {
                foreach (MyIGCMessage message in unprocessedMessages.ToList())
                {
                    bool isProcessed = processMessages(argument, message);
                    if (isProcessed) unprocessedMessages.Remove(message);
                }
                #region Broadcast Listener
                while (myBroadcastListener.HasPendingMessage)
                {
                    MyIGCMessage payload = myBroadcastListener.AcceptMessage();
                    if (payload.Tag != CHANNELSTATION) return;
                    bool isProcessed = processMessages(argument, payload);
                    if (!isProcessed && !unprocessedMessages.Exists(m => m.Data.ToString().Equals(payload.Data.ToString()))) unprocessedMessages.Add(payload);
                }
                #endregion
                #region UNICAST LISTENER
                while (myUnicastListener.HasPendingMessage)
                {
                    MyIGCMessage payload = myUnicastListener.AcceptMessage();

                    bool isProcessed = processMessages(argument, payload);
                    if (!isProcessed && !unprocessedMessages.Exists(m => m.Data.ToString().Equals(payload.Data.ToString()))) unprocessedMessages.Add(payload);
                }


                #endregion
            }
            catch (Exception ex)
            { 
                Echo(ex.ToString());
            }
        }



        double getDistance(Vector3D gridA, Vector3D gridB)
        {
            return Vector3D.Distance(gridA, gridB);
        }

        public void debug(string message)
        {
            Echo(message);
        }
    }
}

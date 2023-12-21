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


        static Program _p;
        static MyIni myDataIni = new MyIni();
        bool isShip = true;
        int _runCount = 0;

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
            }
            else
            {
                station = new Station(getGridBlocks());
            }
        }
        public void Main(string argument, UpdateType updateSource)
        {
            argument = argument.ToLower();            
            #region main
            if (isShip)
            {
                //ship.SamMain(argument, updateSource);
                switch (argument)
                {
                    case "next":
                        ship.nextPort();
                        break;
                    case "update":
                        ship.getShipData();
                        break;
                }
                if ((updateSource & UpdateType.Update10) != 0)
                {
                    updatePriority();

                    ++_runCount;
                    if (_runCount % 6 == 0)
                    {
                        _runCount = 0;
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
                if ((updateSource & UpdateType.IGC) > 0) station.communications(argument);
                if ((updateSource & UpdateType.Update10) != 0)
                {
                    updatePriority();

                    ++_runCount;
                    if (_runCount % 2 == 0 && _runCount != 10 && _runCount != 30)
                    {
                        station.panelRead();
                    }
                    else if (_runCount == 10)
                    {
                        station.scanItems("update");
                    }
                    else if (_runCount >= 20 && _runCount < 30)
                    {
                        
                    }
                    else if (_runCount == 30)
                    {                        
                        station.panelWrite();
                        _runCount = 0;                        
                    }
                    Echo(station.debug() + "runCount: " + _runCount);                    
                }                

            }
            #endregion
            #region COMMUNICATIONS
            #endregion
            #region TEST
            if (argument.Contains(Response.item_request.ToString()))
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

        public void debug(string message)
        {
            Echo(message);
        }
    }
}

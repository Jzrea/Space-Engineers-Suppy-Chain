using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    partial class Program
    {
        public class Dock
        {
            private IMyTerminalBlock block;

            public enum Status { Open,  Occupied, Reserved };
            
            public string id {  get;}
            public string name { get;}
            public Status status { get; set; }

            public Dock(string id, string name, Status status, IMyTerminalBlock block)
            {
                this.id = id;
                this.name = name;
                this.status = status;
                this.block = block;
            }

            public bool IsOpen() { 
                return status == Status.Open;
            }

        }

    }
}

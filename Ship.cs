using Microsoft.Win32;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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

        public class Ship 
        {
            public enum Response
            {
                subscribe,
                register,
                status,
                assign,
                acknowledged,
                hauler
            }
            public enum Status
            {
                idle,
                charging,
                loading,
                unloading,
                loaded
            }
            //MyIni myDataIni = new MyIni();

            const string COMMAND = "START NEXT";

            private List<IMyTerminalBlock> _containers = new List<IMyTerminalBlock>();
            private List<IMyTerminalBlock> _thrusters = new List<IMyTerminalBlock>();
            private List<IMyTerminalBlock> _rechargables = new List<IMyTerminalBlock>();
            private List<IMyTerminalBlock> _extra = new List<IMyTerminalBlock>();

            private IMyTerminalBlock myShipRemote;
            private IMyTerminalBlock myShipConnector;
            private IMyTerminalBlock _sam;

            private double maxlift = 0;
            private double currentLoad = 0;
            private double currentMass = 0;
            private bool isRefilling =true;
            private bool isCharging = false;
            /////////////PUBLIC////////////
            public Status state = Status.idle;            
            /////////////PRIVATE////////////
            private string shipID { get; set; } = "setMyID";
            private double shipEmptyMass { get; set; } = 0;
            private bool autoUnloadAll { get; set; } = false;
            private bool loop { get; set; } = false;
            private byte route { get; set; } = 0;
            private double treshold { get; set; } = 95;       // Percent
            private double fuelMinimum { get; set; } = 0;


            public Ship(List<IMyTerminalBlock> blocks)
            {
                setup(blocks);
            }

            public void updateShipData()
            {

                myDataIni.Clear();
                myDataIni.AddSection(INI_SECTION);
                myDataIni.Set(INI_SECTION, "MyID", shipID);
                myDataIni.Set(INI_SECTION, "EmptyMass", shipEmptyMass);
                myDataIni.Set(INI_SECTION, "Treshold", treshold);
                myDataIni.Set(INI_SECTION, "MinimumFuel", fuelMinimum);
                myDataIni.Set(INI_SECTION, "autoUnloadAll", autoUnloadAll);
                myDataIni.Set(INI_SECTION, "Loop", loop);
                myDataIni.Set(INI_SECTION, "Route", route);
                myDataIni.SetEndComment("0 - Loading, 1 - Onroute,2 - Unloading, 3 - Onroute; set to  current mode");
                //myDataIni.Set(myData, "ShipQueue", compileList(queue));
                _p.Me.CustomData = myDataIni.ToString();
            }
            public void getShipData()
            {
                if (_p.Me.CustomData.Length == 0)
                {
                    updateShipData();
                }
                else if (myDataIni.TryParse(_p.Me.CustomData, INI_SECTION))
                {
                    shipID = myDataIni.Get(INI_SECTION, "MyID").ToString();
                    shipEmptyMass = myDataIni.Get(INI_SECTION, "EmptyMass").ToDouble();
                    treshold = myDataIni.Get(INI_SECTION, "Treshold").ToDouble();
                    fuelMinimum = myDataIni.Get(INI_SECTION, "MinimumFuel").ToDouble();
                    autoUnloadAll = myDataIni.Get(INI_SECTION, "autoUnloadAll").ToBoolean();
                    loop = myDataIni.Get(INI_SECTION, "Loop").ToBoolean();
                    route = myDataIni.Get(INI_SECTION, "Route").ToByte();
                    //decryptList(myDataIni.Get(myData, "ShipQueue").ToString());
                }
            }
            #region Actions
            private void sendStatus(NetworkClient client)
            {
                if (state == Status.charging || state == Status.loaded) return;
                IMyTextSurface screen = _p.Me.GetSurface(0);
                _p.IGC.SendUnicastMessage(client.ID, CHANNELSTATION, $"{Response.status}\n{screen.GetText()}");
            }            



            #endregion
            #region Communications            
            public void dispatcher(NetworkClient client, string data)
            {
                //if (!data.Contains($"{Response.subscribe}") && !data.Contains($"{Response.register}"))
                    //panelCommsPrint($"\n{client.Name}: {data}");

                    if (data.Contains($"{Response.status}") || data.Contains($"{Response.hauler}")) sendStatus(client);
                    else if (data.Contains($"{Response.assign}")) Transaction.updateTransaction(data.Replace($"{Response.assign}\n", ""));
                    else if (data.Contains($"{Station.Response.stationed}")) dockedTo = long.Parse(data.Replace($"{Station.Response.stationed}\n", ""));
            }
            #endregion
            //PULL - CARGO-IN
            //PUSH - CARGO-OUT
            long dockedTo = -1;
            public void manangerTransactions()
            {
                if (((IMyShipConnector)myShipConnector).Status == MyShipConnectorStatus.Unconnected) return;
                if (dockedTo == -1) {
                    _p.transmitToAllConnectedGrid(CHANNELSTATION, Station.Response.stationed.ToString(), "");
                    return;
                };
                IMyTextSurface screen = _p.Me.GetSurface(0);
                Transaction[] transactions = Transaction.ParseTransactions(screen.GetText());
                foreach (Transaction transaction in transactions){
                    if (transaction.State == Transaction.Status.Loaded || transaction.State == Transaction.Status.Delivered || transaction.State == Transaction.Status.Failed) continue;
                        if (transaction.Reciever == dockedTo)
                    {
                        transaction.Orders = cargo(Cargo.UNLOAD,transaction.Orders.ToDictionary(kv => kv.Name, kv => kv));
                        transaction.State = Transaction.Status.Unloading;
                        if (transaction.Orders.Length<=0) transaction.State = Transaction.Status.Delivered;
                        else transaction.State = Transaction.Status.Unloading;
                    }
                    else if (transaction.Sender == dockedTo) {
                        bool loaded = cargo(Cargo.LOAD,transaction.Orders.ToDictionary(kv => kv.Name, kv => kv)).Length <= 0;
                        if (loaded) transaction.State = Transaction.Status.Loaded;
                        else transaction.State = Transaction.Status.Loading;
                    }
                }
                screen.WriteText(Transaction.StringifyTransactions(transactions)); 
            }
            enum Cargo
            {
                LOAD,
                UNLOAD
            }
            public void deliver()
            {
                if (((IMyShipConnector)myShipConnector).Status == MyShipConnectorStatus.Unconnected) return;
                IMyTextSurface screen = _p.Me.GetSurface(0);
                Transaction[] transactions = Transaction.ParseTransactions(screen.GetText());
                foreach (Transaction transaction in transactions)
                {
                    if (transaction.State == Transaction.Status.Loaded || transaction.State == Transaction.Status.Delivered) continue;
                    return;
                }
                _p.IGC.SendUnicastMessage(Array.Find(transactions,t=>t.Reciever!=dockedTo).Reciever, CHANNELSTATION, $"{Station.Response.dock_available}\n");
            }

            private Order[] cargo(Cargo method,Dictionary<string,Order> orders)
            {
                List<IMyTerminalBlock> containers = _p.getOffGridBlocks();

                _containers.ForEach(container =>
                {
                    if (((IMyCargoContainer)container).InventoryCount > 0)
                    {

                        containers.ForEach(OGContainer =>
                        {
                            List<MyInventoryItem> items = new List<MyInventoryItem>();
                            ((IMyCargoContainer)OGContainer).GetInventory().GetItems(items, item =>
                            {
                                string itemName = item.Type.SubtypeId.Trim();
                                itemName = (item.Type ==
                                    MyItemType.MakeIngot(item.Type.SubtypeId)) ? "Ingot_" + itemName :
                                    (item.Type == MyItemType.MakeOre(item.Type.SubtypeId) && !itemName.Equals("Ice")) ? "Ore_" + itemName : itemName;
                                Order _;
                                return orders.TryGetValue(itemName, out _);
                            });
                            foreach (MyInventoryItem item in items)
                            {
                                if (((IMyCargoContainer)container).GetInventory().CanTransferItemTo(((IMyCargoContainer)OGContainer).GetInventory(), item.Type))
                                {
                                    string itemName = item.Type.SubtypeId.Trim();
                                    itemName = (item.Type ==
                                        MyItemType.MakeIngot(item.Type.SubtypeId)) ? "Ingot_" + itemName :
                                        (item.Type == MyItemType.MakeOre(item.Type.SubtypeId) && !itemName.Equals("Ice")) ? "Ore_" + itemName : itemName;
                                    Order order;
                                    if (!orders.TryGetValue(itemName, out order)) continue;
                                    int allowance = ((IMyCargoContainer)OGContainer).GetInventory().MaxVolume.ToIntSafe() - ((IMyCargoContainer)OGContainer).GetInventory().CurrentVolume.ToIntSafe();
                                    if (item.Amount.ToIntSafe() >= order.Amount && allowance >= order.Amount)
                                    {
                                        if (method == Cargo.UNLOAD) {
                                            transferItem(
                                                ((IMyCargoContainer)container).GetInventory(),
                                                ((IMyCargoContainer)OGContainer).GetInventory(),
                                                item, order.Amount
                                                );
                                        }
                                        else transferItem(
                                            ((IMyCargoContainer)OGContainer).GetInventory(),
                                            ((IMyCargoContainer)container).GetInventory(),
                                            item, order.Amount
                                            );
                                        orders.Remove(itemName);
                                    }
                                    else
                                    {
                                        int amount = item.Amount.ToIntSafe() >= allowance ? item.Amount.ToIntSafe() : allowance;
                                        if (method == Cargo.UNLOAD) transferItem(
                                            ((IMyCargoContainer)container).GetInventory(),
                                            ((IMyCargoContainer)OGContainer).GetInventory(),
                                            item, amount
                                            );
                                        else
                                            transferItem(
                                            ((IMyCargoContainer)OGContainer).GetInventory(),
                                            ((IMyCargoContainer)container).GetInventory(),
                                            item, amount
                                            );
                                        order.Amount = Math.Abs(amount - order.Amount);
                                        if(order.Amount<=0) orders.Remove(itemName);
                                    }
                                }
                            }
                        });
                    }
                });
                return orders.Values.ToArray();
            }

            public void unloadCargo()
            {
                List<IMyTerminalBlock> containers = _p.getOffGridBlocks();

                _containers.ForEach(container =>
                {
                    if (((IMyCargoContainer)container).InventoryCount > 0)
                    {

                        containers.ForEach(OGContainer =>
                        {
                            if (((IMyCargoContainer)container).GetInventory().IsConnectedTo(((IMyCargoContainer)OGContainer).GetInventory()))
                            {
                                transferEverything(((IMyCargoContainer)container).GetInventory(), ((IMyCargoContainer)OGContainer).GetInventory());
                            }
                        });
                    }
                });
            }

            private void transferItem(IMyInventory src, IMyInventory dst, MyInventoryItem item,int amount)
            {
                src.TransferItemTo(dst,item, amount);
            }

            private void transferEverything(IMyInventory src, IMyInventory dst)
            {
                for (int i = 0; i < 32; i++)
                {
                    dst.TransferItemFrom(src, i, null, true, null);
                }
            }

            public void nextPort()
            {
                iterateRoute();
                isCharging = discharge();
                updateShipData();
                ((IMyProgrammableBlock)_sam).TryRun(COMMAND);
                //isCharging = false;
            }

            

            public void main()
            {
                currentLoad = GetPercentUsage();
                currentMass = getWeightUsage();
                if (((IMyShipConnector)myShipConnector).Status == MyShipConnectorStatus.Connected)
                {                    
                    if (!isCharging) {
                        iterateRoute();
                        isCharging = recharge();
                        updateShipData();
                    }
                    double liftCapacity = currentMass / maxlift * 100.0;
                    bool aboveTreshold = Math.Round(liftCapacity) >= treshold || Math.Round(currentLoad) >= treshold;
                    switch (route)
                    {
                        case 0:
                            break; 
                        case 1:
                            break; 
                        case 2:
                            if (!autoUnloadAll) break;                            
                            break; 
                        case 3: 
                            break; 
                        default:
                            break;
                    }
                    if ((aboveTreshold && route == 0) || (currentLoad <= 0 && route == 2 && loop))
                    {
                        if (!isRefilling && getFuelPercentUsage() >= fuelMinimum) { 
                            nextPort();
                            dockedTo = -1;
                    }
                    else
                    {
                        isRefilling = getFuelPercentUsage() < 100;
                        if (isRefilling)
                        {
                            recharge();
                        }
                    }
                    }
                }
            }
            
            public string debug()
            {
                return $@"Cargo Drone {shipID}
DEBUG 
Found {_containers.Count} Containers.
Found {_thrusters.Count} Thrusters.
Found {_rechargables.Count} Rechargables.
Connector {((myShipConnector != null) ? "Found" : "Not Found")}.
Programmable Block {((_sam != null) ? "Found" : "Not Found")}.
Fuel Fill/Capacity: {getFuelPercentUsage():F1}%
Weight Current/Max:
    {currentMass:F1}kg : {currentMass / maxlift * 100.0:F1}%
    {maxlift:F1}kg : 100%.
Containers are at {currentLoad:F1}% : {treshold}% usage.
Loop: {loop}
IsCharging {isCharging}
IsRefilling {isRefilling}
Transport Status {route}
";
            }

            /////////////PRIVATE/////////////

            void setup(List<IMyTerminalBlock> blocks)
            {
                blocks.ForEach(block =>
               {
                   if ((block is IMyShipController))
                   {
                       myShipRemote = block;
                   }
                   else if (block is IMyShipConnector)
                   {
                       myShipConnector = block;
                       isCharging = ((IMyShipConnector)myShipConnector).Status == MyShipConnectorStatus.Connected;
                   }
                   else if (block is IMyGasTank || block is IMyBatteryBlock)
                       _rechargables.Add(block);
                   else if (_p.isContainer(block))
                       _containers.Add(block);
                   else if (block is IMyThrust) {
                       _thrusters.Add(block);
                       if (block.Orientation.TransformDirection(Base6Directions.Direction.Forward) == Base6Directions.Direction.Down)
                       {
                           float thrustPower = (block as IMyThrust).MaxEffectiveThrust;
                           maxlift += thrustPower / 9.81f;
                       }
                   }else if (block is IMyProgrammableBlock && block.CustomName.Contains("[SAM]"))
                       _sam = block;
                   else if (block is IMyLightingBlock || block is IMyTimerBlock)
                       _extra.Add(block);
               });          
            }

            void iterateRoute()
            {
                //route =(route<=3)?1:0;
                route++;
                if (route > 3)
                    route = 0;

            }

            private MyTuple<MyFixedPoint, MyFixedPoint> GetVolumeUsedAndTotal(IMyTerminalBlock block)
            {
                MyFixedPoint total = 0;
                MyFixedPoint used = 0;
                for (int i = 0; i < block.InventoryCount; ++i)
            {
                    var inventory = block.GetInventory(i);
                    total += inventory.MaxVolume;
                    used += inventory.CurrentVolume;
                }
                return MyTuple.Create(used, total);
            }

            private float GetPercentUsage()
            {
                var summed = _containers.Select(GetVolumeUsedAndTotal).Aggregate((a, b) => MyTuple.Create(a.Item1 + b.Item1, a.Item2 + b.Item2));
                return (float)summed.Item1 / (float)summed.Item2 * 100.0f;
            }

            private double getFuelPercentUsage()
            {
                double fuelUsage = 0.00;
                double fuelCapacity = 0.00;
                _rechargables.ForEach(block =>
                {
                    if(block is IMyGasTank)
                    {
                        fuelCapacity += ((IMyGasTank)block).Capacity;
                        fuelUsage += ((IMyGasTank)block).Capacity * ((IMyGasTank)block).FilledRatio;
                    }
                });
                return fuelUsage / fuelCapacity * 100.0;
            }


            private double getWeightUsage()
            {
                double initialMass = shipEmptyMass;
                _containers.ForEach(cn => {
                    initialMass += (double)cn.GetInventory().CurrentMass;
                });
                return initialMass / Math.Round(((IMyShipController)myShipRemote).GetTotalGravity().Length(), 2);
            }

            private bool discharge()
            {
                _rechargables.ForEach(b => {
                    if (b is IMyGasTank)
                        b.ApplyAction("Stockpile_Off");
                    else
                        b.ApplyAction("Auto");
                });
                _thrusters.ForEach(b => b.ApplyAction("OnOff_On"));
                _extra.ForEach(b => {
                    if (b is IMyTimerBlock) b.ApplyAction("TriggerNow");
                    else b.ApplyAction("OnOff_On");
                });
                return false;
            }

            private bool recharge()
            {
                bool skipped = false;
                _rechargables.ForEach(b => {
                    if (b == null) return;
                    if (b is IMyGasTank)
                        b.ApplyAction("Stockpile_On");
                    else
                    {
                        if(!skipped)
                            b.ApplyAction("Recharge");
                        skipped = true;
                    }
                });
                _thrusters.ForEach(b => b.ApplyAction("OnOff_Off"));
                _extra.ForEach(b => { 
                    if (b is IMyTimerBlock) b.ApplyAction("TriggerNow");
                    else b.ApplyAction("OnOff_Off");                    
                });

                return true;
            }

        }
    }
}

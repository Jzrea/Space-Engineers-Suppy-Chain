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
            /*
            #region Communications            
            Dictionary<long, NetworkClient> subscribers = new Dictionary<long, NetworkClient>();
            int stationSubcriberCount = 0;
            int shipSubcriberCount = 0;

            public void communications(string argument, UpdateType updateSource, IMyIntergridCommunicationSystem IGC)
            {
                try
                {
                    #region LISTEN TO ALL
                    List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
                    IGC.GetBroadcastListeners(listeners);
                    stationSubcriberCount = listeners.Where(lis => lis.Tag.Equals(CHANNELSTATION)).ToList().Count();
                    shipSubcriberCount = listeners.Where(lis => lis.Tag.Equals(CHANNELSHIP)).ToList().Count();
                    if (listeners.Count > 0)
                    {
                        foreach (IMyBroadcastListener listener in listeners)
                        {
                            while (listener.HasPendingMessage)
                            {
                                MyIGCMessage payload = listener.AcceptMessage();
                                string data = payload.Data.ToString();
                                NetworkClient subcriber;
                                subscribers.TryGetValue(payload.Source, out subcriber);
                                if (subcriber != null)
                                    panelCommsPrint($"\n{subcriber.Name}: {data}");

                                if (data.Contains(subscribe.ToLower()))
                                {
                                    string gridName = _p.Me.CubeGrid.CustomName;
                                    IGC.SendUnicastMessage(payload.Source, CHANNELSTATION, $"{register}{gridName},{myPosistion}");
                                }
                                else if (data.Contains(itemRequest.ToLower()))
                                {
                                    List<Order> orders = _p.deserializeOrders(data.Replace(itemRequest, ""));
                                    orders = orders.Where(order => itemList.Find(item => order.Name.Equals(item.Name)) != null).ToList();
                                    List<Order> export = new List<Order>();
                                    orders.ForEach(order =>
                                    {
                                        ItemData item = itemList.Find(itm => itm.Name.Equals(order.Name));
                                        if (item.isOkayForExport())
                                        {
                                            int orderAmount = item.Supply - ((item.Low > 0) ? item.Low : 0);
                                            Order newOrder = new Order(order.Name, orderAmount);
                                            export.Add(newOrder);
                                            item.Reserves = orderAmount;
                                        };

                                    });

                                    if (export.Count > 0)
                                    {
                                        IGC.SendUnicastMessage(payload.Source, CHANNELSTATION, $"{itemAvailable}\n{_p.serializeOrders(export)}");
                                        panelCommsPrint($"\nto {subscribers[payload.Source].Name}: {itemAvailable}\n{_p.serializeOrders(export)}");
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    #region Listen To Addressed 
                    IMyUnicastListener unicastListener = IGC.UnicastListener;
                    //Dictionary<long, > distributors = new Dictionary<long, int>();
                    List<Order> reserves = imports.ToList();
                    while (unicastListener.HasPendingMessage)
                    {
                        MyIGCMessage payload = unicastListener.AcceptMessage();
                        string data = payload.Data.ToString();
                        NetworkClient subcriber;
                        subscribers.TryGetValue(payload.Source, out subcriber);
                        if (subcriber != null)
                            panelCommsPrint($"\n{subcriber.Name}: {data}");
                        if (data.Contains(register.ToLower()) && !subscribers.TryGetValue(payload.Source, out subcriber))
                        {
                            string[] props = payload.Data.ToString().Replace(register, "").Split(',');
                            NetworkClient client = new NetworkClient();
                            client.Name = props[0];
                            Vector3D position;
                            Vector3D.TryParse(props[1], out position);
                            client.Position = position;
                            subscribers.Add(payload.Source, client);
                        }
                        else if (data.Contains(itemAvailable.ToLower()))
                        {
                            List<Order> orders = _p.deserializeOrders(data.Replace(itemAvailable, ""));
                            orders = orders.Where(order => reserves.Find(item => order.Name.Equals(item.Name)) != null).ToList();
                            orders.ForEach(order => {
                                Order item = reserves.Find(r => r.Name.Equals(order.Name));
                                item.Amount -= order.Amount;
                                if (item.Amount <= 0)
                                {
                                    if (item.Amount < 0) order.Amount += item.Amount;
                                    reserves.Remove(item);
                                };
                            });
                            string message = _p.serializeOrders(orders);
                            IGC.SendUnicastMessage(payload.Source, CHANNELSTATION, $"{itemReserve}\n{message}");
                            subscribers.TryGetValue(payload.Source, out subcriber);
                            panelCommsPrint($"\nto {subcriber.Name}: {itemReserve}\n{message}");

                        }
                        else if (data.Contains(itemReserve.ToLower()))
                        {
                            List<Order> orders = _p.deserializeOrders(data.Replace(itemReserve, ""));
                            orders.ForEach(order => {
                                ItemData item = itemList.Find(import => import.Name.Equals(order.Name));
                                item.Reserves = order.Amount;
                            });
                            IGC.SendUnicastMessage(payload.Source, CHANNELSTATION, $"{acknowledged}");
                            panelCommsPrint($"\\\\\\\\\\\\\\END TRANSACTION\\\\\\\\\\\\\\");
                            string message = $"pull\n{payload.Source}:{_p.serializeOrders(orders)}";
                            if (shipSubcriberCount > 0)
                            {
                                long ship = listeners.Where(lis => lis.Tag.Equals(CHANNELSHIP)).ToList()[0].AcceptMessage().Source;
                                IGC.SendUnicastMessage(ship, CHANNELSHIP, message);

                            }
                            else
                                exports.Add(message);
                        }
                        else if (data.Contains(acknowledged.ToLower()))
                        {
                            if (reserves.Count <= 0) panelCommsPrint($"\\\\\\\\\\\\\\END TRANSACTION\\\\\\\\\\\\\\");

                        }
                    }

                    if (subscribers.Count != stationSubcriberCount)
                    {
                        IGC.SendBroadcastMessage(CHANNELSTATION, subscribe);
                        subscribers.OrderBy(sub => getDistance(myPosistion, sub.Value.Position));
                        panelCommsPrint($"\nto all: {subscribe}");
                    }
                    #endregion                    
                }
                catch (Exception ex)
                {
                    panelCommsPrint(ex.ToString());
                }
            }

            double getDistance(Vector3D gridA, Vector3D gridB)
            {
                return Vector3D.Distance(gridA, gridB);
            }

            public void panelCommsPrint(string content)
            {
                foreach (IMyTextPanel panel in _textPanels)
                {
                    if (panel.CustomName.ToLower().Contains("[comms]"))
                    {
                        //panel.WriteText($"\nsent to: {address}", true);
                        panel.WriteText(content, true);
                    }
                }
            }
            #endregion*/
            private void despositCargo()
            {
                
            }

            private void withdrawCargo()
            {
                List<IMyTerminalBlock> containers = _p.getOffGridBlocks();  
                
                _containers.ForEach(container =>
                {
                    if (((IMyCargoContainer)container).InventoryCount>0)
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
                            withdrawCargo();
                            break; 
                        case 3: 
                            break; 
                        default:
                            break;
                    }
                    if ((aboveTreshold && route == 0) || (currentLoad <= 0 && route == 2 && loop))
                    {
                        if(!isRefilling && getFuelPercentUsage()>= fuelMinimum)
                            nextPort();
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

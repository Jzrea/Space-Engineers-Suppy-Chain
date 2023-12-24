using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
//using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
//using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Scripting;
using VRageMath;
using VRageRender;

namespace IngameScript
{
    partial class Program
    {
        public class Station
        {
        public enum Response
        {
            subscribe,
            register,
            item_request,
            item_available,
            item_reserved,
            acknowledged,
            stationed,
            dock_available
        }

            string stationID = "setMyID";
            
            //MyIni myDataIni = new MyIni();
            //Vector3D myPosistion;            

            List<IMyTerminalBlock> _containers = new List<IMyTerminalBlock>();
            //List<IMyTerminalBlock> _docks = new List<IMyTerminalBlock>();
            Dictionary<string,Dock> _docks = new Dictionary<string,Dock>();

            public List<IMyTerminalBlock> _textPanels = new List<IMyTerminalBlock>();
            //List<ItemData> itemList = new List<ItemData>();
            Dictionary<string,ItemData> itemList = new Dictionary<string,ItemData>();

            #region Station_Setup
            public Station(List<IMyTerminalBlock> blocks)
            {                
                //myPosistion = _p.Me.CubeGrid.GetPosition();                
                setup(blocks);
            }


            public void updateData()
            {

                myDataIni.Clear();
                myDataIni.AddSection(INI_SECTION);
                myDataIni.Set(INI_SECTION, "MyID", stationID);
                //myDataIni.Set(INI_SECTION, "transactions", Transaction.StringifyTransactions(transactions));
                //myDataIni.Set(INI_SECTION, "exports", compileList(reservations));
                //myDataIni.Set(myData, "ShipQueue", compileList(queue));
                //_p.Me.CustomData = myDataIni.ToString();                
            }
            public void getData()
            {
                if (_p.Me.CustomData.Length == 0)
                {
                    updateData();
                }
                else if (myDataIni.TryParse(_p.Me.CustomData, INI_SECTION))
                {
                    stationID = myDataIni.Get(INI_SECTION, "MyID").ToString();
                    //transactions = Transaction.ParseTransactions(myDataIni.Get(INI_SECTION, "transactions").ToString());
                    //decompileList(myDataIni.Get(INI_SECTION, "transactions").ToString(), transactions);
                    //decompileList(myDataIni.Get(INI_SECTION, "exports").ToString(), reservations);
                    //decryptList(myDataIni.Get(myData, "ShipQueue").ToString());````````````````````````````````````````   11111111111
                    //IMyTextSurface screen = _p.Me.GetSurface(0);
                    //try
                    //{
                    //    transactions = Transaction.ParseTransactions(screen.GetText());
                    //}
                    //catch
                    //{
                    //    screen.WriteText("");
                    //}
                }
            }

            void setup(List<IMyTerminalBlock> blocks)
            {
                blocks.ForEach(block =>
                {
                    MyIni _ini = new MyIni();
                    if (block is IMyShipConnector && _ini.ContainsSection(INI_SECTION))
                    {
                        Dock dock = new Dock(
                            $"{block.BlockDefinition}",
                            block.CustomName, 
                            ((IMyShipConnector)block).IsConnected ? Dock.Status.Occupied : Dock.Status.Open,
                            block);
                        _docks[$"{block.BlockDefinition}"] = dock;
                        block.CustomData = "SAM\n";
                    }
                    else if (_p.isContainer(block))
                    {
                        _containers.Add(block);
                        //myInventorys.Add(block.GetInventory());
                    }
                    else if (block is IMyTextPanel && (block.CustomName.ToLower().Contains("[audit]") || block.CustomName.ToLower().Contains("[comms]")))
                    {
                        _textPanels.Add(block);
                        ((IMyTextPanel)block).Font = "Monospace";
                        //((IMyTextPanel)block).WriteText("");
                        ((IMyTextPanel)block).FontSize = (((IMyTextPanel)block).SurfaceSize.X == 512) ? 0.38f : 0.7900f; // Adjust the value as needed                        
                        ((IMyTextPanel)block).ContentType = ContentType.TEXT_AND_IMAGE;
                        ((IMyTextPanel)block).BackgroundColor = Color.Black;
                        ((IMyTextPanel)block).FontColor = Color.Green;
                        if (block.CustomName.ToLower().Contains("[comms]"))
                        {
                            ((IMyTextPanel)block).WriteText("");
                            ((IMyTextPanel)block).WriteText("MyID: " + _p.IGC.Me, false);
                        }
                        //panelWidth = (int)((IMyTextPanel)block).FontSize * 70; // Assuming default font size
                        //columnWidth = panelWidth / 4;

                    }
                });
            }
            #endregion
            #region Inventory_Display
            public void clear()
            {
                itemList.Clear();
                if (_textPanels.Count > 0)
                    ((IMyTextPanel)_textPanels[0]).WriteText("");
            }

            string TableRow(string column1, string column2, string column3, string column4)
            {
                return FormatTableCell(column1, 0) + FormatTableCell(column2, 1) + FormatTableCell(column3, 2) + FormatTableCell(column4, 3);

            }

            string FormatTableCell(string value, byte column)
            {
                int columnWidth = (column == 0) ? 40 : 10;
                string formattedValue = value.PadRight(columnWidth);

                if (formattedValue.Length > columnWidth)
                    formattedValue = formattedValue.Substring(0, columnWidth - 3) + "...";

                return formattedValue;
            }

            Dictionary<string, ItemData> ExtractItemDetails(IMyTextPanel textPanel)
            {
                Dictionary<string, ItemData> itemReadList = new Dictionary<string, ItemData>();

                string[] lines = textPanel.GetText()
                    .Replace("  ", " ").Replace("  ", " ")
                    .Replace("  ", " ").Replace("  ", " ")
                    .Replace("  ", " ").Replace("  ", " ")
                    .Replace("  ", " ").Replace("  ", " ")
                    .Replace("  ", " ").Replace("  ", " ")
                    .Split('\n');

                // Start from the second row to skip the table header
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] columns = line.Split(' ');

                    if (columns.Length == 4)
                    {
                        string itemName = columns[0].Trim();
                        int lowValue = 0;
                        int highValue = 0;

                        int.TryParse(columns[2].Trim(), out lowValue);
                        int.TryParse(columns[3].Trim(), out highValue);

                        ItemData itemDetails = new ItemData(itemName, lowValue, highValue);

                        itemReadList.Add(itemName,itemDetails);
                    }
                }

                return itemReadList;
            }

            public void panelRead()
            {
                Dictionary<string, ItemData> readList = ExtractItemDetails((IMyTextPanel)_textPanels.Find(panel => panel.CustomName.ToLower().Contains("[audit]")));
                if (readList.Count > 0)
                {
                    //itemList = itemList.Where(item => readList.Find(f => f.Name == item.Name) != null).ToList();
                    itemList = ConvertToDictionary(itemList.Where(item => readList[item.Key] != null));
                    //itemList = itemList.;
                    foreach (ItemData item in readList.Values)
                    {
                        ItemData itemFound;
                        itemList.TryGetValue(item.Name, out itemFound);
                        if (itemFound != null)
                        {
                            itemFound.Low = item.Low;
                            itemFound.High = item.High;
                        }
                        else
                        {
                            ItemData newItem = new ItemData(item.Name, item.Low, item.High);
                            itemList.Add(item.Name, newItem);
                        }
                    }

                    //readList.Values.ForEach(item => {
                    //    //var itemFound = itemList.Find(itemE => itemE.Name == item.Name);
                        
                    //});
                }
            }

            public void panelWrite()
            {
                
                itemList = ConvertToDictionary(itemList.OrderBy(item => item.Key));
                List<IMyTerminalBlock> auditPanels = _textPanels.Where(panel => panel.CustomName.ToLower().Contains("[audit]")).ToList();
                foreach (IMyTextPanel textPanel in auditPanels)
                {
                    textPanel.WriteText("");
                    textPanel.WriteText(TableRow("Item Name", "Supply", "Low", "High"), false);
                    foreach (ItemData item in itemList.Values)
                    {
                        string rows = "\n" + TableRow(item.Name, item.Supply.ToString(), item.Low.ToString(), item.High.ToString());
                        textPanel.WriteText(rows, true);
                        
                    }
                }
            }

            public void scanItems(string type)
            {
                //List<ItemData> tempItemList = new List<ItemData>();
                Dictionary<string,ItemData> tempItemList = new Dictionary<string,ItemData>();
                _containers.ForEach(container =>
                {
                    IMyInventory inventory = container.GetInventory();
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    inventory.GetItems(items);
                    foreach (MyInventoryItem item in items)
                    {
                        string itemName = item.Type.SubtypeId.Trim();
                        itemName = (item.Type ==
                            MyItemType.MakeIngot(item.Type.SubtypeId)) ? "Ingot_"+ itemName :
                            (item.Type == MyItemType.MakeOre(item.Type.SubtypeId)&& !itemName.Equals("Ice")) ? "Ore_"+ itemName : itemName;
                        int itemCount = (int)item.Amount;
                        ItemData itemFound;
                        if (tempItemList.TryGetValue(itemName, out itemFound))
                        {
                            itemFound.Supply += itemCount;
                        }
                        else
                        {
                            ItemData newItem = new ItemData(itemName, itemCount, itemCount, itemCount);
                            //SET LOW to stop exporting
                            //SET HIGH to stop importing
                            //IF LOW is SET TO -1 item depletion is not considered
                            //IF HIGH is SET TP -1 import is unlimited
                            tempItemList.Add(itemName,newItem);
                        }
                    }

                });
                foreach (ItemData item in tempItemList.Values)
                {
                    //var itemFound = itemList.Find(itemE => itemE.Name.Equals(item.Name));                    
                    ItemData itemFound;
                    if (itemList.TryGetValue(item.Name, out itemFound))
                    {
                        itemFound.Supply = item.Supply;
                    }
                    else if (type.Equals("scan"))
                    {
                        itemList.Add(item.Name, item);
                    }
                    //_p.debug(item.Name+" is Found: "+(itemFound != null));
                }
            }
            #endregion

            #region Import/Export
            List<Order> requests = new List<Order>();
            //SortedDictionary<int, Transaction> transactions = new SortedDictionary<int, Transaction>();
            //List<Order> reservations = new List<Order>();
            //List<string> reservations = new List<string>();

            public void checkSupply()
            {
                foreach (ItemData item in itemList.Values)
                {
                    if (item.needImport())
                    {
                        Order order = new Order(item.Name, item.getImport());
                        Order import = requests.Find(x => x.Name.Equals(order.Name));
                        order.Amount -= item.Requested;
                        if (import == null)
                            requests.Add(order);
                        else
                            import.Amount = order.Amount;
                    }
                }
                _p.transmitToAll(CHANNELSTATION, $"{Response.item_request}", Order.StringifyOrders(requests.ToArray()));
            }

            #endregion

            #region Actions

            #region STATION
            //Response to item_request
            private void sendAvailable(NetworkClient subcriber, string data)
            {
                //panelCommsPrint("parsing");
                List<Order> orders = Order.ParseOrders(data).ToList();
                orders = orders.Where(order => {
                    ItemData item;
                    itemList.TryGetValue(order.Name, out item);
                    return item != null && item.isOkayForExport();
                }).ToList();
                if (orders.Count > 0)
                {
                    string strOrders = Order.StringifyOrders(orders.ToArray());
                    _p.IGC.SendUnicastMessage(subcriber.ID, CHANNELSTATION, $"{Response.item_available}\n{strOrders}");
                    panelCommsPrint($"\nto {subcriber.Name}: {Response.item_available}\n{strOrders}");
                }
            }

            private void sendReservations(NetworkClient subcriber, string data)
            {
                List<Order> available = Order.ParseOrders(data).ToList();

                available = available.Where(order => requests.Find(item => order.Name.Equals(item.Name)) != null).ToList();
                available.ForEach(availableOrder => {
                    Order order = requests.Find(r => r.Name.Equals(availableOrder.Name));
                    if (availableOrder.Amount >= order.Amount)
                    {
                        availableOrder.Amount = order.Amount;
                        requests.Remove(order);
                    }
                    else if (order != null)
                    {
                        order.Amount -= availableOrder.Amount;
                    }
                });
                if (!(available.Count > 0)) return;
                string message = Order.StringifyOrders(available.ToArray());
                _p.IGC.SendUnicastMessage(subcriber.ID, CHANNELSTATION, $"{Response.item_reserved}\n{message}");
                subscribers.TryGetValue(subcriber.ID, out subcriber);
                panelCommsPrint($"\nto {subcriber.Name}: {Response.item_reserved}\n{message}");

            }
            private void sendAcknowledgement(NetworkClient subcriber, string data)
            {
                Order[] orders = Order.ParseOrders(data);
                for (int i = 0; i < orders.Length; i++)
                {

                    ItemData item;
                    itemList.TryGetValue(orders[i].Name, out item);
                    if (item != null) item.Reservations += orders[i].Amount;
                }
                Transaction transaction = new Transaction(_p.IGC.Me, subcriber.ID, orders);
                Transaction.updateTransaction(transaction.ToString());
                _p.IGC.SendUnicastMessage(subcriber.ID, CHANNELSTATION, $"{Response.acknowledged}\n{transaction}");
                panelCommsPrint($"\n\\\\\\\\\\\\\\END TRANSACTION\\\\\\\\\\\\\\");

                //string message = $"pull\n{subcriber.ID}:{Order.StringifyOrders(orders)}";
                
            }

            private void acknowledged(string data)
            {
                requests.ForEach(order => {
                    ItemData item;
                    itemList.TryGetValue(order.Name, out item);
                    if (item != null)
                        item.Requested += order.Amount;
                    if (order.Amount <= 0) requests.Remove(order);
                });
                Transaction.updateTransaction(data);
            }

            
            #endregion

            #region SHIP
            //private bool assigned;
            private bool screenShip(NetworkClient subcriber, string data) {
                Dictionary<string,Transaction> shipTransactions = Transaction.ParseTransactions(data).ToDictionary(kv => kv.ID.ToString(), kv => kv);
                //assigned = false;
                foreach (Transaction transaction in shipTransactions.Values)
                {
                    if (transaction.Sender == _p.IGC.Me || transaction.Reciever == _p.IGC.Me) continue;
                    return false;
                }

                IMyTextSurface screen = _p.Me.GetSurface(0);
                Dictionary<string, Transaction> stationTransactions = Transaction
                    .ParseTransactions(screen.GetText()).ToDictionary(kv => kv.ID.ToString(), kv => kv); 
                if (stationTransactions.Count <= 0) return false;
                string key ="";
                if (shipTransactions.Count<=0)
                    key = stationTransactions.FirstOrDefault(kv=>kv.Value.State == Transaction.Status.Idle).Key;
                else
                {
                    Transaction _;
                    key = stationTransactions.FirstOrDefault(kv =>
                    shipTransactions.TryGetValue(kv.Value.ID.ToString(), out _) == false &&
                    kv.Value.State == Transaction.Status.Idle && (
                    (shipTransactions.Values.First().Sender == kv.Value.Sender || shipTransactions.Values.First().Sender == kv.Value.Reciever) ||
                    (shipTransactions.Values.First().Reciever == kv.Value.Sender || shipTransactions.Values.First().Reciever == kv.Value.Reciever))
                    ).Key;
                    //index = Array.IndexOf(stationTransactions, Array.Find(stationTransactions, elem => 
                    
                    //));
                }
                if(string.IsNullOrEmpty(key)) return false;
                stationTransactions[key].State = Transaction.Status.Assigned;
                _p.IGC.SendUnicastMessage(subcriber.ID, CHANNELSHIP, $"{Ship.Response.assign}\n{stationTransactions[key]}");                

                screen.WriteText(Transaction.StringifyTransactions(stationTransactions.Values.ToArray()));
                return true;
            }

            private void sendMyId(long address)
            {
                _p.IGC.SendUnicastMessage(address, CHANNELSHIP, $"{Response.stationed}\n{_p.IGC.Me}");
            }
            #endregion
            #endregion

            #region Communications            

            public void dispatcher(NetworkClient client, string data)
            {
                if (data.Contains($"{Response.subscribe}") || data.Contains($"{Response.register}")) return;
                panelCommsPrint($"\n{client.Name}: {data}");
                //FROM OTHER STATION
                if (data.Contains($"{Response.item_request}")) sendAvailable(client, data.Replace($"{Response.item_request}\n", ""));
                else if (data.Contains($"{Response.item_available}")) sendReservations(client, data.Replace($"{Response.item_available}\n", ""));
                else if (data.Contains($"{Response.item_reserved}")) sendAcknowledgement(client, data.Replace($"{Response.item_reserved}\n", ""));
                else if (data.Contains($"{Response.acknowledged}")) acknowledged(data.Replace($"{Response.acknowledged}\n", ""));
                //FROM SHIPS
                else if (data.Contains($"{Ship.Response.status}")) if (!screenShip(client, data.Replace($"{Ship.Response.status}\n", ""))) _p.IGC.SendBroadcastMessage(CHANNELSHIP, $"{Ship.Response.hauler}\n{_p.IGC}");
                else if (data.Contains($"{Response.stationed}")) sendMyId(client.ID);
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
            #endregion


            #region Logistics

            private Transaction[] updateTransactionData()
            {
                IMyTextSurface screen = _p.Me.GetSurface(0);
                Transaction[] transactions = Transaction.ParseTransactions(screen.GetText());
                foreach (Transaction transaction in transactions)
                {
                    if (transaction.State == Transaction.Status.Loading && transaction.isExpired())
                    {
                        foreach (Order order in transaction.Orders)
                        {
                            ItemData item;
                            if (itemList.TryGetValue(order.Name, out item) && transaction.Sender == _p.IGC.Me)
                                item.Reservations -= order.Amount;
                            else if (item != null)
                                item.Requested -= order.Amount;
                        }
                        transaction.State = Transaction.Status.Failed;
                    }

                }
                transactions = transactions.Where(transaction => transaction.State == Transaction.Status.Idle || transaction.State == Transaction.Status.Loading).ToArray();
                screen.WriteText(Transaction.StringifyTransactions(transactions));
                return transactions;
            }

            public void manageLogistics()
            {
                Transaction[] transactions =  updateTransactionData();
                if (transactions.Length <= 0) return;
                _p.transmitToAllConnectedGrid(CHANNELSHIP, Ship.Response.status.ToString(), "");
            }
            #endregion
            public string debug()
            {
                return $@"Station {stationID}
DEBUG 
Found {_containers.Count} Containers.
Found {_textPanels.Count} Panels.
Item Count {itemList.Count}
Order Count {requests.Count}
";
            }

        }
    }
}

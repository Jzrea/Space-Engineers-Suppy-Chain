using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        class Order
        {
            public string Name { get;}

            public int Amount { get; set; }

            public Order(string name, int amount)
            {
                Name = name;
                Amount = amount;
            }

            public Order(string str)
            {                
                string[] parts = str.Trim().Split(':');

                if (parts.Length >= 2)
                {
                    Name = parts[0].Trim();
                    int amount;
                    int.TryParse(parts[1].Trim(), out amount);
                    Amount = amount;
                }
            }

            public override string ToString()
            {
                return $"{Name}:{Amount}";
            }

            public static string StringifyOrders(Order[] orders)
            {
                return $"[{string.Join(",", orders.Select(order => order.ToString()).ToArray())}]";
            }

            public static Order[] ParseOrders(string orders) {
                if (string.IsNullOrEmpty(orders)) return new Order[0];
                //_p.station.panelCommsPrint("\nDEBUG\n");
                //_p.station.panelCommsPrint(orders.Substring(orders.IndexOf('[') + 1, orders.LastIndexOf(']')));
                //_p.station.panelCommsPrint("\nDEBUG\n");
                string[] lines = orders.Substring(orders.IndexOf('[')+1,orders.LastIndexOf(']')-1).Split(',');
                
                Order[] orderList = new Order[lines.Length];

                for (int i = 0; i < lines.Length; i++)
                {
                    orderList[i] = new Order(lines[i].Trim());
                }

                return orderList;
            }
        }
    }
}

using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {


        class Transaction
        {
            public enum Status
            {
                Idle,
                Assigned,
                Loading,
                Loaded,
                Unloading,                
                Intransit,
                Delivered,
                Failed,
            }
            public int ID { get;}

            public long Sender { get;}
            public long Reciever { get;}
            public Order[] Orders { get; set; }
            public Status State { get; set; }
            public long? Expiry { private get; set; }
            public bool? Urgent { get; }
            public Transaction(long sender, long reciever, Order[] orders,bool urgent)
            {
                ID = GenerateRandomId();
                Sender = sender;
                Reciever = reciever;
                Orders = orders;
                State = Status.Idle;
                Urgent = urgent;
            }
            public Transaction(long sender, long reciever, Order[] orders)
            {                
                ID = GenerateRandomId();
                Sender = sender;
                Reciever = reciever;
                Orders = orders;
                State = Status.Idle;
            }
            public Transaction(string str) {
                
                string[] lines = Split(str.Substring(str.IndexOf('{') + 1, str.LastIndexOf('}')-1));                
                ID = int.Parse(lines[0].Trim());
                Sender = long.Parse(lines[1].Trim()); 
                Reciever = long.Parse(lines[2].Trim());
                Orders = Order.ParseOrders(lines[3].Trim());
                State = StatusParse(lines[4]);
                if(lines.Length==6) Expiry = int.Parse(lines[5].Trim());
            }  

            private static Status StatusParse(string str)
            {
                switch(str.ToLower())
                {
                    case "pending": return Status.Loading;
                    case "fullfilled": return Status.Delivered;
                    case "failed": return Status.Failed;
                    default:
                        return Status.Idle;
                }
            }

            public bool isExpired()
            {
                long now = DateTime.Now.Ticks;
                return Expiry - now <= 0;
            }

            public override string ToString()
            {
                return "{" + $"{ID},{Sender},{Reciever},{Order.StringifyOrders(Orders)},{State}" + ((Expiry!=null) ? $",{Expiry}" : "")+"}" ;
            }

            public static string StringifyTransactions(Transaction[] transactions)
            {
                return "["+string.Join(",\n", transactions.Select(transaction => transaction.ToString()).ToArray())+"]";
            }

            //public static string StringifyTransactionIDs(Transaction[] transactions)
            //{
            //    return "[" + string.Join(",\n", transactions.Select(transaction => transaction.ID).ToArray()) + "]";
            //}
            //public static int[] ParseTransactionIDs(string str)
            //{
            //    if (string.IsNullOrEmpty(str)) return new int[0];
            //    string content = str.Substring(str.IndexOf('[') + 1, str.LastIndexOf(']') - 1).Replace(",\n", ",");
            //    string[] lines = Split(content);
            //    int[] transactions = new int[lines.Length];
            //    for (int i = 0; i < lines.Length; i++)
            //    {
            //        //Transaction newTransaction = new Transaction(lines[i]);
            //        transactions[i] = int.Parse(lines[i]);
            //    }
            //    return transactions;
            //}
            public static Transaction[] ParseTransactions(string str)
            {
                if (string.IsNullOrEmpty(str)) return new Transaction[0];
                string content = str.Replace(",\n",",");
                string[] lines = Split(content.Substring(content.IndexOf('[') + 1, content.LastIndexOf(']') - 1));

                Transaction[] transactions = new Transaction[lines.Length];
                for(int i = 0; i < lines.Length; i++){
                    Transaction newTransaction = new Transaction(lines[i]);
                    transactions[i] = newTransaction;
                }
                return transactions;
            }

            private static int GenerateRandomId()
            {
                // Get current date and time
                DateTime now = DateTime.Now;

                // Convert date and time components to an integer
                int dateTimeValue = now.Year * 100000000 +
                                    now.Month * 1000000 +
                                    now.Day * 10000 +
                                    now.Hour * 100 +
                                    now.Minute * 10 +
                                    now.Second;

                // Generate a random number (between 0 and 999) and append it
                Random random = new Random();
                int randomPart = random.Next(1000);

                // Combine the date/time component and random part
                int randomId = dateTimeValue * 1000 + randomPart;

                return randomId;
            }

            public static void updateTransaction(string data)
            {
                Transaction newTransaction = new Transaction(data);
                IMyTextSurface screen = _p.Me.GetSurface(0);
                Transaction[] transactions = ParseTransactions(screen.GetText());

                screen.WriteText(StringifyTransactions(transactions.Append(newTransaction).ToArray()));
            }
        }
    }
}

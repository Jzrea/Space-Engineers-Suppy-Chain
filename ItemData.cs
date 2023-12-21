namespace IngameScript
{
    partial class Program
    {
        class ItemData
        {
            public string Name { get; set; }
            public int Supply { get; set; }
            public int Low { get; set; }
            public int High { get; set; }
            public int Reservations { get; set; }
            public int Requested { get; set; }
            public ItemData(string name, int amount)
            {
                Name = name;
                Supply = amount;
            }
            public ItemData(string name, int amount, int low, int high)
            {
                Name = name;
                Supply = amount;
                Low = low;
                High = high;
            }
            public ItemData(string name, int low, int high)
            {
                Name = name;
                Low = low;
                High = high;
            }
            public int getImport()
            {
                return High - Supply;
            }

            public bool isOkayForExport()
            {
                return (Supply > Low && Supply - Low - Reservations> Low) || Low < 0;
            }

            public bool needImport()
            {
                return Supply + Requested < High || High < 0;
            }
        }
    }
}

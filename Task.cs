namespace IngameScript
{
    partial class Program
    {
        class Task
        {
            public string Name { get; set; }

            public int Amount { get; set; }

            public Task(string name, int amount)
            {
                Name = name;
                Amount = amount;
            }
        }
    }
}

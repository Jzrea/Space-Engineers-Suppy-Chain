using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        
        class NetworkClient

        {
            public long ID { get;}
            public string Name { get; }


            public Vector3D Position { get; }
            //public NetworkClient(){}
            public NetworkClient(long id, string name)
            {
                ID = id;
                Name = name;
            }

            public NetworkClient(long id, string name, Vector3D position)
            {
                ID = id;
                Name = name;
                Position = position;
            }
        }
    }
}

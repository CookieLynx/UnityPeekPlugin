using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityPeekPlugin
{
    [Serializable]
    class Helpers
    {
        //Helper class
        [Serializable]
        public class HierachyStructure
        {
            public string name;
            public string id;
            public HierachyStructure parent;
            public List<HierachyStructure> children = new List<HierachyStructure>();
        }


        public static HierachyStructure DeserializeHierarchy(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return ReadNode(reader);
            }
        }

        private static HierachyStructure ReadNode(BinaryReader reader)
        {
            HierachyStructure node = new Helpers.HierachyStructure
            {
                name = reader.ReadString(),
                id = reader.ReadString(),
                children = new List<Helpers.HierachyStructure>()
            };

            int childCount = reader.ReadInt32(); // Read the number of children
            for (int i = 0; i < childCount; i++)
            {
                var child = ReadNode(reader);
                child.parent = node; // Set parent reference
                node.children.Add(child);
            }

            return node;
        }



    }
}

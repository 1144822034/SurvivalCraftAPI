using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class BlockAllocateData
    {
        public Block Block;
        public ModEntity ModEntity;
        public int Index = 0;
        public bool Allocated = false;
        public bool StaticBlockIndex = false;
    }
}

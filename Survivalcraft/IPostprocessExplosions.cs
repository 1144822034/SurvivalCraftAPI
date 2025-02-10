using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;

namespace Game
{
    public interface IPostprocessExplosions
    {
        public void OnExplosion(SubsystemExplosions subsystemExplosions, ref Vector3 impulse, ref float damage, out bool skipVanilla);
    }
}

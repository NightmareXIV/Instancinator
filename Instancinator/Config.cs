using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instancinator
{
    [Serializable]
    class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public int KeyCode = 0x60;
    }
}

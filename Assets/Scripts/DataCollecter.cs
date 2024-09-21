using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public class DataCollecter
    {
        List<int> collectedFps = new List<int>();
        List<float> collectedMemory = new List<float>();
        List<int> DrawCall = new List<int>();
        List<int> SetPassCall = new List<int>();
        List<int>OverDraw = new List<int>();

    }
}

using UnityEngine;

namespace Sensor.Visualizer
{
    public struct PointXYZRGB
    {
        public Vector3 Position;    // World Space
        public uint ColorRGB;       // Packed RGBA8
    }
}

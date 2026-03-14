using System.Collections.Generic;
using Sensor.Lidar._2D;
using UnityEngine;
using UnitySensors.DataType.Sensor.PointCloud;

namespace Sensor.Visualizer
{
    public class SLAMGeometryMapVisualizer : HDRPPointCloudVisualizerProcedural
    {
        private ScanRaycastSensor _scanRaycastSensor;
        private bool _show;
        
        private HashSet<long> _occupiedVoxels = new();
        private List<PointXYZRGB> _points = new();

        [Header("Geometry Settings")]
        [SerializeField] private float _voxelSize = .1f;
        [SerializeField] private int _heightLayers = 10;
        [SerializeField] private float _heightStep = 0.5f;
        
        public void Initialize(ScanRaycastSensor scanRaycastSensor)
        {
            _scanRaycastSensor = scanRaycastSensor;
            _scanRaycastSensor.onSensorUpdated += Visualize;
        }

        public void Show(bool show) => _show = show;

        private long MakeVoxelKey(int vx, int vz)
        {
            return ((long)vx << 32) | (uint)vz;
        }

        private void Visualize()
        {
            if (!_show) return;

            /*
            var addedPoints = _scanRaycastSensor.GetHitPoints();
            for (int i = 0; i < addedPoints.Length; i++)
            {
                var p = addedPoints[i];

                // voxelize
                var vx = Mathf.FloorToInt(p.x / _voxelSize);
                var vz = Mathf.FloorToInt(p.z / _voxelSize);
                var key = MakeVoxelKey(vx, vz);

                if (!_occupiedVoxels.Add(key))
                    continue;

                // base voxel center
                var baseX = (vx + 0.5f) * _voxelSize;
                var baseZ = (vz + 0.5f) * _voxelSize;

                // add height layers
                for (var h = 0; h < _heightLayers; h++)
                {
                    var y = h * _heightStep;

                    _points.Add(new PointXYZRGB
                    {
                        Position = new Vector3(baseX, y, baseZ),
                        ColorRGB = PackColor(255, 255, 255)
                    });
                }
            }
            
            UpdatePointCloud(_points.ToArray(), _points.Count);*/
        }
        
        public static uint PackColor(byte r, byte g, byte b, byte a = 255)
        {
            return ((uint)r << 24) |
                   ((uint)g << 16) |
                   ((uint)b << 8)  |
                   a;
        }
    }
}

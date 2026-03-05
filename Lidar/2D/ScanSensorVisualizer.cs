using UnityEngine;
using UnitySensors.Attribute;

namespace Sensor.Lidar._2D
{
    [RequireComponent(typeof(ScanRaycastSensor))]
    public class ScanSensorVisualizer : MonoBehaviour
    {
        [SerializeField, ReadOnly]
        private ScanRaycastSensor _sensor;
        
        private LineRenderer[] _lineRenderers;
        private int _lineSampleCount;
        private int _lineRendererOffset = 2;

        private void Start()
        {
            _sensor = GetComponent<ScanRaycastSensor>();
            _sensor.onSensorUpdated += Visualize;
            
            _lineSampleCount = _sensor.Rays.Length / _lineRendererOffset;
            _lineRenderers = new LineRenderer[_lineSampleCount];
            for (int i = 0; i < _lineSampleCount; i++)
            {
                var obj = new GameObject($"LineRenderer-[{i}]", typeof(LineRenderer));
                obj.transform.SetParent(transform);
                
                var lr = obj.GetComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.startWidth = 0.01f;
                lr.endWidth = 0.01f;
                lr.material = new Material(Shader.Find("Sprites/Default"));

                _lineRenderers[i] = lr;
            }
        }

        private void Visualize()
        {
            var rayDatas = _sensor.Rays;
            for (var index = 0; index < _lineRenderers.Length; index++)
            {
                var lineRenderer = _lineRenderers[index];
                var data = rayDatas[0 + index * _lineRendererOffset];
                var endPosition = data.Origin + data.Direction * data.Distance;
                lineRenderer.SetPosition(0, data.Origin);
                lineRenderer.SetPosition(1, endPosition);
            }
        }

        private void OnDestroy()
        {
            if (_sensor != null)
                _sensor.onSensorUpdated -= Visualize;
        }
    }
}
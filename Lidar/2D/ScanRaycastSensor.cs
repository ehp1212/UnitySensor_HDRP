using System;
using System.Utility;
using ROS2;
using sensor_msgs.msg;
using std_msgs.msg;
using UnityEngine;
using UnitySensors.Sensor;

namespace Sensor.Lidar._2D
{
    public struct RayData
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Distance;

        public void Set(Vector3 origin, Vector3 direction, float distance)
        {
            Origin = origin;
            Direction = direction;
            Distance = distance;
        }
    }
    
    public class ScanRaycastSensor : UnitySensor
    {
        [SerializeField] private string _nodeName = "unity_lidar_2d";
        [SerializeField] private string _frameId = "lidar_link";
        [SerializeField] private string _topicName = "/scan";
        
        public float rangeMin = 0.1f;
        public float rangeMax = 10f;
        public int numBeams = 360;

        private ROS2System _ros2System;
        private ROS2Node _node;
        private IPublisher<LaserScan> scanPublisher;
        private float angleIncrement;
        
        private LaserScan _msg;
        
        private float[] _rangeArray;
        private RayData[] _rays;
        public RayData[] Rays => _rays;
        
        protected override void Init()
        {
            _rangeArray = new float[numBeams];
            
            _msg = new LaserScan();
            _msg.Header = new Header();
            _msg.Header.Frame_id = _frameId;
            _msg.Angle_min = -Mathf.PI;
            _msg.Angle_max = Mathf.PI;
            _msg.Range_min = rangeMin;
            _msg.Range_max = rangeMax;
            _msg.Ranges = _rangeArray;
            
            _rays = new RayData[numBeams];
            for (int i = 0; i < _rays.Length; i++)
            {
                _rays[i] = new RayData();
            }
            
            _ros2System = ROS2System.Instance;
            Initialize();
        }

        private void Initialize()
        {
            _node = _ros2System.CreateNode(_nodeName);
            scanPublisher = _node.CreatePublisher<LaserScan>(_topicName);

            angleIncrement = 2f * Mathf.PI / numBeams;
        }

        protected override void UpdateSensor()
        {
            PublishScan();
            onSensorUpdated?.Invoke();
        }

        protected override void OnSensorDestroy()
        {
            _ros2System.OnInitialize.RemoveListener(Initialize);
        }
        
        void PublishScan()
        {
            for (int i = 0; i < numBeams; i++)
            {
                var angle = _msg.Angle_min + i * angleIncrement;
                _msg.Angle_increment = angleIncrement;
                
                var direction = TransformUtility.AngleToUnityDirection(angle);
                var ray = new Ray(transform.position, transform.rotation * direction);
                if (Physics.Raycast(ray, out var hit, rangeMax))
                {
                    _msg.Ranges[i] = hit.distance;
                }
                else
                {
                    _msg.Ranges[i] = float.PositiveInfinity;
                }
                
                // Store ray data
                _rays[i].Set(ray.origin, ray.direction, _msg.Ranges[i] > rangeMax ? rangeMax : _msg.Ranges[i]);
            }

            
            UpdateTimeStamp(ref _msg);
            scanPublisher.Publish(_msg);
        }

        private void UpdateTimeStamp(ref LaserScan laserScan)
        {
            var clockMsg = new rosgraph_msgs.msg.Clock();
            _node.clock.UpdateClockMessage(ref clockMsg);
            
            laserScan.UpdateHeaderTime(clockMsg.Clock_.Sec, clockMsg.Clock_.Nanosec);
        }
    }
}

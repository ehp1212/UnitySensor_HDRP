using UnityEngine;
using UnitySensors.Attribute;
using UnitySensors.Interface.Sensor;
using UnitySensors.Visualization;

namespace Script.IMU
{
    /// <summary>
    /// Visualize class for IMU
    /// </summary>
    [RequireComponent(typeof(IMUPhysicsSensor))]
    public class IMUVisualizer : Visualizer
    {
        [SerializeField, Interface(typeof(IImuDataInterface))]
        private Object _source;
        
        [Header("Visual Elements")]
        [SerializeField] private Transform orientationFrame;
        [SerializeField] private Transform gyroArrow;
        [SerializeField] private Transform accelArrow;

        [SerializeField] private float gyroScale = 0.5f;
        [SerializeField] private float accelScale = 0.2f;
        
        private IMUPhysicsSensor _imuPhysicsSensor;
        private IImuDataInterface _imuDataInterface;

        private void Awake()
        {
            _imuPhysicsSensor = GetComponent<IMUPhysicsSensor>();
            _imuDataInterface = _source as IImuDataInterface;
            if (_imuDataInterface == null)
                Debug.LogError($"{nameof(IMUVisualizer)} requires a {nameof(_source)}.");
        }
        
        private void OnEnable()
        {
            _imuPhysicsSensor.onSensorUpdated += Visualize;
        }

        private void OnDisable()
        {
            _imuPhysicsSensor.onSensorUpdated -= Visualize;
        }

        protected override void Visualize()
        {
            // Orientation
            orientationFrame.rotation = _imuPhysicsSensor.rotation;

            // Angular Velocity
            var omega = _imuPhysicsSensor.angularVelocity;
            if (omega.magnitude > 0.0001f)
            {
                gyroArrow.localRotation =
                    Quaternion.LookRotation(omega.normalized);
                gyroArrow.localScale =
                    new Vector3(1f, 1f, omega.magnitude * gyroScale);
            }

            // 3Acceleration
            var acc = _imuPhysicsSensor.acceleration;
            if (acc.magnitude > 0.0001f)
            {
                accelArrow.localRotation =
                    Quaternion.LookRotation(acc.normalized);
                accelArrow.localScale =
                    new Vector3(1f, 1f, acc.magnitude * accelScale);
            }
        }
    }
}
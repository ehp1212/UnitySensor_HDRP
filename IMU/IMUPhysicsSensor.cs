using Unity.Collections;
using UnityEngine;
using UnitySensors.Interface.Sensor;
using Random = UnityEngine.Random;

namespace Script.IMU
{
    /// <summary>
    /// Imu sensor using physics with rigidbody
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class IMUPhysicsSensor : UnityPhysicsSensor, IImuDataInterface
    {
        [SerializeField] private float accelNoiseStd = 0.02f;  // m/s^2
        [SerializeField] private float gyroNoiseStd  = 0.005f; // rad/s

        [SerializeField] private float accelBiasStd = 0.05f;
        [SerializeField] private float gyroBiasStd  = 0.01f;

        [SerializeField, ReadOnly]
        private Vector3 _acceleration; // linear_acceleration
        [SerializeField, ReadOnly]
        private Quaternion _rotation; // angular_velocity
        [SerializeField, ReadOnly]
        private Vector3 _angularVelocity; // orientation
        
        private Vector3 _accelBias;
        private Vector3 _gyroBias;
        
        private Transform _transform;
        private Rigidbody _rigidbody;
        
        private Vector3 _velocity_tmp;
        private Vector3 _acceleration_tmp;
        private Quaternion _rotation_tmp;
        private Vector3 _angularVelocity_tmp;

        private Vector3 _velocity_last;

        public Vector3 acceleration { get => _acceleration; }
        public Quaternion rotation { get => _rotation; }
        public Vector3 angularVelocity { get => _angularVelocity; }

        public Vector3 localAcceleration => _acceleration;

        protected override void Init()
        {
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();
            
            // Initial bias
            _accelBias = GaussianNoiseVector(accelBiasStd);
            _gyroBias  = GaussianNoiseVector(gyroBiasStd);
            
            _velocity_last = _rigidbody.velocity;
        }

        protected override void FixedUpdate()
        {
            ComputePhysicsMeasurement();
            base.FixedUpdate();
        }

        private void ComputePhysicsMeasurement()
        {
            var dt = Time.fixedDeltaTime;

            _velocity_tmp = _rigidbody.velocity;

            // World acceleration 
            var accel_world = (_velocity_tmp - _velocity_last) / dt;

            // Remove gravity
            accel_world -= Physics.gravity;

            // Convert to local
            var accel_local = _transform.InverseTransformDirection(accel_world);
            
            // Angular velocity (local)
            var gyro_local =
                _transform.InverseTransformDirection(_rigidbody.angularVelocity);
            
            // Noise
            accel_local += _accelBias;
            accel_local += GaussianNoiseVector(accelNoiseStd);

            gyro_local += _gyroBias;
            gyro_local += GaussianNoiseVector(gyroNoiseStd);

            _acceleration_tmp = accel_local;
            _angularVelocity_tmp = gyro_local;
            
            // Convert to local
            _rotation_tmp = _rigidbody.rotation;
            _velocity_last = _velocity_tmp;
        }

        protected override void UpdateSensor()
        {
            _acceleration = _acceleration_tmp;

            _rotation = _rotation_tmp;
            _angularVelocity = _angularVelocity_tmp;
			
            if (onSensorUpdated != null)
                onSensorUpdated.Invoke();
        }

        protected override void OnSensorDestroy()
        {
        }
        
        private float GaussianNoise(float mean, float stdDev)
        {
            float u1 = 1.0f - Random.value;
            float u2 = 1.0f - Random.value;
            float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                                  Mathf.Sin(2.0f * Mathf.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        private Vector3 GaussianNoiseVector(float stdDev)
        {
            return new Vector3(
                GaussianNoise(0f, stdDev),
                GaussianNoise(0f, stdDev),
                GaussianNoise(0f, stdDev)
            );
        }
    }
}

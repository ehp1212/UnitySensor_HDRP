using UnityEngine;
using UnitySensors.Interface.Std;

namespace Sensor
{
    public abstract class UnityPhysicsSensor : MonoBehaviour, ITimeInterface
    {
        [SerializeField]
        private float _frequency = 10.0f;

        private float _time;
        private float _dt;

        public delegate void OnSensorUpdated();
        public OnSensorUpdated onSensorUpdated;


        private float _frequency_inv;

        public float dt { get => _frequency_inv; }
        public float time { get => _time; }

        private void Awake()
        {
            if (_frequency <= 0f)
                Debug.LogError("Sensor frequency must be positive.");
            
            _dt = 0.0f;
            _frequency_inv = 1.0f / _frequency;

            Init();
        }

        protected virtual void FixedUpdate()
        {
            _dt += Time.fixedDeltaTime;

            while (_dt >= _frequency_inv)
            {
                _time += _frequency_inv;

                UpdateSensor();

                _dt -= _frequency_inv;
            }
        }

        private void OnDestroy()
        {
            onSensorUpdated = null;
            OnSensorDestroy();
        }

        protected abstract void Init();
        protected abstract void UpdateSensor();
        protected abstract void OnSensorDestroy();
    }
}
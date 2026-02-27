using System;
using Unity.VisualScripting;
using UnityEngine;
using UnitySensors.Attribute;
using UnitySensors.DataType.Sensor.PointCloud;
using UnitySensors.Interface.Sensor;
using UnitySensors.Interface.Sensor.PointCloud;
using UnitySensors.Sensor;
using UnitySensors.Utils.PointCloud;
using UnitySensors.Visualization;
using Object = UnityEngine.Object;

namespace Script
{
    public class HDRPPointCloudVisualizer : HDRPPointCloudVisualizer<PointXYZI>
    {
        [SerializeField, Interface(typeof(IPointCloudInterface<PointXYZI>))]
        private Object _source;

        private float _maxDistance = 5;
        
        protected override void Start()
        {
            if (_source is UnitySensor)
            {
                (_source as UnitySensor).onSensorUpdated += Visualize;
            }
            base.SetSource(_source as IPointCloudInterface<PointXYZI>);
            base.Start();
            
            Material.SetFloat("_MaxDistance", _maxDistance);
        }
    }
    
    public class HDRPPointCloudVisualizer<T> : Visualizer where T : struct, IPointInterface
    {
        private static readonly int LocalToWorldMatrix = Shader.PropertyToID("LocalToWorldMatrix");
        private static readonly int PointsBuffer = Shader.PropertyToID("PointsBuffer");
        private static readonly int SensorPosition = Shader.PropertyToID("_SensorPosition");
        
        private IPointCloudInterface<T> _sourceInterface;
        private Transform _transform;

        [SerializeField] private Shader _shader;
        
        private Mesh _mesh;
        private Material _mat;
        private ComputeBuffer _pointsBuffer;
        private ComputeBuffer _argsBuffer;

        private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
        private int _cachedPointsCount = -1;
        private int _bufferSize;

        protected Material Material => _mat;
        public void SetSource(IPointCloudInterface<T> sourceInterface)
        {
            _sourceInterface = sourceInterface;
        }

        protected virtual void Start()
        {
            if (_shader == null)
            {
                Debug.LogError("Shader is not assigned!");
                enabled = false;
                return;
            }
            
            _mat = new Material(_shader);
            
            _transform = this.transform;
            _bufferSize = PointUtilities.pointDataSizes[typeof(T)];
            
            _mesh = new Mesh();
            _mesh.vertices = new Vector3[1] { Vector3.zero };
            _mesh.SetIndices(new int[1] { 0 }, MeshTopology.Points, 0);
            _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            
            UpdateBuffers();
        }

        protected override void Visualize()
        {
            if (_sourceInterface.pointsNum != _cachedPointsCount) UpdateBuffers();
            _mat.SetMatrix(LocalToWorldMatrix, _transform.localToWorldMatrix);
            _pointsBuffer.SetData(_sourceInterface.pointCloud.points);
        }

        private void Update()
        {
            UpdateSensorPosition();
            
            Graphics.DrawMeshInstancedIndirect(_mesh, 0, _mat, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), _argsBuffer);
        }

        private void UpdateSensorPosition()
        {
            Material.SetVector(SensorPosition, transform.position);
        }

        private void UpdateBuffers()
        {
            if (_pointsBuffer != null) _pointsBuffer.Release();
            _pointsBuffer = new ComputeBuffer(_sourceInterface.pointsNum, _bufferSize);
            _pointsBuffer.SetData(_sourceInterface.pointCloud.points);
            _mat.SetBuffer(PointsBuffer, _pointsBuffer);

            uint numIndices = (_mesh != null) ? (uint)_mesh.GetIndexCount(0) : 0;
            _args[0] = numIndices;
            _args[1] = (uint)_sourceInterface.pointsNum;
            _argsBuffer.SetData(_args);

            _cachedPointsCount = _sourceInterface.pointsNum;
        }

        // TODO: Replace this with editor version
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(
                new Rect(0, 0, Screen.width, Screen.height),
                _sourceInterface.GetType().Name,
                style
            );
        }

        private void OnDisable()
        {
            if (_pointsBuffer != null) _pointsBuffer.Release();
            _pointsBuffer = null;
            if (_argsBuffer != null) _argsBuffer.Release();
            _argsBuffer = null;
        }
    }
}

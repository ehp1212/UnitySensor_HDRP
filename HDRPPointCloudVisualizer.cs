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
        
        protected override void Start()
        {
            if (_source is UnitySensor)
            {
                (_source as UnitySensor).onSensorUpdated += Visualize;
            }
            base.SetSource(_source as IPointCloudInterface<PointXYZI>);
            base.Start();
        }
    }
    
    public class HDRPPointCloudVisualizer<T> : Visualizer where T : struct, IPointInterface
    {
        private static readonly int LocalToWorldMatrix = Shader.PropertyToID("LocalToWorldMatrix");
        private static readonly int PointsBuffer = Shader.PropertyToID("PointsBuffer");
        private static readonly int SensorPosition = Shader.PropertyToID("_SensorPosition");
        
        private static readonly int CameraRightWs = Shader.PropertyToID("_CameraRightWS");
        private static readonly int CameraUpWs = Shader.PropertyToID("_CameraUpWS");
        
        private static readonly int MaxDistance = Shader.PropertyToID("_MaxDistance");
        private static readonly int MinRedDistance = Shader.PropertyToID("_MinRedDistance");
        private static readonly int PointSize = Shader.PropertyToID("_PointSize");

        [SerializeField] private Shader _shader;
        [SerializeField] private float _maxDistance = 30;
        [SerializeField] private float _pointSize = 0.1f;
        
        private IPointCloudInterface<T> _sourceInterface;
        private Transform _transform;
        
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
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 포인트 많으면 안전

// 카메라 빌보드가 아니라도 “쿼드 한 장”의 로컬 정점(원점 기준)을 만들어 둠
// 실제 크기/방향은 셰이더에서 조절하는 게 제일 깔끔함(=pointSize)
            _mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f), // 0
                new Vector3( 0.5f, -0.5f, 0f), // 1
                new Vector3( 0.5f,  0.5f, 0f), // 2
                new Vector3(-0.5f,  0.5f, 0f), // 3
            };

            _mesh.uv = new Vector2[]
            {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1),
            };

// 삼각형 2개(인덱스 6개)
            _mesh.SetIndices(
                new int[] { 0, 1, 2, 0, 2, 3 },
                MeshTopology.Triangles,
                0,
                true
            );

            _mesh.RecalculateBounds();
            
            
            _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            
            UpdateBuffers();
                        
            Material.SetFloat(MaxDistance, _maxDistance);
            Material.SetFloat(MinRedDistance, 1);
            Material.SetFloat(PointSize, _pointSize);
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
            UpdateCamera();

            var range = _maxDistance + 5f;
            var b = new Bounds(transform.position, Vector3.one * (range * 2f));
            Graphics.DrawMeshInstancedIndirect(_mesh, 0, _mat, b, _argsBuffer);
        }

        private void UpdateCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Material.SetVector(CameraRightWs, cam.transform.right);
            Material.SetVector(CameraUpWs, cam.transform.up);
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
            _args[0] = numIndices;                 // 이제 6이 됨
            _args[1] = (uint)_sourceInterface.pointsNum; // 인스턴스 개수 = 포인트 개수
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

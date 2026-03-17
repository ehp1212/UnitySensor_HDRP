using UnityEngine;
using UnityEngine.Rendering;
using UnitySensors.Interface.Sensor;
using UnitySensors.Interface.Sensor.PointCloud;
using UnitySensors.Utils.PointCloud;

namespace Sensor.Visualizer
{
    /// <summary>
    /// Visualizer class using DrawMeshInstancedIndirect
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class HDRPPointCloudVisualizerIndirect<T> : UnitySensors.Visualization.Visualizer where T : struct, IPointInterface
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
        
        [SerializeField] private LayerMask _layerMask;
        
        private IPointCloudInterface<T> _sourceInterface;
        private Transform _transform;
        
        private Mesh _mesh;
        private Material _mat;
        private ComputeBuffer _pointsBuffer;
        private ComputeBuffer _argsBuffer;

        private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
        private int _cachedPointsCount = -1;
        private int _bufferSize;
        private Camera _camera;

        protected Material Material => _mat;
        public void SetSource(IPointCloudInterface<T> sourceInterface)
        {
            _sourceInterface = sourceInterface;
        }

        protected virtual void Start()
        {
            _camera = Camera.main;
            if (_shader == null)
            {
                Debug.LogError("Shader is not assigned!");
                enabled = false;
                return;
            }
            
            _mat = new Material(_shader);
            _layerMask = LayerMask.NameToLayer("Debug");
            _transform = this.transform;
            _bufferSize = PointUtilities.pointDataSizes[typeof(T)];

            CreateQuad();
            _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            
            UpdateBuffers();
                        
            Material.SetFloat(MaxDistance, _maxDistance);
            Material.SetFloat(MinRedDistance, 1);
            Material.SetFloat(PointSize, _pointSize);
        }

        private void CreateQuad()
        {
            _mesh = new Mesh();
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 

            _mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), // 0
                new Vector3( 0.5f, -0.5f, 0f), // 1
                new Vector3( 0.5f,  0.5f, 0f), // 2
                new Vector3(-0.5f,  0.5f, 0f), // 3
            };

            _mesh.uv = new[]
            {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1),
            };

            _mesh.SetIndices(
                new int[] { 0, 1, 2, 0, 2, 3 },
                MeshTopology.Triangles,
                0,
                true
            );

            _mesh.RecalculateBounds();
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
            Graphics.DrawMeshInstancedIndirect(_mesh, 0, _mat, b, _argsBuffer, 0, null, ShadowCastingMode.Off, false, _layerMask);
        }

        private void UpdateCamera()
        {
            if (_camera == null) return;
            Material.SetVector(CameraRightWs, _camera.transform.right);
            Material.SetVector(CameraUpWs, _camera.transform.up);
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
            if (_camera == null) return;

            var screenPos = _camera.WorldToScreenPoint(transform.position);
            if (screenPos.z < 0)
                return;
            
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter
            };
            
            var x = screenPos.x;
            var y = Screen.height - screenPos.y;

            GUI.Label(
                new Rect(x - 100, y - 15, 200, 100),
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
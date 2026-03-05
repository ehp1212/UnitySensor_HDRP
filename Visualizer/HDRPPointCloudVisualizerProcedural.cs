using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sensor.Visualizer
{
    /// <summary>
    /// Renderer to visualize data on GPU from ROS2
    /// </summary>
    public abstract class HDRPPointCloudVisualizerProcedural : MonoBehaviour
    {
        private static readonly int PointsId = Shader.PropertyToID("_Points");
        private static readonly int PointCountId = Shader.PropertyToID("_PointCount");
        private static readonly int PointSizeId = Shader.PropertyToID("_PointSize");
        
        private static readonly int CameraRightWs = Shader.PropertyToID("_CameraRightWS");
        private static readonly int CameraUpWs = Shader.PropertyToID("_CameraUpWS");
        
        [SerializeField] private Shader _shader;
        [SerializeField] private float _pointSize = 0.2f;
        
        [Header("Capacity")]
        [SerializeField] private int _maxPointCount = 200_000;
        
        private Mesh _mesh;
        private Material _mat;
        private ComputeBuffer _pointBuffer;
        private int _pointCount;
        private Bounds _bounds;
        private Camera _camera;
        
        protected virtual void Awake()
        {
            CreateQuad();
            if (_shader == null)
            {
                Debug.LogError("Shader is not assigned!");
                enabled = false;
                return;
            }
            _camera = Camera.main;
            _mat = new Material(_shader);
            _bounds = new(transform.position, Vector3.one * 500f); // Avoid culling by making big bounds
            Allocate(_maxPointCount);
        }

        protected virtual void OnDestroy()
        {
            Release();
        }

        protected virtual void Allocate(int count)
        {
            Release();
            var stride = UnsafeUtility.SizeOf<PointXYZRGB>();
            _pointBuffer = new ComputeBuffer(count, stride, ComputeBufferType.Structured);
            
            _mat.SetBuffer(PointsId, _pointBuffer);
            _mat.SetFloat(PointSizeId, _pointSize);
        }

        protected virtual void Release()
        {
            _pointBuffer?.Release();
            _pointBuffer = null;
            _pointCount = 0;
        }

        public virtual void UpdatePointCloud(PointXYZRGB[] points, int count)
        {
            if (_pointBuffer == null || count < 0) return;

            _pointCount = Mathf.Min(count, Mathf.Min(points.Length, _maxPointCount));
            _pointBuffer.SetData(points, 0, 0, _pointCount);
            
            _mat.SetInt(PointCountId, _pointCount);
        }
        
        protected virtual void Update()
        {
            if (_pointCount <= 0 || _mat == null || _mesh == null) return;
            
            _mat.SetMatrix("_LocalToWorldMatrix", transform.localToWorldMatrix);
            
            _bounds.center = transform.position;
            Graphics.DrawMeshInstancedProcedural(
                _mesh, 0, _mat, _bounds, _pointCount,
                null, ShadowCastingMode.Off, false, gameObject.layer
            );
        }

        private void LateUpdate()
        {
            _mat.SetVector(CameraRightWs, _camera.transform.right);
            _mat.SetVector(CameraUpWs, _camera.transform.up);
        }

        private void CreateQuad()
        {
            _mesh = new Mesh();
            _mesh.indexFormat = IndexFormat.UInt32; 

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

        public void SetFloatMaterial(int id, float value)
        {
            _mat.SetFloat(id, value);
        }
    }
}

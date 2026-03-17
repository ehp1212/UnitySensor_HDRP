using ROS2;
using RosMessageTypes.Sensor;
using sensor_msgs.msg;
using std_msgs.msg;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sensor.Cam.Test
{
    public class HDRPDepthCameraPublisher : Communication.Publisher<Image>
    {
        [SerializeField] private RenderTexture _depthRenderTexture;
        [SerializeField] private ComputeShader _depthToPointShader;
        
        [SerializeField] protected float _minRange = 0.05f;
        [SerializeField] protected float _maxRange = 100.0f;
        
        [Header("Point Cloud")]
        [SerializeField] private bool _publishPointCloud = true;
        [SerializeField] private string _pcTopicName = "/camera/depth/points";

        private IPublisher<PointCloud2> _pointCloudPublisher;
        private Camera _camera;
        private ComputeBuffer _pointBuffer;
        private int _kernel;

        protected override void Awake()
        {
            base.Awake();
            
            if (_depthRenderTexture == null)
            {
                Debug.LogError($"Cannot visulzie depth camera without Depth Render Texture.");
                return;
            }
            
            _camera = GetComponent<Camera>();
            _camera.nearClipPlane = _minRange;
            _camera.farClipPlane = _maxRange;
            
            var width = _depthRenderTexture.width;
            var height = _depthRenderTexture.height;
            
            _pointBuffer = new ComputeBuffer(_depthRenderTexture.width * _depthRenderTexture.height, sizeof(float) * 3);
            _kernel = _depthToPointShader.FindKernel("CSMain");
            
            _depthToPointShader.SetTexture(_kernel, "depthTexture", _depthRenderTexture);
            _depthToPointShader.SetBuffer(_kernel, "points", _pointBuffer);
            
            _depthToPointShader.SetInt("width", width);
            _depthToPointShader.SetInt("height", height);
            
            SetupIntrinsics();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // Point Cloud
            if (!_publishPointCloud) return;
            
            _pointCloudPublisher = Node.CreatePublisher<PointCloud2>(_pcTopicName);
        }

        private void SetupIntrinsics()
        {
            var width = _depthRenderTexture.width;
            var height = _depthRenderTexture.height;
            
            float fy = height / (2.0f * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f));
            float fx = fy;

            float cx = width * 0.5f;
            float cy = height * 0.5f;

            _depthToPointShader.SetFloat("fx", fx);
            _depthToPointShader.SetFloat("fy", fy);
            _depthToPointShader.SetFloat("cx", cx);
            _depthToPointShader.SetFloat("cy", cy);
        }


        protected override void Publish()
        {
            var width = _depthRenderTexture.width;
            var height = _depthRenderTexture.height;

            // Point could
            if (!_publishPointCloud) return;
            
            var tx = Mathf.CeilToInt(width / 8.0f);
            var ty = Mathf.CeilToInt(height / 8.0f);
            
            _depthToPointShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            _depthToPointShader.Dispatch(_kernel, tx, ty, 1);
            AsyncGPUReadback.Request(_pointBuffer, OInPointsReady);
        }

        private void OInPointsReady(AsyncGPUReadbackRequest req)
        {
            if (req.hasError)
                return;
            
            var raw = req.GetData<Vector3>();
            var rawBytes = raw.Reinterpret<byte>(sizeof(float) * 3);
            var msg = new PointCloud2();

            msg.Header = new Header();
            msg.Header.Frame_id = "lidar_link";
            // Time

            msg.Width = (uint)_depthRenderTexture.width;
            msg.Height = (uint)_depthRenderTexture.height;
            
            msg.Is_bigendian = false;
            msg.Is_dense = true;

            msg.Point_step = 12;
            msg.Row_step = msg.Point_step * msg.Width;
            
            msg.Fields = new PointField[3];
            msg.Fields[0] = CreateField("x", 0);
            msg.Fields[1] = CreateField("y", 4);
            msg.Fields[2] = CreateField("z", 8);
            msg.Data = rawBytes.ToArray();
            
            _pointCloudPublisher.Publish(msg);
        }
        

        private void OnDestroy()
        {
            if (_pointBuffer != null)
                _pointBuffer.Release();
        }
        
        private PointField CreateField(string name, uint offset)
        {
            var f = new PointField();
            f.Name = name;                 // PointField도 DLL마다 Name/name 차이 있을 수 있음 (아래 주의 참고)
            f.Offset = offset;
            f.Datatype = PointField.FLOAT32;
            f.Count = 1;
            return f;
        }
    }
}

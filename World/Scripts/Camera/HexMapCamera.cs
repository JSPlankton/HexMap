using UnityEngine;

namespace JS.HexMap
{
    public class HexMapCamera : MonoBehaviour
    {
        static HexMapCamera instance;
        
        public float moveSpeedMinZoom, moveSpeedMaxZoom;
        public float rotationSpeed;
        //角度
        public float swivelMinZoom, swivelMaxZoom;
        //远近
        public float stickMinZoom, stickMaxZoom;
        public HexGrid grid;

        private Transform swivel, stick;
        private float zoom = 1f;
        private float rotationAngle;

        private void Awake()
        {
            swivel = transform.GetChild(0);
            stick = swivel.GetChild(0);
        }
        
        void OnEnable () {
            instance = this;
            // ValidatePosition();
        }

        void Update()
        {
            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (zoomDelta != 0f)
            {
                AdjustZoom(zoomDelta);
            }
            
            float rotationDelta = Input.GetAxis("Rotation");
            if (rotationDelta != 0f) {
                AdjustRotation(rotationDelta);
            }

            float xDelta = Input.GetAxis("Horizontal");
            float zDelta = Input.GetAxis("Vertical");
            if (xDelta != 0f || zDelta != 0f)
            {
                AdjustPosition(xDelta, zDelta);
            }
        }

        void AdjustZoom(float delta)
        {
            zoom = Mathf.Clamp01(zoom + delta);

            float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
            stick.localPosition = new Vector3(0f, 0f, distance);

            float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
            swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        void AdjustPosition(float xDelta, float zDelta)
        {
            Vector3 direction =
                transform.localRotation *
                new Vector3(xDelta, 0f, zDelta).normalized;
            //阻尼系数
            float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
            float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * damping * Time.deltaTime;

            Vector3 position = transform.localPosition;
            position += direction * distance;
            transform.localPosition =
                grid.Wrapping ? WrapPosition(position) : ClampPosition(position);
        }
        
        Vector3 WrapPosition (Vector3 position) {
            float width = grid.CellCountX * HexMetrics.innerDiameter;
            while (position.x < 0f) {
                position.x += width;
            }
            while (position.x > width) {
                position.x -= width;
            }

            float zMax = (grid.CellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
            position.z = Mathf.Clamp(position.z, 0f, zMax);

            grid.CenterMap(position.x);
            return position;
        }
        
        Vector3 ClampPosition (Vector3 position) 
        {
            float xMax = (grid.CellCountX - 0.5f) * HexMetrics.innerDiameter;
            position.x = Mathf.Clamp(position.x, 0f, xMax);

            float zMax = (grid.CellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
            position.z = Mathf.Clamp(position.z, 0f, zMax);

            return position;
        }
        
        void AdjustRotation (float delta) {
            rotationAngle += delta * rotationSpeed * Time.deltaTime;
            if (rotationAngle < 0f) {
                rotationAngle += 360f;
            }
            else if (rotationAngle >= 360f) {
                rotationAngle -= 360f;
            }
            transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
        }
        
        public static bool Locked {
            set {
                instance.enabled = !value;
            }
        }
        
        public static void ValidatePosition () {
            instance.AdjustPosition(0f, 0f);
        }
    }

}

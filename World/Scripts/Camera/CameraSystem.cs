using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;
    [SerializeField] private bool useEdgeScrollSize = false;
    [SerializeField] private bool useDragPan= false;
    [SerializeField] private float fieldOfViewMax = 50f;
    [SerializeField] private float fieldOfViewMin = 10f;
    [SerializeField] private float followOffsetMax = 50f;
    [SerializeField] private float followOffsetMin = 5f;
    [SerializeField] private float followOffsetMaxY = 50f;
    [SerializeField] private float followOffsetMinY = 10f;
    
    private float moveSpeed = 50f;
    private float rotateSpeed = 100f;
    private float dragPanSpeed = 0.5f;
    private int edgeScrollSize = 20;
    private float targetFieldOfView = 50f;
    private Vector3 followOffset;
    
    private bool dragPanMoveActive = false;
    private Vector2 lastMousePosition = Vector2.zero;
    // Update is called once per frame

    private void Awake()
    {
        followOffset = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
    }

    void Update()
    {
        //移动
        HandleCameraMovement();
        //边缘滚动
        if (useEdgeScrollSize)
        {
            HandleCameraMovementEdgeScrolling();           
        }
        //鼠标滑动
        if (useDragPan)
        {
            HandleCameraMovementDragPan();
        }
        //旋转
        HandleCameraRotation();
        //缩放
        // HandleCameraZoom_FOV();
        // HandleCameraZoom_MoveForward();
        HandleCameraZoom_LowerY();
    }

    private void HandleCameraMovement()
    {
        Vector3 inputDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) inputDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) inputDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) inputDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) inputDir.x = +1f;
        
        Vector3 moveDir = transform.forward * inputDir.z + transform.right * inputDir.x;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    private void HandleCameraMovementEdgeScrolling()
    {
        Vector3 inputDir = Vector3.zero;
        if (Input.mousePosition.x < edgeScrollSize) inputDir.x = -1f;
        if (Input.mousePosition.y < edgeScrollSize) inputDir.z = -1f;
        if (Input.mousePosition.x > Screen.width -  edgeScrollSize) inputDir.x = +1f;
        if (Input.mousePosition.y > Screen.height -  edgeScrollSize) inputDir.z = +1f;

        Vector3 moveDir = transform.forward * inputDir.z + transform.right * inputDir.x;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
    
    private void HandleCameraMovementDragPan()
    {
        Vector3 inputDir = Vector3.zero;
        if (Input.GetMouseButtonDown(0))
        {
            dragPanMoveActive = true;
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            dragPanMoveActive = false;
        }

        if (dragPanMoveActive)
        {
            Vector2 mouseMovementDelta = (Vector2)Input.mousePosition - lastMousePosition;
            inputDir.x = mouseMovementDelta.x * dragPanSpeed * -1f;
            inputDir.z = mouseMovementDelta.y * dragPanSpeed * -1f;
            
            lastMousePosition = Input.mousePosition;
            
            Vector3 moveDir = transform.forward * inputDir.z + transform.right * inputDir.x;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleCameraRotation()
    {
        float rotateDir = 0f;
        if (Input.GetKey(KeyCode.Q)) rotateDir = +1f;
        if (Input.GetKey(KeyCode.E)) rotateDir = -1f;

        transform.eulerAngles += new Vector3(0, rotateDir * rotateSpeed * Time.deltaTime, 0);
    }
    
    private void HandleCameraZoom_FOV()
    {
        if (Input.mouseScrollDelta.y > 0)
        {
            targetFieldOfView -= 5;
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            targetFieldOfView += 5;
        }

        targetFieldOfView = Mathf.Clamp(targetFieldOfView, fieldOfViewMin, fieldOfViewMax);

        _cinemachineVirtualCamera.m_Lens.FieldOfView =
            Mathf.Lerp(_cinemachineVirtualCamera.m_Lens.FieldOfView, targetFieldOfView, Time.deltaTime * 10f);
    }
    
    private void HandleCameraZoom_MoveForward()
    {
        Vector3 zoomDir = followOffset.normalized;
        float zoomAmount = 3f;
        if (Input.mouseScrollDelta.y > 0)
        {
            followOffset -= zoomDir * zoomAmount;
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            followOffset += zoomDir * zoomAmount;
        }

        if (followOffset.magnitude < followOffsetMin)
        {
            followOffset = zoomDir * followOffsetMin;
        }
        if (followOffset.magnitude > followOffsetMax)
        {
            followOffset = zoomDir * followOffsetMax;
        }

        _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset =
        Vector3.Lerp(_cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset,
            followOffset, Time.deltaTime * 10f);
    }

    private void HandleCameraZoom_LowerY()
    {
        float zoomAmount = 3f;
        if (Input.mouseScrollDelta.y > 0)
        {
            followOffset.y -= zoomAmount;
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            followOffset.y += zoomAmount;
        }

        followOffset.y = Mathf.Clamp(followOffset.y, followOffsetMinY, followOffsetMaxY);

        _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset =
            Vector3.Lerp(_cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset,
                followOffset, Time.deltaTime * 10f);
    }
}

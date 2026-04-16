using UnityEngine;

namespace Seb.Helpers
{
    public class DragCam2D : MonoBehaviour
    {
        [SerializeField] float zoomSpeed = 1f;
        [SerializeField] bool zoomToMouse = true;
        [SerializeField] Vector2 zoomRange = new Vector2(0.0001f, 1000);
        [SerializeField] KeyCode zoomInKey = KeyCode.Q;
        [SerializeField] KeyCode zoomOutKey = KeyCode.W;

        Camera cam;
        Vector2 mouseDragScreenPosOld;

        void Start()
        {
            cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            Vector2 mouseScreenPos = Input.mousePosition;
            Vector2 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);

            // Pan
            if (Input.GetMouseButtonDown(2))
            {
                mouseDragScreenPosOld = mouseScreenPos;
            }
            if (Input.GetMouseButton(2))
            {
                Vector2 mouseWorldPosOld = cam.ScreenToWorldPoint(mouseDragScreenPosOld);
                transform.position += (Vector3)(mouseWorldPosOld - mouseWorldPos);
                mouseDragScreenPosOld = mouseScreenPos;
            }
            Vector2 mouseWorldPosAfterPanning = cam.ScreenToWorldPoint(mouseScreenPos);


            // Zoom
            float deltaZoom = -GetScrollInput() * cam.orthographicSize * zoomSpeed * 0.1f;
            if (Input.GetKey(zoomInKey))
            {
                deltaZoom = -1 * cam.orthographicSize * zoomSpeed * 0.01f;

            }
            if (Input.GetKey(zoomOutKey))
            {
                deltaZoom = 1 * cam.orthographicSize * zoomSpeed * 0.01f;

            }
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + deltaZoom, zoomRange.x, zoomRange.y);
            // Adjust cam pos to centre zoom on mouse
            if (zoomToMouse)
            {
                Vector2 mouseWorldPosAfterZoom = cam.ScreenToWorldPoint(mouseScreenPos);
                transform.position += (Vector3)(mouseWorldPosAfterPanning - mouseWorldPosAfterZoom);
            }

        }

        float GetScrollInput()
        {
            float scroll = Input.mouseScrollDelta.y;

            // On windows, scroll has huge values. (bug?)
            if (Mathf.Abs(scroll) >= 120)
            {
                scroll /= 120f;
            }
            return scroll;
        }

    }
}
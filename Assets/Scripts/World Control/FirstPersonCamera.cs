using UnityEngine;

//https://www.codrbook.com/2023/01/how-to-create-first-person-camera.html
public class FirstPersonCamera : MonoBehaviour {
    public float moveSpeed = 5f;
    public float moveScrollSpeed = 3f;
    public float movePanSensitivity = 0.03f;
    public float mouseSensitivity = 2f;

    private float rotationX;
    private float rotationY;
    private bool move = false;
    private Vector3 lastMousePosition;
    private Vector3 lastCamPosition;
    private Vector3 lastRotationFocusPoint;

    public bool LockToDontMove { get; set; }

    void Start() {
        //Cursor.lockState = CursorLockMode.Locked;
        var rot = transform.localRotation.eulerAngles;
        rotationX = rot.x;
        rotationY = rot.y;
    }

    private void Update() {
        if (!LockToDontMove) {
            if (/*Input.GetMouseButtonDown(0) ||*/ Input.GetKeyDown(KeyCode.Escape)) {
                move = !move;
            }
        }
    }

    void LateUpdate() {
        var deltaTime = Time.deltaTime;
        if (Input.GetMouseButtonDown(1))
            lastRotationFocusPoint = transform.position + (transform.position.magnitude) * transform.forward;//VectorPlaneIntersection(transform.position, transform.forward, Vector3.right, Vector3.up);// A point in the plane is Vector.right = 1,0,0, to avoid start to center that is a valid point but... 
        if (move || Input.GetMouseButton(1))
            ApplyRotation();
        if (move)
            ApplyTranslation(deltaTime);
        ApplyScrollTranslation();
        if (Input.GetMouseButtonDown(2)) {
            lastMousePosition = Input.mousePosition;
            lastCamPosition = transform.position;
        }
        if (Input.GetMouseButton(2))
            ApplyMiddleButtonTranslation();
    }

    void ApplyRotation() {
        // rotate our camera
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        rotationY += mouseX;

        //transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);        
        transform.RotateAround(lastRotationFocusPoint, Vector3.up, mouseX);
        transform.RotateAround(lastRotationFocusPoint, transform.right, -mouseY);
    }

    void ApplyTranslation(float deltaTime) {
        // move our camera forward, backward, left and right
        float moveH = Input.GetAxis("Horizontal");
        float moveV = Input.GetAxis("Vertical");
        transform.position += (Vector3.forward * moveV + transform.right * moveH).normalized * moveSpeed * deltaTime;
    }

    void ApplyScrollTranslation() {
        if(IsMouseOverGameWindow)
            transform.position += transform.forward * Input.mouseScrollDelta.y * moveScrollSpeed;
    }

    void ApplyMiddleButtonTranslation() {
        Vector3 mouseDisplacement = Input.mousePosition - lastMousePosition;
        transform.position = lastCamPosition - (transform.up * mouseDisplacement.y + transform.right * mouseDisplacement.x) * Camera.main.WorldToScreenPoint(Vector3.zero).z * movePanSensitivity;
    }

    //https://www.habrador.com/tutorials/math/4-plane-ray-intersection/
    Vector3 VectorPlaneIntersection(Vector3 originVector, Vector3 dirVector, Vector3 pointInPlane, Vector3 normalPlane) {
        //A plane can be defined as:
        //a point representing how far the plane is from the world origin
        Vector3 p_0 = pointInPlane;
        //a normal (defining the orientation of the plane), should be negative if we are firing the ray from above
        Vector3 n = -normalPlane;
        //We are intrerested in calculating a point in this plane called p
        //The vector between p and p0 and the normal is always perpendicular: (p - p_0) . n = 0

        //A ray to point p can be defined as: l_0 + l * t = p, where:
        //the origin of the ray
        Vector3 l_0 = originVector;
        //l is the direction of the ray
        Vector3 l = dirVector;
        //t is the length of the ray, which we can get by combining the above equations:
        //t = ((p_0 - l_0) . n) / (l . n)

        //But there's a chance that the line doesn't intersect with the plane, and we can check this by first
        //calculating the denominator and see if it's not small. 
        //We are also checking that the denominator is positive or we are looking in the opposite direction
        float denominator = Vector3.Dot(l, n);

        if (denominator > 0.00001f) {
            //The distance to the plane
            float t = Vector3.Dot(p_0 - l_0, n) / denominator;

            //Where the ray intersects with a plane
            Vector3 p = l_0 + l * t;
            return p;
        } else {
            //Debug.Log("No intersection");
            return Camera.main.WorldToScreenPoint(Vector3.zero).z * dirVector;
        }
    }

    //https://discussions.unity.com/t/how-can-i-tell-if-the-mouse-is-over-the-game-window/139428/4
    private bool IsMouseOverGameWindow {
        get {
            Vector3 mp = Input.mousePosition;
            return !(0 > mp.x || 0 > mp.y || Screen.width < mp.x || Screen.height < mp.y);
        }
    }
}
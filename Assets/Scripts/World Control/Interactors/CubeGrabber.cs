using UnityEngine;

public class CubeGrabber {

    public bool IsDragging => _isDragging;

    private string _targetTag;
    private Transform _visualFeedback;  

    private bool _isDragging = false;
    private Vector3 _originalPosition;
    private Vector3 _offset;
    private GameObject _draggedCube;
    private Plane _dragPlane;

    public CubeGrabber(string targetTag, Transform visualFeedback) {
        _targetTag = targetTag;
        this._visualFeedback = visualFeedback;
    }


    public GameObject StartAction() {
        GameObject caughtCube = null;
        if (!_isDragging) {
            // Check if we are clicking on a cube
            Vector3 normal;
            _draggedCube = GetCubeUnderMouse(out normal);
            if (_draggedCube != null) {
                _originalPosition = _draggedCube.transform.position;
                _dragPlane = new Plane(normal, _originalPosition);
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                float distance;
                if (_dragPlane.Raycast(ray, out distance)) {
                    _offset = _originalPosition - ray.GetPoint(distance);
                }
                _isDragging = true;
                if (_visualFeedback != null) {
                    _visualFeedback.gameObject.SetActive(true);
                    _visualFeedback.position = _originalPosition + normal * _draggedCube.transform.localScale.x * 0.501f;
                    _visualFeedback.rotation = Quaternion.LookRotation(normal);
                }
                caughtCube = _draggedCube;
            }
        }
        return caughtCube;
    }

    public GameObject EndAction() {
        GameObject caughtCube = null;
        if (_isDragging) {
            _isDragging = false;
            caughtCube = _draggedCube;
            _draggedCube = null;
            if (_visualFeedback != null) {
                _visualFeedback.gameObject.SetActive(false);
            }
        }
        return caughtCube;
    }

    public void UpdateAction() {
        if (_isDragging && _draggedCube != null) {
            // Update the cube's position based on the mouse movement on the face plane
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float distance;
            if (_dragPlane.Raycast(ray, out distance)) {
                Vector3 targetPosition = ray.GetPoint(distance) + _offset;
                _draggedCube.transform.position = targetPosition;
                if (_visualFeedback != null) {
                    _visualFeedback.position = targetPosition + _dragPlane.normal * _draggedCube.transform.localScale.x * 0.501f;
                }
            }
        }
    }


    private GameObject GetCubeUnderMouse(out Vector3 normal) {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        foreach (RaycastHit hit in hits) {
            if (hit.collider.CompareTag(_targetTag)) {
                normal = hit.normal;
                return hit.collider.gameObject;
            }
        }
        normal = Vector3.zero;
        return null;
    }
}

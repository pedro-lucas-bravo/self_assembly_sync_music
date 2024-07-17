using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentVisualFeedback : MonoBehaviour
{
    public Color defaultColor = Color.black;
    public CrossBeatingBehaviour beatingBehaviour;
    public Transform genericRadius;
    public Renderer render;
    public GameObject idLabel;

    public Color TargetColor => _targetColor;

    private float _defaultAlpha = 1.0f;
    private Renderer _genericRadiusRender;

    private void Awake() {
        if(render == null)
            render = GetComponent<Renderer>();
        _targetMaterial = render.materials[0];
        _outlineMaterial = render.materials[1];
        SetOutline(Color.white, 0);
        _targetColor = _targetMaterial.color;
        if (idLabel != null) {
            _transIdLabel = idLabel.transform;
            idLabel.GetComponent<TMPro.TextMeshPro>().sortingOrder = 1;
            ShowIdLabel(true);
        }
        if (genericRadius != null) {
            _genericRadiusRender = genericRadius.GetComponent<Renderer>();
            genericRadius.localScale = Vector3.zero;
        }
    }

    private void Update() {
        if (_applyColor) {
            if (_setDirect) {
                _targetMaterial.color = _targetColor;
                _setDirect = false;
                _applyColor = false;
            } else {
                if (beatingBehaviour.IsApplying) {
                    beatingBehaviour.Update(Time.deltaTime);
                    _targetMaterial.color = Color.Lerp(defaultColor, _targetColor, beatingBehaviour.GetValue());
                } else {
                    _targetMaterial.color = defaultColor;
                    _applyColor = false;
                }
            }
        }
        if (_showIdLabel) {
            var dir = (Camera.main.transform.position - transform.position).normalized;
            //transText_.position = trans.position + dir * textSeparation_;
            _transIdLabel.forward = -dir;
        }
    }

    public void ChangeDefaultAlpha(float alpha) {
        _defaultAlpha = alpha;
        _targetColor.a = _defaultAlpha;
        _targetMaterial.color = _targetColor;
    }

    public void SetIdLabel(int id) {
        if(idLabel != null)
            idLabel.GetComponent<TMPro.TextMeshPro>().text = id.ToString();
    }

    bool _showIdLabel;
    Transform _transIdLabel;
    public void ShowIdLabel(bool show) {
        if (idLabel != null)
            idLabel.SetActive(show);
        _showIdLabel = show;
    }

    public void SetColor(Color color, bool beat) {
        _targetColor = color;
        _targetColor.a = _defaultAlpha;
        if (beat) {
            beatingBehaviour.Apply();            
        } else {
            _setDirect = true;            
        }
        _applyColor = true;
    }

    public void SetOutline(Color color, float width) {
        _outlineMaterial.SetColor("_OutlineColor", color);
        _outlineMaterial.SetFloat("_OutlineWidth", width);
    }

    public void SetGenericRadius(float radius, Color color, float lifespan = -1) {
        genericRadius.localScale = Vector3.one * radius * 2f;
        _genericRadiusRender.material.color = color;
        if (lifespan >= 0) { 
            IEnumerator CheckRadiusLifespan() {
                float time = 0;
                while (time < lifespan) {
                    time += Time.deltaTime;
                    yield return null;
                }
                genericRadius.localScale = Vector3.zero;
            }
            StartCoroutine(CheckRadiusLifespan());
        }
    }

    Material _targetMaterial;
    Material _outlineMaterial;
    Color _targetColor;
    bool _setDirect;
    bool _applyColor;

}

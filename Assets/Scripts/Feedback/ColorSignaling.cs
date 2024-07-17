using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSignaling : MonoBehaviour
{

    
    public Color signalColor = Color.red;
    public float signalDuration = 0.1f;

    private Renderer _rendererTarget;
    private Color _originalColor;

    private void Awake() {
        _rendererTarget = GetComponent<Renderer>();
        _originalColor = _rendererTarget.material.color;
    }

    private async UniTask ActivePulseFeedback(Color color) {
        _rendererTarget.material.color = color;
        await UniTask.Delay((int)(signalDuration * 1000f), cancellationToken: destroyCancellationToken);
        _rendererTarget.material.color = _originalColor;
    }

    public void Do() {
        ActivePulseFeedback(signalColor).Forget();
    }
}

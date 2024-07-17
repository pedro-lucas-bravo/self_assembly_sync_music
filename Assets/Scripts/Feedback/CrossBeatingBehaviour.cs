using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CrossBeatingBehaviour {

    public AnimationCurve beatingCurve;
    public float time;
    public float factor = 1;

    public bool IsApplying { get; private set; }

    public void Apply() {
        if (currentIndex_ < 16) {
            timerStack_[currentIndex_++] = 0;
            IsApplying = true;
        }
    }

    public float GetValue() {
        return currentValue_ * factor;
    }

    public void Update(float deltaTime) {
        if (!IsApplying) return;
        var maxValue = 0f;
        for (int i = 0; i < currentIndex_; i++) {
            var timer = timerStack_[i];
            if (timer <= time) {
                var value = beatingCurve.Evaluate(timer / time);
                if (value > maxValue)
                    maxValue = value;
                timerStack_[i] += deltaTime;
            } else {
                var value = beatingCurve.Evaluate(1.0f);
                if (value > maxValue)
                    maxValue = value;
                //interchange and remove
                timerStack_[i] = timerStack_[currentIndex_ - 1];
                i--;
                currentIndex_--;
            }
        }
        currentValue_ = maxValue;
        if (currentIndex_ <= 0) {
            currentIndex_ = 0;
            currentValue_ = 0;
            IsApplying = false;
        }
    }

    float currentValue_ = 0;
    float[] timerStack_ = new float[16];
    int currentIndex_ = 0;
}
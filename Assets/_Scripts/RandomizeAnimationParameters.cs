using System.Collections.Generic;
using UnityEngine;

public class RandomizeAnimationParameters : MonoBehaviour
{
    [SerializeField] bool cycleOffset, speed;
    [SerializeField] List<string> customBoolParameters;

    void Start()
    {
        var animator = GetComponent<Animator>();

        if (cycleOffset)
        {
            animator.SetFloat("cycleOffset", Random.Range(0.0f, 1.0f));
        }

        if (speed)
        {
            animator.SetFloat("speed", Random.Range(0.0f, 1.0f));
        }

        foreach (var parameter in customBoolParameters)
        {
            animator.SetBool(parameter, Random.Range(0.0f, 1.0f) < 0.5f);
        }
    }
}

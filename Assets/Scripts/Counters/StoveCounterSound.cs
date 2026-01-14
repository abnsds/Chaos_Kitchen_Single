using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounterSound : MonoBehaviour
{
    [SerializeField] private StoveCounter stoveCounter;
    private AudioSource audioSource;
    private float WarningSoundTimer;
    private bool playWarningSound;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        stoveCounter.OnStateChanged += StoveCounter_OnStateChanged;
        stoveCounter.OnProgressChanged += StoveCounter_OnProgressChanged;
    }

    private void StoveCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {
        float burnShowProgressAmout = .5f;
        playWarningSound = stoveCounter.IsFrid() && e.progressNormalized >= burnShowProgressAmout;
    }

    private void StoveCounter_OnStateChanged(object sender, StoveCounter.OnStateChangedEventArgs e)
    {
        bool playSound = e.state == StoveCounter.State.Frying || e.state == StoveCounter.State.Frid;
        if (playSound )
        {
            audioSource.Play();
        }
        else
        {
            audioSource.Pause();
        }
    }

    private void Update()
    {
        if( playWarningSound )
        {
            WarningSoundTimer -= Time.deltaTime;
            if (WarningSoundTimer < 0f)
            {
                float warningSoundTimerMax = .2f;
                WarningSoundTimer = warningSoundTimerMax;
                SoundMgr.GetInstance().PlayWarningSound(stoveCounter.transform.position);
            }
        }
        
    }
}

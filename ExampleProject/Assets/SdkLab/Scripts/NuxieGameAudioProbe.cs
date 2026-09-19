#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using UnityEngine;

namespace Nuxie.Unity.Samples
{
    // Explicit development-only proof of this Lab's pause-all-game-audio policy.
    public sealed class NuxieGameAudioProbe : MonoBehaviour
    {
        private INuxieClient sdk;
        private IDisposable activity;
        private AudioSource tone;
        private AudioClip clip;
        private readonly float[] output = new float[256];
        private float shownAt = -1, resumedAt = -1;
        private int pausedSample, resumedSample;
        private bool baseline, paused, dismissed, finished;

        public void Initialize(INuxieClient client)
        {
            sdk = client;
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            tone = gameObject.AddComponent<AudioSource>();
            clip = AudioClip.Create("Qualification 440Hz", 48000 * 30, 1, 48000, false);
            var samples = new float[clip.samples];
            for (var i = 0; i < samples.Length; i++) samples[i] = .1f * Mathf.Sin(i * 2 * Mathf.PI * 440 / 48000);
            clip.SetData(samples, 0);
            tone.clip = clip; tone.loop = true; tone.Play();
            activity = sdk.SubscribeActivity(value => {
                if (value.Name == "screen_shown" && shownAt < 0) { shownAt = Time.realtimeSinceStartup; pausedSample = tone.timeSamples; }
                if ((value.Name == "screen_dismissed" || value.Name == "journey_completed") && shownAt >= 0 && resumedAt < 0) { resumedAt = Time.realtimeSinceStartup; resumedSample = tone.timeSamples; }
            });
        }

        private void Update()
        {
            if (finished || tone == null) return;
            var now = Time.realtimeSinceStartup;
            if (shownAt < 0 && !baseline && tone.timeSamples > 9600 && Peak() > .001f)
            {
                baseline = true;
                Debug.Log("NUXIE_GAME_AUDIO baseline samples=" + tone.timeSamples + " peak=" + Peak());
            }
            if (shownAt >= 0 && !dismissed && now - shownAt > 4)
            {
                paused = AudioListener.pause && Math.Abs(tone.timeSamples - pausedSample) < 2880;
                Debug.Log("NUXIE_GAME_AUDIO presented paused=" + AudioListener.pause + " delta=" + (tone.timeSamples - pausedSample) + " pass=" + paused);
                dismissed = true;
                Dismiss();
            }
            if (resumedAt >= 0 && now - resumedAt > .5f && (Peak() > .001f || now - resumedAt > 3))
            {
                var resumed = !AudioListener.pause && tone.timeSamples - resumedSample > 4800 && Peak() > .001f;
                finished = true;
                Debug.Log("NUXIE_GAME_AUDIO completed pass=" + (baseline && paused && resumed) + " resumed=" + resumed + " delta=" + (tone.timeSamples - resumedSample) + " peak=" + Peak());
                tone.Stop();
            }
        }

        private float Peak() { tone.GetOutputData(output, 0); float peak = 0; foreach (var value in output) peak = Mathf.Max(peak, Mathf.Abs(value)); return peak; }
        private async void Dismiss() { try { await sdk.DismissAsync(); } catch (Exception error) { Debug.LogError("NUXIE_GAME_AUDIO failed " + error.Message); } }
        private void OnDestroy() { activity?.Dispose(); if (clip != null) Destroy(clip); }
    }
}
#endif

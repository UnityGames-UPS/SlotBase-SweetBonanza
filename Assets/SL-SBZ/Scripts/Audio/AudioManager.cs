using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Solid, encapsulated audio manager for Sweet Bonanza.
/// Manages dedicated audio channels (BGM, SFX Pool, UI, Reserve),
/// volume persistence via PlayerPrefs, dynamic multi-channel pooling,
/// and smooth audio transitions.
/// </summary>
[DisallowMultipleComponent]
internal class AudioManager : MonoBehaviour
{
    #region Singleton & Initialization

    internal static AudioManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        // Load persisted audio settings
        _musicEnabled  = PlayerPrefs.GetInt(PrefKeyMusic, 1) == 1;
        _sfxEnabled    = PlayerPrefs.GetInt(PrefKeySfx, 1) == 1;
        _musicVolume   = PlayerPrefs.GetFloat(PrefKeyMusicVol, 0.5f);
        _sfxVolume     = PlayerPrefs.GetFloat(PrefKeySfxVol, 1.0f);
        _masterVolume  = PlayerPrefs.GetFloat(PrefKeyMasterVol, 0.5f);
        isForceMuted   = PlayerPrefs.GetInt(PrefKeyMuted, 0) == 1;

        ApplyMasterVolume();
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private void Start()
    {
        // Start music even when muted: mute only sets the listener volume to 0,
        // so the music is already running when the player unmutes.
        if (_musicEnabled)
        {
            PlayBgMusic();
        }
    }

    private void OnDisable()
    {
        musicFadeTween?.Kill();
        musicFadeTween = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            musicFadeTween?.Kill();
            musicFadeTween = null;
            PlayerPrefs.Save();
            Instance = null;
        }
    }

    #endregion

    #region Constants & Persistence Keys

    private const string PrefKeyMusic     = "settings_ambient_music";
    private const string PrefKeySfx       = "settings_sound_fx";
    private const string PrefKeyMusicVol  = "audio_music_volume";
    private const string PrefKeySfxVol    = "audio_sfx_volume";
    private const string PrefKeyMasterVol = "settings_master_volume";
    private const string PrefKeyMuted     = "settings_sound_muted";

    #endregion

    #region Serialized Audio Channels

    [Header("Audio Sources (Channels)")]
    [SerializeField] private AudioSource bgMusicSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource reserveSource;
    [SerializeField] private AudioSource primaryButtonSource;
    [SerializeField] private AudioSource[] sfxSourcePool;

    #endregion

    #region Serialized Audio Clips (Preserving exact scene bindings)

    [Header("Core Gameplay & Background Clips")]
    [SerializeField] private AudioClip clipGameMainBg;
    [SerializeField] private AudioClip clipFeatureBg;
    [SerializeField] private AudioClip clipReelSpinLoop;
    [SerializeField] private AudioClip clipReelStop;
    [SerializeField] private AudioClip clipTensionBuilder;

    [Header("Win & Feedback Clips")]
    [SerializeField] private AudioClip clipWinLinePhase1Start;
    [SerializeField] private AudioClip clipWinTypePopupOpen;
    [SerializeField] private AudioClip clipResultPopupOpen;

    [Header("UI & Button Clips")]
    [SerializeField] private AudioClip clipPrimaryActionButton;
    [SerializeField] private AudioClip clipGeneralButtonClick;
    [SerializeField] private AudioClip clipBetPlusMinus;
    [SerializeField] private AudioClip clipMaxBetReached;
    [SerializeField] private AudioClip clipPopupOpenClose;
    [SerializeField] private AudioClip clipAutoplayPanelOpen;
    [SerializeField] private AudioClip clipTurboButtonClick;

    [Header("Sweet Bonanza Tumble & Feature Clips")]
    [SerializeField] private AudioClip clipSymbolPop;
    [SerializeField] private AudioClip clipBombExplode;
    [SerializeField] private AudioClip clipScatterCelebration;

    #endregion

    #region Encapsulated State & Backing Fields

    private bool _musicEnabled = true;
    private bool _sfxEnabled   = true;
    private float _musicVolume = 0.5f;
    private float _sfxVolume   = 1.0f;
    private float _masterVolume = 1.0f;
    private bool isForceMuted  = false;

    private Tween musicFadeTween;

    // SFX voice pool round-robin index
    private int nextSfxPoolIndex = 0;

    #endregion

    #region Public Encapsulated Properties

    internal bool MusicEnabled => _musicEnabled;
    internal bool SfxEnabled   => _sfxEnabled;
    internal float MusicVolume => _musicVolume;
    internal float SfxVolume   => _sfxVolume;
    internal float MasterVolume => _masterVolume;

    #endregion

    #region SFX Voice Pooling & Editor Helper

#if UNITY_EDITOR
    [ContextMenu("Auto-Assign AudioSources From This Object")]
    private void AutoAssignSources()
    {
        UnityEditor.Undo.RecordObject(this, "Auto-Assign AudioSources");
        sfxSourcePool = GetComponents<AudioSource>();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private AudioSource GetNextAvailableSfxSource()
    {
        if (sfxSourcePool == null || sfxSourcePool.Length == 0) return uiSource;

        // 1. Look for a voice that has finished playing
        for (int i = 0; i < sfxSourcePool.Length; i++)
        {
            var src = sfxSourcePool[i];
            if (src != null && !src.isPlaying) return src;
        }

        // 2. Round-robin steal if all voices are currently active
        var stolen = sfxSourcePool[nextSfxPoolIndex];
        nextSfxPoolIndex = (nextSfxPoolIndex + 1) % sfxSourcePool.Length;
        return stolen != null ? stolen : uiSource;
    }

    #endregion

    #region Volume & Mute Controls

    internal void SetMusicEnabled(bool on)
    {
        if (_musicEnabled == on) return;
        _musicEnabled = on;
        PlayerPrefs.SetInt(PrefKeyMusic, on ? 1 : 0);
        ApplyMusicVolume();

        if (!on && bgMusicSource != null)
        {
            bgMusicSource.Stop();
        }
        else if (on && bgMusicSource != null && !bgMusicSource.isPlaying)
        {
            PlayBgMusic();
        }
    }

    internal void SetSfxEnabled(bool on)
    {
        if (_sfxEnabled == on) return;
        _sfxEnabled = on;
        PlayerPrefs.SetInt(PrefKeySfx, on ? 1 : 0);
        ApplySfxVolume();
    }

    internal void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefKeyMusicVol, _musicVolume);
        ApplyMusicVolume();
    }

    internal void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefKeySfxVol, _sfxVolume);
        ApplySfxVolume();
    }

    internal void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefKeyMasterVol, _masterVolume);
        ApplyMasterVolume();
    }

    internal void SetMuteAll(bool forceMute)
    {
        if (forceMute == isForceMuted) return;
        isForceMuted = forceMute;
        PlayerPrefs.SetInt(PrefKeyMuted, forceMute ? 1 : 0);
        ApplyMasterVolume();
    }

    private void ApplyMasterVolume()
    {
        AudioListener.volume = isForceMuted ? 0f : _masterVolume;
    }

    private void ApplyMusicVolume()
    {
        if (bgMusicSource == null) return;
        bgMusicSource.volume = _musicEnabled ? _musicVolume : 0f;
    }

    private void ApplySfxVolume()
    {
        float v = _sfxEnabled ? _sfxVolume : 0f;
        if (uiSource != null) uiSource.volume = v;
        if (reserveSource != null) reserveSource.volume = v;
        if (primaryButtonSource != null) primaryButtonSource.volume = v;

        if (sfxSourcePool != null)
        {
            for (int i = 0; i < sfxSourcePool.Length; i++)
            {
                if (sfxSourcePool[i] != null) sfxSourcePool[i].volume = v;
            }
        }
    }

    #endregion

    #region Playback Core Helpers

    private void PlayUISound(AudioClip clip)
    {
        if (!_sfxEnabled || clip == null) return;

        var source = GetNextAvailableSfxSource();
        if (source != null)
        {
            source.pitch = 1f;
            source.PlayOneShot(clip, _sfxVolume);
        }
        else if (uiSource != null)
        {
            uiSource.pitch = 1f;
            uiSource.PlayOneShot(clip, _sfxVolume);
        }
    }

    internal void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (!_sfxEnabled || clip == null) return;
        var source = GetNextAvailableSfxSource();
        if (source != null)
        {
            source.pitch = 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _sfxVolume);
        }
    }

    internal void PlayOneShotRandomPitch(AudioClip clip, float pitchVariation = 0.04f, float volumeScale = 1f)
    {
        if (!_sfxEnabled || clip == null) return;
        var source = GetNextAvailableSfxSource();
        if (source != null)
        {
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _sfxVolume);
        }
    }

    private void PlayLoop(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.DOKill();
        source.clip = clip;
        source.loop = true;
        source.pitch = 1f;
        source.volume = (source == bgMusicSource)
            ? (_musicEnabled ? _musicVolume : 0f)
            : (_sfxEnabled ? _sfxVolume : 0f);
        source.Play();
    }

    private void StopSource(AudioSource source)
    {
        if (source == null) return;
        source.DOKill();
        source.Stop();
        source.loop = false;
        source.clip = null;
    }

    #endregion

    #region Background Music & Cross-fading

    internal void PlayBgMusic()
    {
        if (bgMusicSource == null || clipGameMainBg == null) return;
        if (bgMusicSource.isPlaying && bgMusicSource.clip == clipGameMainBg) return;

        CrossFadeMusic(clipGameMainBg, 0.4f, true);
    }

    internal void PlayFeatureBg()
    {
        if (clipFeatureBg == null) return;
        CrossFadeMusic(clipFeatureBg, 0.5f, true);
    }

    internal void StopFeatureBg()
    {
        if (bgMusicSource != null && bgMusicSource.clip == clipFeatureBg)
        {
            CrossFadeMusic(clipGameMainBg, 0.5f, true);
        }
    }

    internal void CrossFadeMusic(AudioClip newClip, float duration = 0.5f, bool loop = true)
    {
        if (bgMusicSource == null || newClip == null) return;
        if (!_musicEnabled)
        {
            if (bgMusicSource != null) bgMusicSource.Stop();
            return;
        }

        musicFadeTween?.Kill();
        float targetVolume = _musicEnabled ? _musicVolume : 0f;

        if (bgMusicSource.isPlaying)
        {
            // Smoothly fade out current track, switch, then fade up
            musicFadeTween = bgMusicSource.DOFade(0f, duration * 0.45f).OnComplete(() =>
            {
                bgMusicSource.clip = newClip;
                bgMusicSource.loop = loop;
                bgMusicSource.Play();
                musicFadeTween = bgMusicSource.DOFade(targetVolume, duration * 0.55f);
            });
        }
        else
        {
            bgMusicSource.clip = newClip;
            bgMusicSource.loop = loop;
            bgMusicSource.volume = 0f;
            bgMusicSource.Play();
            musicFadeTween = bgMusicSource.DOFade(targetVolume, duration);
        }
    }

    #endregion

    #region Core Game Actions

    internal void PlayPrimaryActionButton()
    {
        if (!_sfxEnabled) return;
        AudioClip clip = clipPrimaryActionButton != null ? clipPrimaryActionButton : clipGeneralButtonClick;
        if (clip == null) return;

        if (primaryButtonSource != null)
        {
            primaryButtonSource.pitch = 1f;
            primaryButtonSource.PlayOneShot(clip, _sfxVolume);
        }
        else
        {
            PlayUISound(clip);
        }
    }

    internal void PlaySpinStart()    => PlayPrimaryActionButton();
    internal void PlaySpinStop()     => PlayPrimaryActionButton();
    internal void PlayAutoplayStop() => PlayPrimaryActionButton();

    internal void PlayButton()
    {
        PlayUISound(clipGeneralButtonClick);
    }

    internal void PlayBetPlusMinus()
    {
        PlayUISound(clipBetPlusMinus);
    }

    internal void PlayMaxBetReached()
    {
        PlayUISound(clipMaxBetReached);
    }

    internal void PlayPopupOpenClose()
    {
        PlayUISound(clipPopupOpenClose != null ? clipPopupOpenClose : clipGeneralButtonClick);
    }

    internal void PlayPopupClose() => PlayPopupOpenClose();
    internal void PlayPopupOpen()  => PlayPopupOpenClose();

    internal void PlayAutoplayPanelOpen()
    {
        PlayUISound(clipAutoplayPanelOpen != null ? clipAutoplayPanelOpen : clipPopupOpenClose);
    }

    internal void PlayTurboButtonClick()
    {
        PlayUISound(clipTurboButtonClick != null ? clipTurboButtonClick : clipGeneralButtonClick);
    }

    #endregion

    #region Reel & Tumble Audio

    internal void PlayReelStop()
    {
        PlayOneShotRandomPitch(clipReelStop, 0.03f, 1f);
    }

    internal void PlayWinLinePhase1Start()
    {
        PlayUISound(clipWinLinePhase1Start);
    }

    internal void PlayReelSpinLoop()
    {
        if (!_sfxEnabled || clipReelSpinLoop == null) return;
        AudioSource targetSource = (reserveSource != null) ? reserveSource : uiSource;
        PlayLoop(targetSource, clipReelSpinLoop);
    }

    internal void StopReelSpinLoop()
    {
        if (reserveSource != null && reserveSource.clip == clipReelSpinLoop)
        {
            StopSource(reserveSource);
        }
        if (uiSource != null && uiSource.clip == clipReelSpinLoop)
        {
            StopSource(uiSource);
        }
    }

    internal void PlayTensionBuilder()
    {
        if (!_sfxEnabled || clipTensionBuilder == null) return;
        // Own channel (uiSource) so the tension loop does not cut the reel spin loop on reserveSource.
        AudioSource targetSource = (uiSource != null) ? uiSource : reserveSource;
        PlayLoop(targetSource, clipTensionBuilder);
    }

    internal void StopTensionBuilder()
    {
        if (reserveSource != null && reserveSource.clip == clipTensionBuilder)
        {
            StopSource(reserveSource);
        }
        if (uiSource != null && uiSource.clip == clipTensionBuilder)
        {
            StopSource(uiSource);
        }
    }

    #endregion

    #region Win Celebrations & Popups

    internal void PlayWinTypePopupOpen()
    {
        if (!_sfxEnabled || clipWinTypePopupOpen == null) return;
        AudioSource targetSource = (reserveSource != null) ? reserveSource : uiSource;
        PlayLoop(targetSource, clipWinTypePopupOpen);
    }

    internal void StopWinTypePopupOpen()
    {
        if (reserveSource != null && reserveSource.clip == clipWinTypePopupOpen)
        {
            StopSource(reserveSource);
        }
        if (uiSource != null && uiSource.clip == clipWinTypePopupOpen)
        {
            StopSource(uiSource);
        }
    }

    internal void PlayResultPopupOpen()
    {
        PlayUISound(clipResultPopupOpen != null ? clipResultPopupOpen : clipPopupOpenClose);
    }

    #endregion

    #region Sweet Bonanza Tumble & Feature SFX

    internal void PlaySymbolPop()
    {
        AudioClip clip = clipSymbolPop != null ? clipSymbolPop : clipReelStop;
        if (clip != null) PlayOneShotRandomPitch(clip, 0.05f, 0.9f);
    }

    internal void PlayBombExplode()
    {
        AudioClip clip = clipBombExplode != null ? clipBombExplode : clipWinLinePhase1Start;
        if (clip != null) PlayOneShot(clip, 1f);
    }

    internal void PlayScatterCelebration()
    {
        AudioClip clip = clipScatterCelebration != null ? clipScatterCelebration : clipResultPopupOpen;
        if (clip != null) PlayOneShot(clip, 1f);
    }

    #endregion

    #region Application Focus & Lifecycle Handling

    private void OnApplicationFocus(bool hasFocus)
    {
        // Solid focus handling: never clobbers player's custom master volume setting!
        AudioListener.volume = (!hasFocus || isForceMuted) ? 0f : _masterVolume;
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused) PlayerPrefs.Save();
        AudioListener.volume = (isPaused || isForceMuted) ? 0f : _masterVolume;
    }

    #endregion
}

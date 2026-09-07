using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource effectAudioSource;
    [SerializeField] private AudioSource defaultAudioSource;
    [SerializeField] private AudioSource bossAudioSource;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip energyClip;
    [SerializeField] private AudioClip reLoadClip;

    [Header("Master Mute Button Settings")]
    [SerializeField] private Image muteButtonImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Slider volumeSlider;

    private bool isMuted = false;
    private float previousVolume = 1f;

    [Header("Default Audio (Nhạc nền + Boss) Mute Button Settings")]
    [SerializeField] private Image defaultMuteButtonImage;
    [SerializeField] private Slider defaultVolumeSlider;

    private bool isDefaultMuted = false;
    private float previousDefaultVolume = 1f;

    [Header("Effect Audio (SFX riêng) Mute Button Settings")]
    [SerializeField] private Image effectMuteButtonImage;
    [SerializeField] private Slider effectVolumeSlider;

    private bool isEffectMuted = false;
    private float previousEffectVolume = 1f;

    private void Start()
    {
        // Khôi phục lại âm lượng mặc định mỗi khi mở game (tránh bị kẹt ở 0 do test trước đó)
        AudioListener.volume = 1f;

        if (defaultAudioSource != null) defaultAudioSource.volume = 1f;
        if (bossAudioSource != null) bossAudioSource.volume = 1f;
        if (effectAudioSource != null) effectAudioSource.volume = 1f;
    }

    public void PlayShootSound()
    {
        effectAudioSource.PlayOneShot(shootClip);
    }
    public void PlayReloadSound()
    {
        effectAudioSource.PlayOneShot(reLoadClip);
    }

    // Phát 1 âm thanh bất kỳ - dùng cho âm thanh riêng theo nhân vật (VD tiếng bắn/nạp đạn của Pháp sư)
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null) effectAudioSource.PlayOneShot(clip);
    }
    public void PlayEnergySound()
    {
        effectAudioSource.PlayOneShot(energyClip);
    }
    public void PlayDefaultAudio()
    {
        bossAudioSource.Stop();
        defaultAudioSource.Play();
    }
    public void PlayBossAudio()
    {
        defaultAudioSource.Stop();
        bossAudioSource.Play();
    }

    /// <summary>
    /// Thay đổi tổng âm lượng của game. Hàm này dùng cho UI Slider.
    /// Giá trị volume thường nằm từ 0.0 đến 1.0
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        // Nếu người chơi kéo thanh trượt > 0 trong lúc đang tắt tiếng thì tự động bật tiếng lại
        if (isMuted && volume > 0f)
        {
            isMuted = false;
            if (muteButtonImage != null && soundOnSprite != null)
            {
                muteButtonImage.sprite = soundOnSprite;
            }
        }

        if (!isMuted && volume == 0f)
        {
            isMuted = true;
            if (muteButtonImage != null && soundOffSprite != null)
            {
                muteButtonImage.sprite = soundOffSprite;
            }
        }

        AudioListener.volume = volume;
        if (!isMuted) previousVolume = volume;
    }

    /// <summary>
    /// Bật/Tắt âm thanh (Mute/Unmute) và cập nhật UI.
    /// Dùng cho Nút Mute.
    /// </summary>
    public void ToggleMute()
    {
        isMuted = !isMuted;
        if (isMuted)
        {
            // Lưu lại mức âm lượng hiện tại trước khi tắt
            previousVolume = AudioListener.volume > 0f ? AudioListener.volume : 1f;
            AudioListener.volume = 0f;
            
            // Đổi hình nút sang Tắt tiếng
            if (muteButtonImage != null && soundOffSprite != null)
                muteButtonImage.sprite = soundOffSprite;
            
            // Đồng bộ kéo thanh Slider về 0 (nhưng không kích hoạt SetMasterVolume lại làm mất logic)
            if (volumeSlider != null)
                volumeSlider.SetValueWithoutNotify(0f); 
        }
        else
        {
            // Khôi phục mức âm lượng
            AudioListener.volume = previousVolume;
            
            // Đổi hình nút sang Bật tiếng
            if (muteButtonImage != null && soundOnSprite != null)
                muteButtonImage.sprite = soundOnSprite;
            
            // Trả thanh Slider về vị trí cũ
            if (volumeSlider != null)
                volumeSlider.SetValueWithoutNotify(previousVolume);
        }
    }

    /// <summary>
    /// Chỉnh riêng âm lượng nhạc nền (Default) + âm thanh Boss - cả 2 dùng chung 1 slider vì Boss Audio
    /// được coi là 1 phần của nhóm âm thanh Default. Dùng cho UI Slider riêng trong Settings.
    /// </summary>
    public void SetDefaultVolume(float volume)
    {
        if (isDefaultMuted && volume > 0f)
        {
            isDefaultMuted = false;
            if (defaultMuteButtonImage != null && soundOnSprite != null)
            {
                defaultMuteButtonImage.sprite = soundOnSprite;
            }
        }

        if (!isDefaultMuted && volume == 0f)
        {
            isDefaultMuted = true;
            if (defaultMuteButtonImage != null && soundOffSprite != null)
            {
                defaultMuteButtonImage.sprite = soundOffSprite;
            }
        }

        if (defaultAudioSource != null) defaultAudioSource.volume = volume;
        if (bossAudioSource != null) bossAudioSource.volume = volume;
        if (!isDefaultMuted) previousDefaultVolume = volume;
    }

    /// <summary>
    /// Bật/Tắt riêng nhạc nền + âm thanh Boss. Dùng cho nút Mute riêng của nhóm Default.
    /// </summary>
    public void ToggleDefaultMute()
    {
        isDefaultMuted = !isDefaultMuted;
        if (isDefaultMuted)
        {
            float currentVolume = (defaultAudioSource != null) ? defaultAudioSource.volume : 1f;
            previousDefaultVolume = currentVolume > 0f ? currentVolume : 1f;

            if (defaultAudioSource != null) defaultAudioSource.volume = 0f;
            if (bossAudioSource != null) bossAudioSource.volume = 0f;

            if (defaultMuteButtonImage != null && soundOffSprite != null)
                defaultMuteButtonImage.sprite = soundOffSprite;

            if (defaultVolumeSlider != null)
                defaultVolumeSlider.SetValueWithoutNotify(0f);
        }
        else
        {
            if (defaultAudioSource != null) defaultAudioSource.volume = previousDefaultVolume;
            if (bossAudioSource != null) bossAudioSource.volume = previousDefaultVolume;

            if (defaultMuteButtonImage != null && soundOnSprite != null)
                defaultMuteButtonImage.sprite = soundOnSprite;

            if (defaultVolumeSlider != null)
                defaultVolumeSlider.SetValueWithoutNotify(previousDefaultVolume);
        }
    }

    /// <summary>
    /// Chỉnh riêng âm lượng âm thanh hiệu ứng (bắn, nạp đạn, nhặt vật phẩm...). Dùng cho UI Slider riêng trong Settings.
    /// </summary>
    public void SetEffectVolume(float volume)
    {
        if (isEffectMuted && volume > 0f)
        {
            isEffectMuted = false;
            if (effectMuteButtonImage != null && soundOnSprite != null)
            {
                effectMuteButtonImage.sprite = soundOnSprite;
            }
        }

        if (!isEffectMuted && volume == 0f)
        {
            isEffectMuted = true;
            if (effectMuteButtonImage != null && soundOffSprite != null)
            {
                effectMuteButtonImage.sprite = soundOffSprite;
            }
        }

        if (effectAudioSource != null) effectAudioSource.volume = volume;
        if (!isEffectMuted) previousEffectVolume = volume;
    }

    /// <summary>
    /// Bật/Tắt riêng âm thanh hiệu ứng. Dùng cho nút Mute riêng của nhóm Effect.
    /// </summary>
    public void ToggleEffectMute()
    {
        isEffectMuted = !isEffectMuted;
        if (isEffectMuted)
        {
            float currentVolume = (effectAudioSource != null) ? effectAudioSource.volume : 1f;
            previousEffectVolume = currentVolume > 0f ? currentVolume : 1f;

            if (effectAudioSource != null) effectAudioSource.volume = 0f;

            if (effectMuteButtonImage != null && soundOffSprite != null)
                effectMuteButtonImage.sprite = soundOffSprite;

            if (effectVolumeSlider != null)
                effectVolumeSlider.SetValueWithoutNotify(0f);
        }
        else
        {
            if (effectAudioSource != null) effectAudioSource.volume = previousEffectVolume;

            if (effectMuteButtonImage != null && soundOnSprite != null)
                effectMuteButtonImage.sprite = soundOnSprite;

            if (effectVolumeSlider != null)
                effectVolumeSlider.SetValueWithoutNotify(previousEffectVolume);
        }
    }
}

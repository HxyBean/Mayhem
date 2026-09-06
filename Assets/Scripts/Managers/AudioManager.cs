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

    [Header("Mute Button Settings")]
    [SerializeField] private Image muteButtonImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Slider volumeSlider;

    private bool isMuted = false;
    private float previousVolume = 1f;

    private void Start()
    {
        // Khôi phục lại âm lượng mặc định mỗi khi mở game (tránh bị kẹt ở 0 do test trước đó)
        AudioListener.volume = 1f;
    }

    public void PlayShootSound()
    {
        effectAudioSource.PlayOneShot(shootClip);
    }
    public void PlayReloadSound()
    {
        effectAudioSource.PlayOneShot(reLoadClip);
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
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gắn vào mỗi nút nhân vật trong panel Character Select (Scene MainMenu).
// Bấm vào: lưu nhân vật đã chọn rồi load đúng Scene của Stage đã chọn trước đó.
public class CharacterButton : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (characterData == null) return;
        if (GameProgress.SelectedStage == null || string.IsNullOrEmpty(GameProgress.SelectedStage.sceneName)) return;

        GameProgress.SelectedCharacter = characterData;
        SceneManager.LoadScene(GameProgress.SelectedStage.sceneName);
    }
}

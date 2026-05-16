using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Button btnAICompose;
    public Button btnScoreLibrary;
    public AIChatManager aiChat;
    public ScoreLibraryManager scoreLibrary;

    void Start()
    {
        if (btnAICompose != null && aiChat != null)
            btnAICompose.onClick.AddListener(() => aiChat.OpenChat());

        if (btnScoreLibrary != null && scoreLibrary != null)
            btnScoreLibrary.onClick.AddListener(() => scoreLibrary.OpenLibrary());
    }
}
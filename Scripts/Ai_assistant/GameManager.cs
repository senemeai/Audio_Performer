using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Button btnAICompose;
    public AIChatManager aiChat;

    void Start()
    {
        btnAICompose.onClick.AddListener(() => aiChat.OpenChat());
    }
}
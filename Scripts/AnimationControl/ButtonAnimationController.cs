using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ButtonAnimationController : MonoBehaviour
{
    [Header("组件引用")]
    public Animator imageAnimator;           // 图片的Animator组件
    public TextMeshProUGUI buttonText;       // 按钮上的TextMeshPro文字
    public Image targetImage;                // 目标图片组件（用于控制raycastTarget）

    [Header("动画Trigger名称")]
    public string closeAnimTrigger = "Close";  // 关闭动画的Trigger（默认→关闭状态）
    public string openAnimTrigger = "Open";    // 打开动画的Trigger（打开→关闭状态）

    [Header("文字设置")]
    public string closedText = "关闭";         // 关闭状态显示的文字
    public string openedText = "打开";         // 打开状态显示的文字

    private bool isOpen = false;      // 当前图片是否处于打开状态
    private bool isAnimating = false; // 防止动画播放中重复点击

    void Start()
    {
        // 获取TextMeshPro组件
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        // 获取Image组件（如果没有手动拖拽）
        if (targetImage == null)
            targetImage = imageAnimator.GetComponent<Image>();

        // 设置初始文字
        buttonText.text = closedText;

        // 初始状态：raycastTarget = false（不可交互）
        if (targetImage != null)
            targetImage.raycastTarget = false;

        // 绑定按钮点击事件
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        // 动画播放中不允许点击
        if (isAnimating) return;

        StartCoroutine(PlayAnimation());
    }

    IEnumerator PlayAnimation()
    {
        isAnimating = true;

        if (!isOpen)
        {
            // 当前是关闭状态 → 要切换到打开状态
            // 播放关闭动画（从静止到打开的过程）
            imageAnimator.SetTrigger(closeAnimTrigger);

            // 等待动画播放完成
            yield return WaitForAnimationComplete();

            // 切换状态
            isOpen = true;
            buttonText.text = openedText;

            // 开启raycastTarget（图片变得可交互）
            if (targetImage != null)
                targetImage.raycastTarget = true;
        }
        else
        {
            // 当前是打开状态 → 要切换到关闭状态
            // 播放打开动画（从打开到关闭的过程）
            imageAnimator.SetTrigger(openAnimTrigger);

            // 等待动画播放完成
            yield return WaitForAnimationComplete();

            // 切换状态
            isOpen = false;
            buttonText.text = closedText;

            // 关闭raycastTarget（图片恢复不可交互）
            if (targetImage != null)
                targetImage.raycastTarget = false;
        }

        isAnimating = false;
    }

    // 等待当前动画播放完成
    IEnumerator WaitForAnimationComplete()
    {
        // 获取当前播放的动画信息
        AnimatorStateInfo stateInfo = imageAnimator.GetCurrentAnimatorStateInfo(0);

        // 等待动画播放到结束
        while (stateInfo.normalizedTime < 1f)
        {
            stateInfo = imageAnimator.GetCurrentAnimatorStateInfo(0);
            yield return null;
        }

        // 额外等待一帧确保动画完全结束
        yield return null;
    }
}
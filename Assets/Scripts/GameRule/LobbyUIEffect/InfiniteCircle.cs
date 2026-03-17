using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 舞力全开 Anxiety 画框 无限循环放大/缩小动画
/// 修复版：平滑动画 + 不报错 + 无限循环
/// </summary>
public class InfiniteCircle : MonoBehaviour
{
    [Header("<<<效果参数>>>")]
    public Image targetImage;
    public Vector2 targetSize = new Vector2(1200, 1200); // 最终放大大小
    public Vector2 targetPos = new Vector2(0, 0);        // 最终位置
    public int loopTimes = -1;                           // -1 = 无限循环
    public float animationTime = 1.2f;                   // 一次动画时长
    public bool autoStart = true;                        // 自动开始
    public float WaitTime = 0.5f;

    private RectTransform _rect;
    private Vector2 _originSize;
    private Vector2 _originPos;
    private bool _isAnimating = false;
    private float _timer = 0;
    private float nextWaitFinish = 0f;


    void Start()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        _rect = targetImage.rectTransform;
        _originSize = _rect.sizeDelta;
        _originPos = _rect.anchoredPosition;

        if (autoStart)
            StartAnimation();
    }

    void Update()
    {
        if (!_isAnimating) {

            return;
        }

        if (Time.time < nextWaitFinish) {
            return;
        }
        

        // 平滑动画
        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / animationTime);

        // 平滑缩放 + 位移
        _rect.sizeDelta = Vector2.Lerp(_originSize, targetSize, t);
        _rect.anchoredPosition = Vector2.Lerp(_originPos, targetPos, t);

        // 单次动画完成
        if (t >= 1)
        {
            OneLoopFinish();
            nextWaitFinish = Time.time + WaitTime;
        }
    }

    /// <summary>
    /// 开始动画
    /// </summary>
    public void StartAnimation()
    {
        _timer = 0;
        _isAnimating = true;
    }

    /// <summary>
    /// 一次循环结束
    /// </summary>
    void OneLoopFinish()
    {
        _isAnimating = false;

        // 有限次数
        if (loopTimes > 0)
        {
            loopTimes--;
            if (loopTimes == 0) return;
        }

        // 重置并继续循环
        ResetToOrigin();
        StartAnimation();
    }

    /// <summary>
    /// 重置回初始状态
    /// </summary>
    void ResetToOrigin()
    {
        _rect.sizeDelta = _originSize;
        _rect.anchoredPosition = _originPos;
        _timer = 0;
    }
}
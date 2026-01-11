using UnityEngine;

/* UI 요소와 입력을 총괄하는 매니저 클래스입니다. */
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Mobile Movement Buttons")]
    [SerializeField] private DirectionButton leftButton;
    [SerializeField] private DirectionButton rightButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 좌우 버튼의 상태를 종합하여 -1, 0, 1 값을 반환합니다.
    public float GetHorizontalInput()
    {
        float h = 0f;
        if (leftButton != null && leftButton.IsPressed) h -= 1f;
        if (rightButton != null && rightButton.IsPressed) h += 1f;
        return h;
    }
}
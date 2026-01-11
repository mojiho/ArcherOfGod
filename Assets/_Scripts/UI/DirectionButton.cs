using UnityEngine;
using UnityEngine.EventSystems;

/* 터치 입력을 받기 위한 스크립트 입니다.*/
public class DirectionButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerExitHandler, ICancelHandler
{
    // 외부 프래스 판단 변수
    public bool IsPressed { get; private set; }

    // 눌렀을 때
    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
    }

    // 뗐을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
    }

    // 누른 상태로 버튼 밖으로 나가면(드래그로 벗어남) false 처리
    public void OnPointerExit(PointerEventData eventData)
    {
        IsPressed = false;
    }

    public void OnCancel(BaseEventData eventData)
    {
        IsPressed = false;
    }

    private void OnDisable()
    {
        IsPressed = false;
    }
}

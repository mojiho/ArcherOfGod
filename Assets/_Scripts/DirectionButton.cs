using UnityEngine;
using UnityEngine.EventSystems;

/* 터치 입력을 받기 위한 스크립트 입니다.*/
public class DirectionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // 외부에서 이 변수를 보고 눌렸는지 판단함
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
}
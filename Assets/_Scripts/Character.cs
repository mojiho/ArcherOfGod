using UnityEngine;

/* 
 * 캐릭터의 모든 상태와 컴포넌트를 연결하는 중심 허브 클래스입니다.
 */
[RequireComponent(typeof(Health))]
public class Character : MonoBehaviour
{
    public Health Health { get; private set; }
    public Animator Anim { get; private set; }
    public SpriteRenderer Sprite { get; private set; }
    public ISkillCaster Controller { get; private set; }

    private void Awake()
    {
        Health = GetComponent<Health>();
        Anim = GetComponent<Animator>();
        Sprite = GetComponent<SpriteRenderer>();
        Controller = GetComponent<ISkillCaster>();
    }
    private void Start()
    {
        if (Health != null)
        {
            Health.OnDamageTaken += (damage, pos, dir) => {
                if (DamagePopupManager.Instance != null)
                {
                    DamagePopupManager.Instance.ShowPopup(damage, pos, dir);
                }
            };
        }
    }
}
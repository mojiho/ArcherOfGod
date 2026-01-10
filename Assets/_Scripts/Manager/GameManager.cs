using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 (UI)
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 111f; // 게임 시간 (60초)

    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;   // 남은 시간 표시
    [SerializeField] private TextMeshProUGUI centerText;  // 카운트다운 & 결과 (Win/Loss)
    [SerializeField] private GameObject startButton;      // 게임 시작 버튼
    [SerializeField] private GameObject restartButton;    // 재시작 버튼 (선택사항)
    [SerializeField] private HudHpBar playerHpBar;
    [SerializeField] private HudHpBar enemyHpBar;
    [SerializeField] private GameObject pannel_blackOut;

    // 글로벌 참조용
    public SkillData skillDatabase;
    [HideInInspector] public GameObject player; // 다른 스크립트들이 참조할 수 있게
    [HideInInspector] public GameObject enemy;

    [SerializeField] private SkillUIManager skillUIManager;

    private float _currentTime;
    private GameState _currentState = GameState.Ready;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 게임 시작 전 초기화
        InitGame();
    }

    private void Update()
    {
        if (_currentState == GameState.Playing)
        {
            // 시간 카운트 다운
            _currentTime -= Time.deltaTime;
            UpdateTimerUI();

            // 시간 종료 체크 (무승부)
            if (_currentTime <= 0)
            {
                OnGameOver("DRAW", Color.yellow);
            }
        }
    }

    // 초기 상태로 리셋
    private void InitGame()
    {
        _currentState = GameState.Ready;
        _currentTime = gameDuration;

        if (timerText) timerText.text = $"{_currentTime:F0}";
        if (centerText) centerText.text = "Ready?";

        if (restartButton) restartButton.SetActive(false);
        if (startButton) startButton.SetActive(true);

        // 기존 캐릭터가 있다면 제거 (재시작 시)
        if (player != null) Destroy(player);
        if (enemy != null) Destroy(enemy);
    }

    // ▶ [버튼 연결용] 시작 버튼 누르면 호출
    public void OnClickStartGame()
    {
        StartCoroutine(GameFlowRoutine());
    }

    // ▶ [버튼 연결용] 재시작 버튼
    public void OnClickRestart()
    {
        InitGame();
        // 바로 시작하고 싶으면 OnClickStartGame(); 호출
    }

    // 게임 전체 흐름 코루틴
    private IEnumerator GameFlowRoutine()
    {
        // 1. UI 숨기기 및 상태 변경
        if (startButton) startButton.SetActive(false);
        if (restartButton) restartButton.SetActive(false);
        if (centerText) centerText.gameObject.SetActive(true);
        _currentState = GameState.Countdown;

        // 2. 카운트다운 (3, 2, 1, GO!)
        string[] counts = { "3", "2", "1", "BATTLE!" };
        foreach (var count in counts)
        {
            if (centerText) centerText.text = count;
            // 텍스트 커지는 애니메이션 (선택사항)
            if (centerText) centerText.transform.localScale = Vector3.one * 1.5f;

            yield return new WaitForSeconds(1f);
        }

        if (centerText) centerText.text = ""; // 중앙 텍스트 지우기
        if (pannel_blackOut) pannel_blackOut.SetActive(false);

        // 3. 캐릭터 스폰
        SpawnCharacters();

        // 4. 게임 시작 (타이머 작동)
        _currentState = GameState.Playing;
    }

    private void SpawnCharacters()
    {
        // 1. 플레이어 생성 및 연결
        if (playerPrefab != null && playerSpawnPoint != null)
        {
            player = Instantiate(playerPrefab, playerSpawnPoint.position, Quaternion.identity);

            var character = player.GetComponent<Character>();
            if (character != null)
            {
                // [핵심] 체력바에 플레이어 연결!
                if (playerHpBar != null) playerHpBar.Setup(character);

                if (skillUIManager != null) skillUIManager.Setup(player);

                character.Health.OnDead += () => OnGameOver("YOU LOSE", Color.red);
            }
        }

        // 2. 적 생성 및 연결
        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            enemy = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);

            var character = enemy.GetComponent<Character>();
            if (character != null)
            {
                // [핵심] 체력바에 적 연결!
                if (enemyHpBar != null) enemyHpBar.Setup(character);

                character.Health.OnDead += () => OnGameOver("YOU WIN!", Color.green);
            }
        }
    }

    // 게임 오버 처리
    private void OnGameOver(string message, Color color)
    {
        if (_currentState == GameState.GameOver) return; // 이미 끝났으면 무시

        _currentState = GameState.GameOver;

        // 결과 텍스트 표시
        if (centerText)
        {
            centerText.text = message;
            centerText.color = color;
            centerText.gameObject.SetActive(true);
        }

        // 재시작 버튼 보이기
        if (restartButton) restartButton.SetActive(true);

        // (옵션) 게임이 끝났으니 캐릭터들 멈추게 하거나 무적으로 만들기
        // Time.timeScale = 0; // 시간을 멈추고 싶다면 사용
    }

    private void UpdateTimerUI()
    {
        if (timerText)
        {
            // 00:00 포맷 or 단순 초
            timerText.text = $"{Mathf.Max(0, _currentTime):F0}";

            // 시간 얼마 안 남으면 빨간색 경고
            if (_currentTime <= 10f) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }
}
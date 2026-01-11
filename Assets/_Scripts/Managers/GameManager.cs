using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

/*
 *  게임 매니저 클래스 입니다.
 *  게임의 전체 흐름 (시작, 종료, 시간 관리 등)을 담당합니다.
 */
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 111f;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI centerText;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private HudHpBar playerHpBar;
    [SerializeField] private HudHpBar enemyHpBar;
    [SerializeField] private GameObject pannel_blackOut;

    // 글로벌 참조용
    public SkillData skillDatabase;
    [HideInInspector] public GameObject player;
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
        InitGame();
    }

    private void Update()
    {
        if (_currentState == GameState.Playing)
        {
            _currentTime -= Time.deltaTime;
            UpdateTimerUI();

            if (_currentTime <= 0)
            {
                OnGameOver("DRAW", Color.yellow);
            }
        }
    }

    private void InitGame()
    {
        _currentState = GameState.Ready;
        _currentTime = gameDuration;

        if (timerText) timerText.text = $"{_currentTime:F0}";
        if (centerText) centerText.text = "Ready?";

        if (restartButton) restartButton.SetActive(false);
        if (startButton) startButton.SetActive(true);

        if (player != null) Destroy(player);
        if (enemy != null) Destroy(enemy);
    }

    public void OnClickStartGame()
    {
        StartCoroutine(GameFlowRoutine());
    }

    public void OnClickRestart()
    {
        InitGame();
        // 바로 시작하고 싶으면 OnClickStartGame(); 호출
    }

    private IEnumerator GameFlowRoutine()
    {
        if (startButton) startButton.SetActive(false);
        if (restartButton) restartButton.SetActive(false);
        if (centerText) centerText.gameObject.SetActive(true);
        _currentState = GameState.Countdown;

        string[] counts = { "3", "2", "1", "BATTLE!" };
        foreach (var count in counts)
        {
            if (centerText) centerText.text = count;
            if (centerText) centerText.transform.localScale = Vector3.one * 1.5f;

            yield return new WaitForSeconds(1f);
        }

        if (centerText) centerText.text = "";
        if (pannel_blackOut) pannel_blackOut.SetActive(false);

        SpawnCharacters();

        _currentState = GameState.Playing;
    }

    private void SpawnCharacters()
    {
        if (playerPrefab != null && playerSpawnPoint != null)
        {
            player = Instantiate(playerPrefab, playerSpawnPoint.position, Quaternion.identity);

            var character = player.GetComponent<Character>();
            if (character != null)
            {
                if (playerHpBar != null) playerHpBar.Setup(character);

                if (skillUIManager != null) skillUIManager.Setup(player);

                character.Health.OnDead += () => OnGameOver("YOU LOSE", Color.red);
            }
        }

        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            enemy = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);

            var character = enemy.GetComponent<Character>();
            if (character != null)
            {
                if (enemyHpBar != null) enemyHpBar.Setup(character);

                character.Health.OnDead += () => OnGameOver("YOU WIN!", Color.green);
            }
        }
    }

    private void OnGameOver(string message, Color color)
    {
        if (_currentState == GameState.GameOver) return;

        _currentState = GameState.GameOver;

        if (centerText)
        {
            centerText.text = message;
            centerText.color = color;
            centerText.gameObject.SetActive(true);
        }

        if (restartButton) restartButton.SetActive(true);

        // (옵션) 게임이 끝났으니 캐릭터들 멈추게 하거나 무적으로 만들기
        Time.timeScale = 0; // 시간을 멈춤
    }

    private void UpdateTimerUI()
    {
        if (timerText)
        {
            timerText.text = $"{Mathf.Max(0, _currentTime):F0}";

            // 시간 얼마 안 남으면 빨간색 경고
            if (_currentTime <= 10f) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }
}
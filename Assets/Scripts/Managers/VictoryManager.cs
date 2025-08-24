using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject victoryCanvas;
    [SerializeField] private TextMeshProUGUI victoryMessageText;
    [SerializeField] private GameObject relicSelectionPanel;
    [SerializeField] private Transform relicOptionsParent;
    [SerializeField] private GameObject relicOptionPrefab; // Reuse BossRewardOptionUI prefab
    [SerializeField] private Button confirmSelectionButton;

    [Header("Relic Settings")]
    [SerializeField] private List<RelicBase> possibleVictoryRelics; // Relics to offer on victory

    private GameManager gameManager;
    private BossRewardOptionUI selectedRelicOption = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("[VictoryManager] GameManager.Instance not found!");
        }

        if (confirmSelectionButton != null)
        {
            confirmSelectionButton.onClick.RemoveAllListeners();
            confirmSelectionButton.onClick.AddListener(OnConfirmSelectionButtonClick);
            confirmSelectionButton.interactable = false;
        }

        if (victoryCanvas != null) victoryCanvas.SetActive(false);
        if (relicSelectionPanel != null) relicSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// Shows the victory screen and initiates relic selection.
    /// </summary>
    public void ShowVictoryScreen()
    {
        Debug.Log("[VictoryManager] Showing victory screen.");
        Time.timeScale = 0f; // Pause game

        if (victoryCanvas != null) victoryCanvas.SetActive(true);
        if (victoryMessageText != null) victoryMessageText.text = "VICTORY!";

        ShowRelicSelection();
    }

    /// <summary>
    /// Generates and displays relic options for selection.
    /// </summary>
    private void ShowRelicSelection()
    {
        Debug.Log("[VictoryManager] Showing relic selection.");
        if (relicSelectionPanel != null) relicSelectionPanel.SetActive(true);

        ClearRelicOptions();

        if (relicOptionPrefab == null || relicOptionsParent == null)
        {
            Debug.LogError("[VictoryManager] Relic option prefab or parent is not set!");
            return;
        }

        // Generate 3 random relic options from the possibleVictoryRelics pool
        List<RelicBase> availableRelics = new List<RelicBase>(possibleVictoryRelics);
        for (int i = 0; i < 3; i++)
        {
            if (availableRelics.Count == 0)
            {
                Debug.LogWarning("[VictoryManager] Not enough unique relics in the pool to generate 3 options.");
                break;
            }

            int randomIndex = Random.Range(0, availableRelics.Count);
            RelicBase chosenRelic = availableRelics[randomIndex];
            availableRelics.RemoveAt(randomIndex); // Ensure unique options

            GameObject optionObj = Instantiate(relicOptionPrefab, relicOptionsParent);
            BossRewardOptionUI optionUI = optionObj.GetComponent<BossRewardOptionUI>();

            if (optionUI != null)
            {
                // Create a BossRewardOption for the relic
                BossRewardOption rewardOption = new BossRewardOption
                {
                    id = chosenRelic.relicId,
                    displayName = chosenRelic.name,
                    description = "A powerful relic to aid your next run.", // Or get from relic itself
                    icon = null, // Assign relic icon if available
                    slotType = BossRewardSlotType.Trophy, // Treat as a Trophy for UI display
                    relic = chosenRelic
                };
                optionUI.Initialize(rewardOption, null); // Pass null for manager as VictoryManager handles selection
                optionUI.OnOptionSelectedCallback += OnRelicOptionSelected; // Subscribe to selection event
            }
            else
            {
                Debug.LogError("[VictoryManager] Relic option prefab does not have BossRewardOptionUI component!");
            }
        }
    }

    /// <summary>
    /// Clears all displayed relic options.
    /// </summary>
    private void ClearRelicOptions()
    {
        foreach (Transform child in relicOptionsParent)
        {
            Destroy(child.gameObject);
        }
        selectedRelicOption = null;
        if (confirmSelectionButton != null) confirmSelectionButton.interactable = false;
    }

    /// <summary>
    /// Callback when a relic option is selected.
    /// </summary>
    private void OnRelicOptionSelected(BossRewardOptionUI selectedUI)
    {
        if (selectedRelicOption != null) selectedRelicOption.SetSelected(false);
        selectedRelicOption = selectedUI;
        selectedRelicOption.SetSelected(true);
        if (confirmSelectionButton != null) confirmSelectionButton.interactable = true;
    }

    /// <summary>
    /// Handles the confirm button click.
    /// </summary>
    private void OnConfirmSelectionButtonClick()
    {
        if (selectedRelicOption == null || gameManager == null || gameManager.RelicManager == null)
        {
            Debug.LogWarning("[VictoryManager] No relic selected or managers not found.");
            return;
        }

        RelicBase chosenRelic = selectedRelicOption.GetRewardOption().relic;
        gameManager.RelicManager.Acquire(chosenRelic); // Acquire the chosen relic
        Debug.Log($"[VictoryManager] Acquired relic: {chosenRelic.name}");

        HideVictoryScreen();
        // TODO: Transition to main menu or next game phase
    }

    /// <summary>
    /// Hides the victory screen and resumes game time.
    /// </summary>
    private void HideVictoryScreen()
    {
        if (victoryCanvas != null) victoryCanvas.SetActive(false);
        if (relicSelectionPanel != null) relicSelectionPanel.SetActive(false);
        Time.timeScale = 1f; // Resume game
        ClearRelicOptions();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (confirmSelectionButton != null)
        {
            confirmSelectionButton.onClick.RemoveAllListeners();
        }
    }
}
using UnityEngine;

/// <summary>
/// SafeArea component for handling Android notch and gesture areas
/// Automatically adjusts RectTransform to fit within device safe area
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    [Header("Safe Area Settings")]
    [SerializeField] private bool applyTop = true;
    [SerializeField] private bool applyBottom = true;
    [SerializeField] private bool applyLeft = true;
    [SerializeField] private bool applyRight = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2Int lastScreenSize = new Vector2Int(0, 0);
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    
    void Start()
    {
        ApplySafeArea();
    }
    
    void Update()
    {
        // Check if screen size or safe area has changed
        if (Screen.safeArea != lastSafeArea || 
            new Vector2Int(Screen.width, Screen.height) != lastScreenSize)
        {
            ApplySafeArea();
        }
    }
    
    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        
        // Convert to normalized coordinates
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        
        // Apply settings
        if (!applyLeft) anchorMin.x = 0f;
        if (!applyRight) anchorMax.x = 1f;
        if (!applyBottom) anchorMin.y = 0f;
        if (!applyTop) anchorMax.y = 1f;
        
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Update cached values
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        
        if (showDebugInfo)
        {
            Debug.Log($"SafeArea Applied - Screen: {Screen.width}x{Screen.height}, " +
                     $"SafeArea: {safeArea}, AnchorMin: {anchorMin}, AnchorMax: {anchorMax}");
        }
    }
    
    /// <summary>
    /// Force refresh safe area (useful when called from other scripts)
    /// </summary>
    public void RefreshSafeArea()
    {
        ApplySafeArea();
    }
    
    /// <summary>
    /// Get current safe area in screen pixels
    /// </summary>
    public Rect GetSafeAreaRect()
    {
        return Screen.safeArea;
    }
    
    /// <summary>
    /// Check if device has notch or cutout areas
    /// </summary>
    public bool HasNotchOrCutout()
    {
        Rect safeArea = Screen.safeArea;
        return safeArea.x > 0 || safeArea.y > 0 || 
               safeArea.width < Screen.width || 
               safeArea.height < Screen.height;
    }
}
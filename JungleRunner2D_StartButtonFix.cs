using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Runtime fallback for a visible START RUN button that does not respond.
/// Add this component to the Jungle Runner 2D Game Controller.
/// It finds the button by its displayed label and hooks it to common gameplay
/// start methods on components in the scene.
/// </summary>
[DisallowMultipleComponent]
public class JungleRunner2DStartButtonFix : MonoBehaviour
{
    private Button hookedButton;

    private static readonly string[] StartMethodNames =
    {
        "StartRun", "StartGame", "BeginRun", "StartGameplay",
        "StartGameLoop", "PlayGame", "OnStartRun", "BeginGame"
    };

    private void Awake()
    {
        EnsureEventSystem();
    }

    private void Update()
    {
        if (hookedButton != null)
            return;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (IsStartButton(button))
            {
                hookedButton = button;
                hookedButton.interactable = true;
                hookedButton.onClick.AddListener(HandleStartClicked);
                Debug.Log("[Jungle Runner] START RUN button connected by JungleRunner2DStartButtonFix.");
                break;
            }
        }
    }

    private bool IsStartButton(Button button)
    {
        if (button == null) return false;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null && tmp.text.IndexOf("START RUN", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        Text legacy = button.GetComponentInChildren<Text>(true);
        if (legacy != null && legacy.text.IndexOf("START RUN", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return button.name.IndexOf("START", StringComparison.OrdinalIgnoreCase) >= 0
            && button.name.IndexOf("RUN", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void HandleStartClicked()
    {
        bool invoked = false;
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this) continue;

            Type type = behaviour.GetType();
            foreach (string methodName in StartMethodNames)
            {
                MethodInfo method = type.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);

                if (method == null || method.ReturnType != typeof(void))
                    continue;

                try
                {
                    method.Invoke(behaviour, null);
                    Debug.Log($"[Jungle Runner] Invoked {type.Name}.{methodName}()");
                    invoked = true;
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Jungle Runner] Could not invoke {type.Name}.{methodName}(): {ex.Message}");
                }
            }

            if (invoked) break;
        }

        if (!invoked)
        {
            Debug.LogError(
                "[Jungle Runner] START RUN was clicked, but no start method was found. " +
                "Open Console and share the first red error, plus the JungleRunner2DGame.cs code.");
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        Debug.Log("[Jungle Runner] Added missing EventSystem for UI clicks.");
    }
}

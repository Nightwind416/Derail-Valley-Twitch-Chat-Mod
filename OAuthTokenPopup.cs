using UnityEngine;

public class OAuthTokenPopup : MonoBehaviour
{
    private static string oauthToken = string.Empty;
    private static bool showPopup = false;

    public static void ShowToken(string token)
    {
        oauthToken = token;
        showPopup = true;
    }

    private void OnGUI()
    {
        if (showPopup)
        {
            // Create a simple popup window
            Rect windowRect = new(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 150);
            GUI.ModalWindow(0, windowRect, DrawWindow, "OAuth Token");
        }
    }

    private void DrawWindow(int windowID)
    {
        GUILayout.Label("OAuth Token:");
        GUILayout.TextField(oauthToken, GUILayout.Height(50));
        if (GUILayout.Button("Close"))
        {
            showPopup = false;
        }
    }
}
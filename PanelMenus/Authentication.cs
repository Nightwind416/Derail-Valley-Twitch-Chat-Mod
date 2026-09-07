using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Panel for linking and unlinking the player's Twitch account.
    /// </summary>
    /// <remarks>
    /// Deliberately spells out what is being authorized before anything is requested. The consent
    /// step exists because a player is being asked to hand a mod access to their Twitch account,
    /// and they should be able to read exactly what that means without leaving the game or trusting
    /// a summary written somewhere else.
    /// </remarks>
    public class AuthenticationPanel : PanelConstructor.BasePanel
    {
        /// <summary>Which screen of the panel the player is looking at.</summary>
        private enum Screen
        {
            /// <summary>Reflects whatever <see cref="OAuthTokenManager.Phase"/> currently is.</summary>
            Status,
            /// <summary>The explanation shown before any request is made to Twitch.</summary>
            Consent
        }

        /// <summary>
        /// Height of both sections. They fill the panel so the consent text, which is the longest
        /// thing either screen has to show, is laid out without being truncated.
        /// </summary>
        private const int SectionHeight = 400;

        private Screen screen = Screen.Status;

        private GameObject? statusSection;
        private GameObject? consentSection;

        private Text? statusText;
        private Text? accountText;
        private Text? codeText;
        private Text? codeHintText;
        private Text? storageText;

        private Button? connectButton;
        private Button? revokeButton;
        private Button? cancelButton;

        public AuthenticationPanel(Transform parent) : base(parent)
        {
            CreateStatusSection();
            CreateConsentSection();
            UpdateAuthenticationPanelValues();
        }

        /// <summary>
        /// Builds the default screen: who is connected, and the controls to connect or disconnect.
        /// </summary>
        private void CreateStatusSection()
        {
            statusSection = PanelConstructor.Section.Create(panelObject.transform, "Twitch Account", 25, SectionHeight);

            accountText = PanelConstructor.DisplayText.Create(statusSection.transform, "Not connected", 10, 28, Color.yellow, 1, 13);
            statusText = PanelConstructor.DisplayText.Create(statusSection.transform, OAuthTokenManager.StatusMessage, 10, 48, Color.cyan, 2, 11);

            // The pairing code, shown large because the player is reading it off an in-game panel,
            // in VR, and typing it on a phone.
            codeText = PanelConstructor.DisplayText.Create(statusSection.transform, "", 10, 95, Color.white, 1, 22);
            codeHintText = PanelConstructor.DisplayText.Create(statusSection.transform, "", 10, 135, Color.gray, 3, 11);

            connectButton = PanelConstructor.Button.Create(statusSection.transform,
                "Connect Twitch Account", 105, 200, Color.white,
                () => ShowScreen(Screen.Consent), 190);

            cancelButton = PanelConstructor.Button.Create(statusSection.transform,
                "Cancel", 105, 200, Color.white,
                () =>
                {
                    OAuthTokenManager.CancelDeviceAuthorization();
                    OAuthTokenManager.SetPhase(AuthPhase.NotConnected, "Not connected");
                    UpdateAuthenticationPanelValues();
                }, 190);

            revokeButton = PanelConstructor.Button.Create(statusSection.transform,
                "Sign Out & Revoke Access", 105, 228, Color.white,
                () =>
                {
                    _ = OAuthTokenManager.RevokeAndSignOut();
                }, 190);

            storageText = PanelConstructor.DisplayText.Create(statusSection.transform, "", 10, 348, Color.gray, 4, 10);
        }

        /// <summary>
        /// Builds the consent screen, which lists in plain language what the mod is about to ask
        /// Twitch for, what it will not be able to do, and where the resulting token is kept.
        /// </summary>
        private void CreateConsentSection()
        {
            consentSection = PanelConstructor.Section.Create(panelObject.transform, "What you are approving", 25, SectionHeight);

            int y = 26;

            PanelConstructor.DisplayText.Create(consentSection.transform,
                "Approved on Twitch's own site. The mod never sees your password.",
                10, y, Color.white, 3, 11);
            y += 44;

            PanelConstructor.DisplayText.Create(consentSection.transform, "It will be able to:", 10, y, Color.green, 1, 11);
            y += 14;

            foreach (string allowed in OAuthTokenManager.ScopeExplanations)
            {
                PanelConstructor.DisplayText.Create(consentSection.transform, "- " + allowed, 12, y, Color.white, 2, 10);
                y += 24;
            }

            y += 6;
            PanelConstructor.DisplayText.Create(consentSection.transform, "It cannot:", 10, y, Color.red, 1, 11);
            y += 14;

            foreach (string denied in OAuthTokenManager.ScopeExclusions)
            {
                PanelConstructor.DisplayText.Create(consentSection.transform, "- " + denied, 12, y, Color.gray, 2, 10);
                y += 24;
            }

            y += 6;
            PanelConstructor.DisplayText.Create(consentSection.transform,
                "Your access key stays on this PC and is sent only to Twitch. The mod author cannot see it.",
                 10, y, Color.white, 4, 10);
            y += 54;

            PanelConstructor.Button.Create(consentSection.transform,
                "Continue to Twitch", 105, y, Color.white,
                () =>
                {
                    ShowScreen(Screen.Status);
                    _ = OAuthTokenManager.StartDeviceAuthorization();
                }, 190);

            PanelConstructor.Button.Create(consentSection.transform,
                "Cancel", 105, y + 26, Color.white,
                () => ShowScreen(Screen.Status), 190);
        }

        /// <summary>
        /// Switches between the status and consent screens.
        /// </summary>
        private void ShowScreen(Screen next)
        {
            screen = next;
            UpdateAuthenticationPanelValues();
        }

        /// <inheritdoc/>
        public override void Tick(float deltaTime) => UpdateAuthenticationPanelValues();

        /// <summary>
        /// Brings every control on the panel into line with the current authentication state.
        /// Called from the menu update loop, so it must stay cheap and allocation-free.
        /// </summary>
        public void UpdateAuthenticationPanelValues()
        {
            if (statusSection == null || consentSection == null || statusText == null || accountText == null ||
                codeText == null || codeHintText == null || storageText == null || connectButton == null ||
                revokeButton == null || cancelButton == null)
            {
                return;
            }

            bool showConsent = screen == Screen.Consent;
            statusSection.SetActive(!showConsent);
            consentSection.SetActive(showConsent);

            if (showConsent)
            {
                return;
            }

            AuthPhase phase = OAuthTokenManager.Phase;
            bool hasToken = !string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken);
            bool pairing = phase == AuthPhase.AwaitingUser && !string.IsNullOrEmpty(OAuthTokenManager.UserCode);

            accountText.text = phase == AuthPhase.Connected && !string.IsNullOrEmpty(Settings.Instance.twitchUsername)
                ? Settings.Instance.twitchUsername
                : "Not connected";
            accountText.color = phase == AuthPhase.Connected ? Color.green : Color.yellow;

            statusText.text = OAuthTokenManager.StatusMessage;
            statusText.color = phase switch
            {
                AuthPhase.Connected => Color.green,
                AuthPhase.Failed => Color.red,
                AuthPhase.AwaitingUser => Color.cyan,
                _ => Color.yellow
            };

            if (pairing)
            {
                codeText.text = OAuthTokenManager.UserCode;
                codeHintText.text = $"Go to {OAuthTokenManager.ActivationPage} on any device and enter this code.";
            }
            else
            {
                codeText.text = string.Empty;
                codeHintText.text = string.Empty;
            }

            // While a code is pending, Cancel replaces Connect rather than sitting beside it.
            connectButton.gameObject.SetActive(!pairing && phase != AuthPhase.Connected);
            cancelButton.gameObject.SetActive(pairing);
            revokeButton.gameObject.SetActive(hasToken && !pairing);

            connectButton.GetComponentInChildren<Text>().text = phase == AuthPhase.Failed
                ? "Try Again"
                : "Connect Twitch Account";

            storageText.text = TokenStore.IsEncrypted
                ? "Access key is encrypted on this PC (Windows DPAPI)."
                : "Access key is stored encoded on this PC; encryption is unavailable on this system.";
        }

        /// <summary>
        /// Controls the visibility of the panel and its sections.
        /// </summary>
        public override void Show()
        {
            base.Show();
            UpdateAuthenticationPanelValues();
        }

        /// <summary>
        /// Leaving the panel drops back to the status screen, so a half-read consent
        /// screen is never what greets the player next time.
        /// </summary>
        public override void Hide()
        {
            screen = Screen.Status;
            base.Hide();
        }
    }
}

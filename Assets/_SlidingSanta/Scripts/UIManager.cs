using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;
using UnityEngine.SocialPlatforms;

#if EASY_MOBILE
using EasyMobile;
#endif

namespace SgLib
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        public static bool firstLoad = true;

        [Header("Object References")]
        public GameObject mainCanvas;
        public GameObject characterSelectionUI;
        public GameObject instruction;
        public GameObject header;
        public GameObject title;
        public Text score;
        public Text bestScore;
        public Text coinText;
        public GameObject newBestScore;
        public GameObject playBtn;
        public GameObject characterSelectBtn;
        public GameObject menuButtons;
        public GameObject dailyRewardBtn;
        public Text dailyRewardBtnText;
        public GameObject rewardUI;
        public GameObject settingsUI;
        public GameObject soundOnBtn;
        public GameObject soundOffBtn;
        public GameObject musicOnBtn;
        public GameObject musicOffBtn;

        [Header("Premium Features Buttons")]
        public GameObject watchRewardedAdBtn;
        public GameObject leaderboardBtn;
        public GameObject achievementBtn;
        public GameObject iapPurchaseBtn;
        public GameObject removeAdsBtn;
        public GameObject restorePurchaseBtn;
        public bool isBannerOn = false;

        [Header("In-App Purchase Store")]
        public GameObject storeUI;

        [Header("Sharing-Specific")]
        public GameObject shareUI;
        public ShareUIController shareUIController;

        const string TUTORIAL_AUTO_DISPLAY_PPK = "SGLIB_TUTORIAL_AUTO_DISPLAY";
        Animator scoreAnimator;
        Animator dailyRewardAnimator = null;
        bool isWatchAdsForCoinBtnActive;
        bool isAutoDisplayingTutorial;

        void OnEnable()
        {
            GameManager.GameStateChanged += GameManager_GameStateChanged;
            ScoreManager.ScoreUpdated += OnScoreUpdated;
        }

        void OnDisable()
        {
            GameManager.GameStateChanged -= GameManager_GameStateChanged;
            ScoreManager.ScoreUpdated -= OnScoreUpdated;
        }

        // Use this for initialization
        void Start()
        {
            scoreAnimator = score.GetComponent<Animator>();
            dailyRewardAnimator = dailyRewardBtn.GetComponent<Animator>();
            Instance = this;
            Reset();
            ShowStartUI();
        }

        // Update is called once per frame
        void Update()
        {
            score.text = ScoreManager.Instance.Score.ToString();
            bestScore.text = ScoreManager.Instance.HighScore.ToString();
            coinText.text = CoinManager.Instance.Coins.ToString();

            if (!DailyRewardController.Instance.disable && dailyRewardBtn.gameObject.activeInHierarchy)
            {
                if (DailyRewardController.Instance.CanRewardNow())
                {
                    dailyRewardBtnText.text = "GRAB YOUR REWARD!";
                    dailyRewardAnimator.SetTrigger("activate");
                }
                else
                {
                    TimeSpan timeToReward = DailyRewardController.Instance.TimeUntilReward;
                    dailyRewardBtnText.text = string.Format("REWARD IN {0:00}:{1:00}:{2:00}", timeToReward.Hours, timeToReward.Minutes, timeToReward.Seconds);
                    dailyRewardAnimator.SetTrigger("deactivate");
                }
            }

            if (settingsUI.activeInHierarchy)
            {
                UpdateMuteButtons();
                UpdateMusicButtons();
            }
        }

        void GameManager_GameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Playing)
            {
                ShowGameUI();
            }
            else if (newState == GameState.PreGameOver)
            {
                // Before game over, i.e. game potentially will be recovered
            }
            else if (newState == GameState.GameOver)
            {
                Invoke("ShowGameOverUI", 1f);
            }
        }

        void Reset()
        {
            mainCanvas.SetActive(true);
            characterSelectionUI.SetActive(false);
            header.SetActive(false);
            title.SetActive(false);
            score.gameObject.SetActive(false);
            newBestScore.SetActive(false);
            playBtn.SetActive(false);
            characterSelectBtn.SetActive(false);
            menuButtons.SetActive(false);
            dailyRewardBtn.SetActive(false);
            instruction.SetActive(false);

            // Enable or disable premium stuff
            bool enablePremium = IsPremiumFeaturesEnabled();
            leaderboardBtn.SetActive(true);
            iapPurchaseBtn.SetActive(true);
            removeAdsBtn.SetActive(true);
            restorePurchaseBtn.SetActive(true);

            // Hidden by default
            storeUI.SetActive(false);
            settingsUI.SetActive(false);
            shareUI.SetActive(false);

            // These premium feature buttons are hidden by default
            // and shown when certain criteria are met (e.g. rewarded ad is loaded)
            //watchRewardedAdBtn.gameObject.SetActive(false);
        }

        public void StartGame()
        {
            if (GameManager.Instance.GameState == GameState.Prepare)
            {
                if (!HasDisplayedTutorial())
                {
                    ShowGameUI();
                    ShowTutorial();
                    isAutoDisplayingTutorial = true;
                }
                else
                {
                    GameManager.Instance.StartGame();
                }
            }
            else if (GameManager.Instance.GameState == GameState.GameOver)
            {
                GameManager.Instance.RestartGame(0.2f);
            }
        }

        public void EndGame()
        {
            GameManager.Instance.GameOver();
        }

        public void RestartGame()
        {
            GameManager.Instance.RestartGame(0.2f);
        }

        public void ShowTutorial()
        {
            instruction.SetActive(true);
            RememberDisplayedTutorial();
        }

        public void CloseTutorial()
        {
            instruction.SetActive(false);
            if (isAutoDisplayingTutorial)
            {
                isAutoDisplayingTutorial = false;
                StartGame();
            }
        }

        //Show Leaderboard
        public void ShowLeaderboard()
        {
            PlayGamesScript.ShowLeaderboardsUI();
        }

        public void ShowStartUI()
        {
            settingsUI.SetActive(false);

            header.SetActive(true);
            title.SetActive(true);
            playBtn.SetActive(true);
            characterSelectBtn.SetActive(true);
            menuButtons.SetActive(true);
            shareUI.SetActive(false);

            // If first launch: show "WatchForCoins" and "DailyReward" buttons if the conditions are met
            if (GameManager.GameCount == 0)
            {
                ShowWatchForCoinsBtn();
                ShowDailyRewardBtn();
            }
        }

        public void ShowGameUI()
        {
            header.SetActive(true);
            title.SetActive(false);
            score.gameObject.SetActive(true);
            playBtn.SetActive(false);
            characterSelectBtn.SetActive(false);
            menuButtons.SetActive(false);
            dailyRewardBtn.SetActive(false);
            watchRewardedAdBtn.SetActive(false);
        }

        public void ShowGameOverUI()
        {
            header.SetActive(true);
            title.SetActive(false);
            score.gameObject.SetActive(true);
            newBestScore.SetActive(ScoreManager.Instance.HasNewHighScore);

            playBtn.SetActive(true);
            characterSelectBtn.SetActive(true);
            menuButtons.SetActive(true);
            settingsUI.SetActive(false);
            ShowWatchForCoinsBtn();
            // Show 'daily reward' button
            ShowDailyRewardBtn();
            
            //ShowShareUI();
            // Show these if premium features are enabled (and relevant conditions are met)
            if (IsPremiumFeaturesEnabled())
            {
                
                
            }
        }

        void RememberDisplayedTutorial()
        {
            // Set any value other than 0
            PlayerPrefs.SetInt(TUTORIAL_AUTO_DISPLAY_PPK, 1);
        }

        bool HasDisplayedTutorial()
        {
            return PlayerPrefs.GetInt(TUTORIAL_AUTO_DISPLAY_PPK, 0) != 0;
        }

        void ShowWatchForCoinsBtn()
        {
            // Only show "watch for coins button" if a rewarded ad is loaded and premium features are enabled
            watchRewardedAdBtn.SetActive(true);
            watchRewardedAdBtn.GetComponent<Animator>().SetTrigger("activate");
#if EASY_MOBILE
            if (IsPremiumFeaturesEnabled() && AdDisplayer.Instance.CanShowRewardedAd() && AdDisplayer.Instance.watchAdToEarnCoins)
            {
                watchRewardedAdBtn.SetActive(true);
                watchRewardedAdBtn.GetComponent<Animator>().SetTrigger("activate");
            }
            else
            {
                watchRewardedAdBtn.SetActive(false);
            }
#endif
        }

        void ShowDailyRewardBtn()
        {
            // Not showing the daily reward button if the feature is disabled
            if (!DailyRewardController.Instance.disable)
            {
                dailyRewardBtn.SetActive(true);
            }
        }

        public void ShowSettingsUI()
        {
            settingsUI.SetActive(true);
        }

        public void HideSettingsUI()
        {
            settingsUI.SetActive(false);
        }

        public void ShowStoreUI()
        {
            storeUI.SetActive(true);
        }

        public void HideStoreUI()
        {
            storeUI.SetActive(false);
        }

        public void ShowCharacterSelectionScene()
        {
            mainCanvas.SetActive(false);
            characterSelectionUI.SetActive(true);
        }

        public void CloseCharacterSelectionScene()
        {
            mainCanvas.SetActive(true);
            characterSelectionUI.SetActive(false);
        }

        public void WatchRewardedAd()
        {
#if EASY_MOBILE
            // Hide the button
            watchRewardedAdBtn.SetActive(false);

            AdDisplayer.CompleteRewardedAdToEarnCoins += OnCompleteRewardedAdToEarnCoins;
            AdDisplayer.Instance.ShowRewardedAdToEarnCoins();
#endif
        }

        void OnCompleteRewardedAdToEarnCoins()
        {
#if EASY_MOBILE
            // Unsubscribe
            AdDisplayer.CompleteRewardedAdToEarnCoins -= OnCompleteRewardedAdToEarnCoins;

            // Give the coins!
            ShowRewardUI(AdDisplayer.Instance.rewardedCoins);
#endif
        }

        public void GrabDailyReward()
        {
            if (DailyRewardController.Instance.CanRewardNow())
            {
                int reward = DailyRewardController.Instance.GetRandomReward();

                // Round the number and make it mutiplies of 5 only.
                int roundedReward = (reward / 5) * 5;

                // Show the reward UI
                ShowRewardUI(roundedReward);

                // Update next time for the reward
                DailyRewardController.Instance.ResetNextRewardTime();
            }
        }

        public void GrabVideoReward()
        {
            ShowRewardUI(30);
            //CoinManager.Instance.AddCoins(50);
        }

        public void ShowRewardUI(int reward)
        {
            rewardUI.SetActive(true);
            rewardUI.GetComponent<RewardUIController>().Reward(reward);
        }

        public void HideRewardUI()
        {
            rewardUI.GetComponent<RewardUIController>().Close();
        }

        public void ShowLeaderboardUI()
        {
#if EASY_MOBILE
            if (GameServiceManager.IsInitialized())
            {
                GameServiceManager.ShowLeaderboardUI();
            }
            else
            {
#if UNITY_IOS
                MobileNativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
#elif UNITY_ANDROID
                GameServiceManager.Init();
#endif
            }
#endif
        }

        public void ShowAchievementsUI()
        {
#if EASY_MOBILE
            if (GameServiceManager.IsInitialized())
            {
                GameServiceManager.ShowAchievementsUI();
            }
            else
            {
#if UNITY_IOS
                MobileNativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
#elif UNITY_ANDROID
                GameServiceManager.Init();
#endif
            }
#endif
        }

        public void PurchaseRemoveAds()
        {
#if EASY_MOBILE
            InAppPurchaser.Instance.Purchase(InAppPurchaser.Instance.removeAds);
#endif
        }

        public void RestorePurchase()
        {
#if EASY_MOBILE
            InAppPurchaser.Instance.RestorePurchase();
#endif
        }

        public void ShowShareUI()
        {
            if (!ScreenshotSharer.Instance.disableSharing)
            {
                Texture2D texture = ScreenshotSharer.Instance.CapturedScreenshot;
                shareUIController.ImgTex = texture;

#if EASY_MOBILE
                AnimatedClip clip = ScreenshotSharer.Instance.RecordedClip;
                shareUIController.AnimClip = clip;
#endif

                shareUI.SetActive(true);
            }
        }

        public void HideShareUI()
        {
            shareUI.SetActive(false);
        }

        public void ToggleSound()
        {
            SoundManager.Instance.ToggleSound();
        }

        public void ToggleMusic()
        {
            SoundManager.Instance.ToggleMusic();
        }

        public void RateApp()
        {
            Utilities.RateApp();
        }

        public void OpenTwitterPage()
        {
            Utilities.OpenTwitterPage();
        }

        public void OpenFacebookPage()
        {
            Utilities.OpenFacebookPage();
        }

        public void ButtonClickSound()
        {
            Utilities.ButtonClickSound();
        }

        void UpdateMuteButtons()
        {
            if (SoundManager.Instance.IsSoundOff())
            {
                soundOnBtn.gameObject.SetActive(false);
                soundOffBtn.gameObject.SetActive(true);
            }
            else
            {
                soundOnBtn.gameObject.SetActive(true);
                soundOffBtn.gameObject.SetActive(false);
            }
        }

        void UpdateMusicButtons()
        {
            if (SoundManager.Instance.IsMusicOff())
            {
                musicOffBtn.gameObject.SetActive(true);
                musicOnBtn.gameObject.SetActive(false);
            }
            else
            {
                musicOffBtn.gameObject.SetActive(false);
                musicOnBtn.gameObject.SetActive(true);
            }
        }

        bool IsPremiumFeaturesEnabled()
        {
            return PremiumFeaturesManager.Instance != null && PremiumFeaturesManager.Instance.enablePremiumFeatures;
        }

        void OnScoreUpdated(int newScore)
        {
            scoreAnimator.Play("NewScore");
        }
    }
}

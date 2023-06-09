﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using Unity.Services.Core;
using System.Threading.Tasks;
using Unity.Services.Mediation;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

namespace Unity.Services.Mediation.Samples
{
    public class ShowAds : MonoBehaviour
    {
        [Header("Ad Unit Ids"), Tooltip("Android Ad Unit Ids")]
        public string androidAdUnitId;
        [Tooltip("iOS Ad Unit Ids")]
        public string iosAdUnitId;

        [Header("Game Ids"), Tooltip("[Optional] Specifies the iOS GameId. Otherwise uses the GameId of the linked project.")]
        public string iosGameId;
        [Tooltip("[Optional] Specifies the Android GameId. Otherwise uses the GameId of the linked project.")]
        public string androidGameId;

        IRewardedAd m_RewardedAd;

        public static bool dontShow, shouldShowRewardedAd, poppedUp, skipPoppedUp;
        bool changeLevel;
        [SerializeField] SpriteRenderer[] popup = new SpriteRenderer[8];

        [SerializeField] SpriteRenderer[] skipPopup = new SpriteRenderer[8];

        void Awake()
        {
            dontShow = false;
            poppedUp = false;
            skipPoppedUp = false;
            shouldShowRewardedAd = false;
            changeLevel = false;

#if UNITY_IOS
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
            ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {

                ATTrackingStatusBinding.RequestAuthorizationTracking();

            }
#endif
        }

        // Start is called before the first frame update
        async void Start()
        {
            try
            {
                Debug.Log("Initializing...");
                await UnityServices.InitializeAsync(GetGameId());
                Debug.Log("Initialized!");
                InitializationComplete();
            }
            catch (Exception e)
            {
                InitializationFailed(e);
            }

            if (PlayerPrefs.GetInt("FailedInARow", 0) >= 3)
            {
                skipPoppedUp = true;
                StartCoroutine(ShowSkipPopUp());
                PlayerPrefs.SetInt("FailedInARow", 0);
                PlayerPrefs.SetInt("GamesSinceLastAdPop", 0);
            }

            if (PlayerPrefs.GetInt("GamesSinceLastAdPop", 0) >= 2 && PlayerPrefs.GetInt("GameSinceLastAd", 0) >= 7)
            {
                // show pop-up
                poppedUp = true;
                StartCoroutine(ShowPopUp());

                PlayerPrefs.SetInt("GamesSinceLastAdPop", 0);
            }
            else
            {
                PlayerPrefs.SetInt("GamesSinceLastAdPop", PlayerPrefs.GetInt("GamesSinceLastAdPop", 0) + 1);
            }
            PlayerPrefs.SetInt("GameSinceLastAd", PlayerPrefs.GetInt("GameSinceLastAd", 6) + 1);
        }

        void OnDestroy()
        {
            m_RewardedAd?.Dispose();
        }

        InitializationOptions GetGameId()
        {
            var initializationOptions = new InitializationOptions();

#if UNITY_IOS
            if (!string.IsNullOrEmpty(iosGameId))
            {
                initializationOptions.SetGameId(iosGameId);
            }
#elif UNITY_ANDROID
        if (!string.IsNullOrEmpty(androidGameId))
        {
            initializationOptions.SetGameId(androidGameId);
        }
#endif

            return initializationOptions;
        }

        public async void ShowRewarded()
        {
            if (m_RewardedAd?.AdState == AdState.Loaded)
            {
                try
                {
                    var showOptions = new RewardedAdShowOptions { AutoReload = true };
                    await m_RewardedAd.ShowAsync(showOptions);
                    Debug.Log("Rewarded Shown!");
                }
                catch (ShowFailedException e)
                {
                    Debug.LogWarning($"Rewarded failed to show: {e.Message}");
                }
            }
        }

        public async void ShowRewardedWithOptions()
        {
            if (m_RewardedAd?.AdState == AdState.Loaded)
            {
                try
                {
                    //Here we provide a user id and custom data for server to server validation.
                    RewardedAdShowOptions showOptions = new RewardedAdShowOptions();
                    showOptions.AutoReload = true;
                    S2SRedeemData s2SData;
                    s2SData.UserId = "my cool user id";
                    s2SData.CustomData = "{\"reward\":\"Gems\",\"amount\":20}";
                    showOptions.S2SData = s2SData;

                    await m_RewardedAd.ShowAsync(showOptions);
                    Debug.Log("Rewarded Shown!");
                }
                catch (ShowFailedException e)
                {
                    Debug.LogWarning($"Rewarded failed to show: {e.Message}");
                }
            }
        }

        async void LoadAd()
        {
            try
            {
                await m_RewardedAd.LoadAsync();
            }
            catch (LoadFailedException)
            {
                // We will handle the failure in the OnFailedLoad callback
            }
        }

        void InitializationComplete()
        {
            // Impression Event
            MediationService.Instance.ImpressionEventPublisher.OnImpression += ImpressionEvent;

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    m_RewardedAd = MediationService.Instance.CreateRewardedAd(androidAdUnitId);
                    break;

                case RuntimePlatform.IPhonePlayer:
                    m_RewardedAd = MediationService.Instance.CreateRewardedAd(iosAdUnitId);
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    m_RewardedAd = MediationService.Instance.CreateRewardedAd(!string.IsNullOrEmpty(androidAdUnitId) ? androidAdUnitId : iosAdUnitId);
                    break;
                default:
                    Debug.LogWarning("Mediation service is not available for this platform:" + Application.platform);
                    return;
            }

            // Load Events
            m_RewardedAd.OnLoaded += AdLoaded;
            m_RewardedAd.OnFailedLoad += AdFailedLoad;

            // Show Events
            m_RewardedAd.OnUserRewarded += UserRewarded;
            m_RewardedAd.OnClosed += AdClosed;

            Debug.Log($"Initialized On Start. Loading Ad...");

            LoadAd();
        }

        void InitializationFailed(Exception error)
        {
            SdkInitializationError initializationError = SdkInitializationError.Unknown;
            if (error is InitializeFailedException initializeFailedException)
            {
                initializationError = initializeFailedException.initializationError;
            }
            Debug.Log($"Initialization Failed: {initializationError}:{error.Message}");
        }

        void UserRewarded(object sender, RewardEventArgs e)
        {
            PlayerPrefs.SetInt("GameSinceLastAd", 0);
            if (changeLevel)
            {
                PlayerPrefs.SetInt("SpawnNewLevel", 1);
                PlayerPrefs.SetInt("levelCount", PlayerPrefs.GetInt("levelCount", 1) + 1);
                changeLevel = false;
                GameManager.reloadLevel = true;
            }
            Debug.Log($"User Rewarded! Type: {e.Type} Amount: {e.Amount}");
        }

        void AdClosed(object sender, EventArgs args)
        {
            Debug.Log("Rewarded Closed! Loading Ad...");
        }

        void AdLoaded(object sender, EventArgs e)
        {
            Debug.Log("Ad loaded");
        }

        void AdFailedLoad(object sender, LoadErrorEventArgs e)
        {
            Debug.Log("Failed to load ad");
            Debug.Log(e.Message);
        }

        void ImpressionEvent(object sender, ImpressionEventArgs args)
        {
            var impressionData = args.ImpressionData != null ? JsonUtility.ToJson(args.ImpressionData, true) : "null";
            Debug.Log($"Impression event from ad unit id {args.AdUnitId} : {impressionData}");
        }

        IEnumerator ShowPopUp()
        {
            float timer = 0, totalTime = 24;
            while (timer <= totalTime)
            {
                for (int i = 0; i < popup.Length; i++)
                {
                    popup[i].color = Color.Lerp(new Color(popup[i].color.r, popup[i].color.g, popup[i].color.b, 0),
                                            new Color(popup[i].color.r, popup[i].color.g, popup[i].color.b, 1), timer / totalTime);
                }
                yield return new WaitForFixedUpdate();
                timer++;
            }
        }

        IEnumerator HidePopUp()
        {
            float timer = 0, totalTime = 24;
            poppedUp = false;
            while (timer <= totalTime)
            {
                for (int i = 0; i < popup.Length; i++)
                {
                    popup[i].color = Color.Lerp(new Color(popup[i].color.r, popup[i].color.g, popup[i].color.b, 1),
                                            new Color(popup[i].color.r, popup[i].color.g, popup[i].color.b, 0), timer / totalTime);
                }
                yield return new WaitForFixedUpdate();
                timer++;
            }
        }

        IEnumerator ShowSkipPopUp()
        {
            float timer = 0, totalTime = 24;
            changeLevel = true;
            while (timer <= totalTime)
            {
                for (int i = 0; i < skipPopup.Length; i++)
                {
                    skipPopup[i].color = Color.Lerp(new Color(skipPopup[i].color.r, skipPopup[i].color.g, skipPopup[i].color.b, 0),
                                            new Color(skipPopup[i].color.r, skipPopup[i].color.g, skipPopup[i].color.b, 1), timer / totalTime);
                }
                yield return new WaitForFixedUpdate();
                timer++;
            }
        }

        IEnumerator HideSkipPopUp()
        {
            float timer = 0, totalTime = 24;
            skipPoppedUp = false;
            while (timer <= totalTime)
            {
                for (int i = 0; i < skipPopup.Length; i++)
                {
                    skipPopup[i].color = Color.Lerp(new Color(skipPopup[i].color.r, skipPopup[i].color.g, skipPopup[i].color.b, 1),
                                            new Color(skipPopup[i].color.r, skipPopup[i].color.g, skipPopup[i].color.b, 0), timer / totalTime);
                }
                yield return new WaitForFixedUpdate();
                timer++;
            }
        }

        private void Update()
        {
            if (shouldShowRewardedAd)
            {
                if (skipPoppedUp)
                    StartCoroutine(HideSkipPopUp());
                else
                    StartCoroutine(HidePopUp());
                ShowRewarded();
                shouldShowRewardedAd = false;
            }
            if (dontShow)
            {
                if (skipPoppedUp)
                    StartCoroutine(HideSkipPopUp());
                else
                    StartCoroutine(HidePopUp());
                dontShow = false;
            }
        }
    }
}
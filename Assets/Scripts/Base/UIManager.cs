﻿using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.U2D;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine.Networking;
using static Globals.Config;
using System.Linq;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Video;
using Globals;
using System.Text;


#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

using System.Threading;

public class UIManager : MonoBehaviour
{
    public static UIManager instance = null;
    [SerializeField] Sprite sf_toast = null;
    [SerializeField] GameObject nodeLoad;

    [SerializeField] public Transform parentPopups, parentGame, parentBanner;
    public TMP_FontAsset fontDefault = null;

    //public AnimatorController animatorButton;

    public LoginView loginView;
    public LobbyView lobbyView;

    [SerializeField] TextMeshProUGUI testFont;

    float timeShowLoad = 0;

    public SpriteAtlas avatarAtlas, cardAtlas;
    [SerializeField] Sprite avtDefault;
    [SerializeField] Canvas canvasGame;
    [HideInInspector] public GameView gameView;
    [SerializeField] AlertMessage alertMessage;
    [SerializeField] VideoPlayer videoPlayer;
    [SerializeField] GameObject videoBg;

    private DialogView currentDialogNoti = null;

    //private DialogView messageBox;
    //private DialogView dialogViewPopup;

    public List<DialogView> dialogPool = new List<DialogView>();
    public List<DialogView> listDialogOne = new List<DialogView>();
    [SerializeField] public VideoClip videoStartSiXiang;
    private List<ButtonVipFarm> _VipFarmVFs = new();
    public long PusoyJackPot;
    public bool SendChatEmoToHiddenPlayers = false;
    
    private string _videoSiXiangUrl =
        "https://res.cloudinary.com/dytclhw7n/video/upload/v1740734548/intromp4_wxsfza.mp4";

    public Sprite spAvatarMe;
    void Awake()
    {
        // Application.targetFrameRate = 60;

        instance = this;
        curGameId = PlayerPrefs.GetInt("curGameId", 0);
        curServerIp = PlayerPrefs.GetString("curServerIp", "");
        loadTextConfig();
        getConfigSetting();

        TimeOpenApp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        Input.multiTouchEnabled = false;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        refreshUIFromConfig();
    }
    private void OnEnable()
    {
        Debug.Log("OnEnable Uimanager");
    }
    public IEnumerator loadVideoAsync(Action<VideoClip> cb)
    {
        ResourceRequest resourceRequest = Resources.LoadAsync<VideoClip>("GameView/SiXiang/intromp4");
        yield return resourceRequest;
        cb(resourceRequest.asset as VideoClip);
        //loadAsyncTask.Start();
    }
    public void SetDataVipFarmList()
    {
        if (Config.dataVipFarm == null) return;
        float dataVipFarm = Mathf.Min((float)Config.dataVipFarm["farmPercent"], 100);
        foreach (ButtonVipFarm item in _VipFarmVFs) item.SetData(dataVipFarm);
    }
    public void UpdateVipFarmsList(ButtonVipFarm bvf, bool isAdding)
    {
        if (isAdding) _VipFarmVFs.Add(bvf);
        else _VipFarmVFs.Remove(_VipFarmVFs.Find(x => x == bvf));
    }

    VideoPlayer.EventHandler videoStartedListener;
    VideoPlayer.EventHandler videoEndedListener;
    public void playVideoSiXiang()
    {
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.url = _videoSiXiangUrl;

            videoBg.SetActive(false);
            videoBg.GetComponent<RawImage>().color = new Color32(255, 255, 225, 0);
            videoPlayer.gameObject.SetActive(true);

            videoPlayer.prepareCompleted += (vp) =>
            {
                Debug.Log("videoPlayer.prepareCompleted is run "+ (float)videoPlayer.length);
                videoPlayer.Play();

                DOTween.Sequence().AppendInterval(1.4f).AppendCallback(() =>
                {
                    Debug.Log("showGame is run");
                    showGame();
                });
            };

            videoStartedListener = delegate
            {
                Debug.Log("videoStartedListener is run");
                videoBg.SetActive(true);
                videoBg.GetComponent<RawImage>().color = new Color32(255, 255, 225, 255);
                videoPlayer.started -= videoStartedListener;
            };

            videoEndedListener = delegate
            {
                Debug.Log("videoEndedListener is run");
                videoBg.SetActive(false);
                videoPlayer.gameObject.SetActive(false);
                videoPlayer.loopPointReached -= videoEndedListener; 
            };
            
            videoPlayer.started += videoStartedListener;
            videoPlayer.loopPointReached += videoEndedListener;
            
            videoPlayer.errorReceived += (vp, message) =>
            {
                Debug.LogError("Error: " + message);
                showGame();
            };

            videoPlayer.Prepare();
        }
    }

    
    //internal void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
    //{
    //    //Globals.Logging.Log($"{permissionName} PermissionDeniedAndDontAskAgain");
    //}

    //internal void PermissionCallbacks_PermissionGranted(string permissionName)
    //{
    //    //Globals.Logging.Log($"{permissionName} PermissionCallbacks_PermissionGranted");
    //}

    //internal void PermissionCallbacks_PermissionDenied(string permissionName)
    //{
    //    //Globals.Logging.Log($"{permissionName} PermissionCallbacks_PermissionDenied");
    //}
    void Start()
    {
        lobbyView.hide(false);
        videoPlayer.Prepare();
        if (Screen.width <= Screen.height)
        {
            RectTransform videoRT = videoPlayer.GetComponent<RectTransform>();
            RectTransform bgVideoRT = videoBg.GetComponent<RectTransform>();
            float ratio = Mathf.Max(Screen.width / 720f, Screen.height / 1280f);
            videoRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ratio * 1280);
            videoRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ratio * 720);
            bgVideoRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ratio * 1280);
            bgVideoRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ratio * 720);
        }
        // OneSignal.Default.Initialize("def6e8d4-8348-439e-8294-760cb99e4864");
#if UNITY_ANDROID
        //if (Permission.HasUserAuthorizedPermission(Permission.))
        //{
        //    // The user authorized use of the microphone.
        //}
        //else
        //{
        //    bool useCallbacks = false;
        //    if (!useCallbacks)
        //    {
        //        // We do not have permission to use the microphone.
        //        // Ask for permission or proceed without the functionality enabled.
        //        Permission.RequestUserPermission(Permission.Microphone);
        //    }
        //    else
        //    {
        //        var callbacks = new PermissionCallbacks();
        //        callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
        //        callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
        //        callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
        //        Permission.RequestUserPermission(Permission.Microphone, callbacks);
        //    }
        //}
#elif UNITY_IOS
#endif

        //     Unity.Notifications.Android.AndroidNotificationCenter.NotificationReceivedCallback receivedNotificationHandler =
        // delegate (Unity.Notifications.Android.AndroidNotificationIntentData data)
        // {
        //     var msg = "Notification received id: " + data.Id + "\n";
        //     msg += "\n Notification received: ";
        //     msg += "\n .Title: " + data.Notification.Title;
        //     msg += "\n .Body: " + data.Notification.Text;
        //     msg += "\n .Channel: " + data.Channel;
        //     msg += "\n .Group: " + data.Notification.Group;
        //     Globals.Logging.Log(msg);
        // };

        //     Unity.Notifications.Android.AndroidNotificationCenter.OnNotificationReceived += receivedNotificationHandler;
    }

    public string environment = "production";

    async void init()
    {
        try
        {
            var options = new InitializationOptions()
                .SetEnvironmentName(environment);

            await UnityServices.InitializeAsync(options);
        }
        catch (Exception exception)
        {
            // An error occurred during initialization.
        }
    }


    public void pushLocalNotiOff()
    {
        return;
        if (!allowPushOffline || !isLoginSuccess) return;
        var timeLeft = Globals.Promotion.time;
#if UNITY_ANDROID
        var c = new AndroidNotificationChannel()
        {
            Id = DateTime.Now.Millisecond.ToString(),
            Name = "Default Channel",
            Importance = Importance.High,
            Description = getTextConfig("txt_freechip")
        };
        AndroidNotificationCenter.RegisterNotificationChannel(c);
        var notification = new AndroidNotification();
        notification.Title = getTextConfig("txt_freechip");
        notification.Text = getTextConfig("txt_noti_free_chip");
        notification.FireTime = DateTime.Now.AddSeconds(timeLeft);
        notification.Group = "free_chip";
        AndroidNotificationCenter.SendNotification(notification, c.Id);
#elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, timeLeft),
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            // You can optionally specify a custom identifier which can later be 
            // used to cancel the notification, if you don't set one, a unique 
            // string will be generated automatically.
            Identifier = "_notification_01",
            Title = "Title",
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    public void pushLocalNotiEveryDay()
    {
        if (!allowPushOffline || !isLoginSuccess) return;
        //var listNoti = delayNoti;
        //    let date = new Date();
        //    let hour = date.getHours();
        //    let min = date.getMinutes();
        //    let time = hour * 3600 + min * 60;
        //    for (let i = 0; i < listNoti.length; i++)
        //    {
        //        let timeLeft = time;
        //        let content = listNoti[i].text;
        //        let title = listNoti[i].title;
        //        let data = listNoti[i].data;
        //        var noti = {
        //                title: title,
        //                time: timeLeft,
        //                content: Global.encode(content),
        //                category: "",
        //                identifier: "",
        //                data: data,
        //                isLoop: true
        //            };
        //    require("Util").pushNotiOffline(JSON.stringify(noti));
        //}
    }
    private void checkSalert()
    {
        if (list_Alert.Count > 0)
        {
            JObject data = list_Alert[0];
            list_Alert.RemoveAt(0);
            showAlertMessage(data);
        }
    }
    public Sprite getRandomAvatar()
    {

        return avtDefault;
    }
    public void changeOrientation(ScreenOrientation screenOrient)
    {
        canvasGame.GetComponent<CanvasScaler>().matchWidthOrHeight = screenOrient == ScreenOrientation.Portrait ? 0 : 1.0f;
        canvasGame.GetComponent<CanvasScaler>().referenceResolution = screenOrient == ScreenOrientation.Portrait ? new Vector2(720, 1280) : new Vector2(1280, 720);
        Screen.orientation = screenOrient;
        DOTween.Sequence().AppendInterval(0.5f).AppendCallback(() =>
        {
            SafeArena.instance.changeOrient();
            AlertShort.Instance.updateChangeOrient();
        });
    }


    public void changeOrientTest()
    {
        if (Screen.orientation == ScreenOrientation.Portrait)
        {
            changeOrientation(ScreenOrientation.LandscapeLeft);
        }
        else
        {
            changeOrientation(ScreenOrientation.Portrait);
        }

        DOTween.Sequence().SetDelay(5.0f).AppendCallback(() =>
        {
            changeOrientation(ScreenOrientation.Portrait);

        });
    }

    public void OnApplicationQuit()
    {
        Globals.Logging.LogWarning("-=-=OnApplicationQuit ");
        SocketIOManager.getInstance().stopIO();
    }
    //long timeOnPause = 0;
    public void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Globals.Logging.Log("-=-=OnApplicationPause ");
            //timeOnPause = DateTime.Now.Millisecond;
            pushLocalNotiOff();
        }
        else
        {
            Globals.Logging.Log("-=-=!OnApplicationResume " + (gameView != null));
            if (gameView != null)
                showLoginScreen(true);
        }
    }
    public bool isLoginShow()
    {
        return loginView.getIsShow();
    }

    public void showAlertMessage(JObject data)
    {
        if (loginView.getIsShow()) return;
        alertMessage.addAlertMessage(data);
    }
    void Update()
    {
        if (timeShowLoad > 0)
        {
            timeShowLoad -= Time.deltaTime;
            if (timeShowLoad <= 0)
            {
                hideWatting();
            }
        }
    }

    public void showWaiting(float timeOut = 10)
    {
        if (nodeLoad.activeSelf) return;
        timeShowLoad = timeOut;
        nodeLoad.SetActive(true);
    }

    public void hideWatting()
    {
        timeShowLoad = 0;
        nodeLoad.SetActive(false);
    }
    public void updateMailAndMessageNoti()
    {
        lobbyView.updateMailandMessageNoti();
    }
    public void setNotiMessage(bool state)
    {
        lobbyView.setNotiMessage(state);
    }
    public void updateChipUser()
    {
        lobbyView.updateAg();
        if (ProfileView.instance != null && ProfileView.instance.gameObject.activeSelf)
        {
            ProfileView.instance.updateAg();
        }
        if (ShopView.instance != null && ShopView.instance.gameObject.activeSelf)
        {
            ShopView.instance.updateAg();
        }
        if (gameView != null && gameView.gameObject.activeSelf)
        {
            gameView.thisPlayer.updateMoney();
        }
        if (TableView.instance != null && TableView.instance.gameObject.activeSelf)
        {
            TableView.instance.updateAg();
        }
    }
    public void showLoginScreen(bool isReconnect = false)
    {

        if (loginView.getIsShow()) return;
        Globals.Logging.Log("UImanager showLoginScreen");
        //if (!isReconnect)
        //{
        //    WebSocketManager.getInstance().stop();
        //}
        if (seqPing != null)
        {
            seqPing.Kill();
        }
        seqPing = null;


        lobbyView.hide(false);

        if (TableView.instance != null)
        {
            Destroy(TableView.instance.gameObject);
        }
        TableView.instance = null;

        Globals.Logging.Log("gameView   " + (gameView != null));
        if (gameView != null)
        {
            Destroy(gameView);
        }
        gameView = null;
        destroyAllChildren(parentGame);
        dialogPool.Clear();
        listDialogOne.Clear();

        destroyAllPopup();

        WebSocketManager.getInstance().stop();
        SocketIOManager.getInstance().stopIO();
        loginView.show();
        if (isReconnect)
        {
            loginView.reconnect();
        }
    }

    public void showGame()
    {

        if (gameView != null && curGameId != (int)Globals.GAMEID.SLOT_SIXIANG)
        {

            Destroy(gameView.gameObject);

        }
        if (gameView != null && curGameId == (int)Globals.GAMEID.SLOT_SIXIANG)
        {
            return;
        }
        gameView = null;
        switch (curGameId)
        {
            case (int)Globals.GAMEID.DUMMY:
                {
                    Globals.Logging.Log("Di vao day RUMMY");
                    gameView = Instantiate(loadPrefabGame("DummyView"), parentGame).GetComponent<DummyView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    Globals.Logging.Log("showGame RUMMY 2   " + (gameView != null));
                    break;
                }

            case (int)Globals.GAMEID.SLOTNOEL:
                {
                    Globals.Logging.Log("showGame SLOTNOEL");
                    gameView = Instantiate(loadPrefabGame("SlotNoelView"), parentGame).GetComponent<SlotNoelView>();
                    break;
                }
            case (int)Globals.GAMEID.SLOTTARZAN:
                {
                    Globals.Logging.Log("showGame SLOTTARZAN");
                    gameView = Instantiate(loadPrefabGame("SlotTarzanView"), parentGame).GetComponent<SlotTarzanView>();
                    break;
                }
            case (int)Globals.GAMEID.SLOT_JUICY_GARDEN:
                {
                    Globals.Logging.Log("showGame SLOT_9900");
                    gameView = Instantiate(loadPrefabGame("SlotJuicyGardenView"), parentGame).GetComponent<SlotJuicyGardenView>();
                    break;
                }
            case (int)Globals.GAMEID.SLOT_INCA:
                {
                    Globals.Logging.Log("showGame SLOTINCA");

                    gameView = Instantiate(loadPrefabGame("SlotInCaView"), parentGame).GetComponent<SlotInCaView>();
                    break;
                }
            case (int)Globals.GAMEID.SLOT_SIXIANG:
                {
                    Globals.Logging.Log("showGame SLOT_SIXIANG");
                    gameView = Instantiate(loadPrefabGame("SiXiangView"), parentGame).GetComponent<SiXiangView>();
                    break;
                }
            case (int)Globals.GAMEID.SLOT20FRUIT:
                {
                    Globals.Logging.Log("showGame SLOT20FRUIT");
                    gameView = Instantiate(loadPrefabGame("SlotFruitView"), parentGame).GetComponent<SlotFruitView>();
                    break;
                }
            case (int)Globals.GAMEID.LUCKY_89:
                {
                    Globals.Logging.Log("showGame Lucky89");
                    gameView = Instantiate(loadPrefabGame("Lucky89View"), parentGame).GetComponent<Lucky89View>();
                    break;
                }
            case (int)Globals.GAMEID.KEANG:
                {
                    Globals.Logging.Log("showGame KEANG");
                    gameView = Instantiate(loadPrefabGame("KeangView"), parentGame).GetComponent<KeangView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }
            case (int)Globals.GAMEID.GAOGEA:
                {
                    Globals.Logging.Log("showGame GAOGEA");
                    gameView = Instantiate(loadPrefabGame("GaoGeaView"), parentGame).GetComponent<GaoGeaView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }
            case (int)Globals.GAMEID.SICBO:
                {
                    Globals.Logging.Log("showGame SICBO");
                    gameView = Instantiate(loadPrefabGame("SicboView"), parentGame).GetComponent<SicboView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }
            case (int)Globals.GAMEID.BANDAR_QQ:
                {
                    Globals.Logging.Log("showGame Bandar");
                    gameView = Instantiate(loadPrefabGame("BandarQQView"), parentGame).GetComponent<BandarQQView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }

            case (int)Globals.GAMEID.RONGHO:
                {
                    Globals.Logging.Log("showGame RONGHO");
                    gameView = Instantiate(loadPrefabGame("DragonTigerView"), parentGame).GetComponent<DragonTigerView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }
            case (int)Globals.GAMEID.DOMINO:
                {
                    Globals.Logging.Log("showGame DOMINO");
                    gameView = Instantiate(loadPrefabGame("DominoGaple"), parentGame).GetComponent<DominoGapleView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }

            case (int)Globals.GAMEID.BACCARAT:
                {
                    Globals.Logging.Log("showGame BACCARAT");
                    gameView = Instantiate(loadPrefabGame("BaccaratView"), parentGame).GetComponent<BaccaratView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }
            case (int)Globals.GAMEID.PUSOY:
                {
                    Globals.Logging.Log("showGame BINH");
                    gameView = Instantiate(loadPrefabGame("BinhView"), parentGame).GetComponent<BinhGameView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    Debug.Log("Set Game View Binh:" + gameView);
                    break;
                }
            case (int)Globals.GAMEID.KARTU_QIU:
                {
                    Globals.Logging.Log("showGame KARTU_QIU");
                    gameView = Instantiate(loadPrefabGame("BorkKdengView"), parentGame).GetComponent<BorkKDengView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }
            case (int)Globals.GAMEID.BLACKJACK:
                {
                    Globals.Logging.Log("showGame BLACKJACK");
                    gameView = Instantiate(loadPrefabGame("BlackJackView"), parentGame).GetComponent<BlackJackView>();
                    //gameView.transform.eulerAngles = new Vector3(0, 0, -90);
                    break;
                }
            case (int)Globals.GAMEID.TONGITS_OLD:
                {
                    Globals.Logging.Log("showGame TONGITS thuong");
                    gameView = Instantiate(loadPrefabGame("TongitsView"), parentGame).GetComponent<TongitsView>();
                    break;
                }
            case (int)Globals.GAMEID.TONGITS:
                {
                    Globals.Logging.Log("showGame TONGITS butasan");
                    gameView = Instantiate(loadPrefabGame("TongitsView"), parentGame).GetComponent<TongitsView>();
                    break;
                }
            case (int)Globals.GAMEID.TONGITS_JOKER:
                {
                    Globals.Logging.Log("showGame TONGITS joker");
                    gameView = Instantiate(loadPrefabGame("TongitsView"), parentGame).GetComponent<TongitsView>();
                    break;
                }
            case (int)Globals.GAMEID.LUCKY9:
                {
                    Globals.Logging.Log("showGame LUCKY9");
                    gameView = Instantiate(loadPrefabGame("Lucky9View"), parentGame).GetComponent<Lucky9View>();
                    break;
                }
            case (int)Globals.GAMEID.SABONG:
                {
                    Globals.Logging.Log("showGame SABONG");
                    gameView = Instantiate(loadPrefabGame("SabongView"), parentGame).GetComponent<SabongGameView>();
                    break;
                }
            case (int)Globals.GAMEID.MINE_FINDING:
                {
                    Globals.Logging.Log("showGame MineFinding");
                    gameView = Instantiate(loadPrefabGame("PopupMineFinding"), parentGame).GetComponent<MineFindingView>();
                    break;
                }
            //case (int)Globals.GAMEID.ROULETTE:
            //    {
            //        Globals.Logging.Log("showGame ROULETTE");
            //        gameView = Instantiate(loadPrefabGame(""), parentGame).GetComponent<SabongGameView>();
            //        break;
            //    }
            //case (int)Globals.GAMEID.BAUCUA:
            //    {
            //        Globals.Logging.Log("showGame BAUCUA");
            //        gameView = Instantiate(loadPrefabGame("SabongView"), parentGame).GetComponent<SabongGameView>();
            //        break;
            //    }
            default:
                {
                    Globals.Logging.Log("-=-= chua co game nao ma vao. Lm thi tu them vao di;;;;");
                    break;
                }
        }
        if (gameView != null)
        {
            Globals.CURRENT_VIEW.setCurView(curGameId.ToString());
            if (TableView.instance)
                TableView.instance.hide(false);
            //if (!isShowTableWithGameId(curGameId))
            //{
            if (lobbyView.getIsShow())
                lobbyView.hide(false);
            //}
            gameView.transform.localScale = Vector3.one;

            destroyAllPopup();
        }
        if (!TELEGRAM_TOKEN.Equals("") && ExchangeView.instance != null) Destroy(ExchangeView.instance);
    }

    public void destroyAllChildren(Transform transform)
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
    public void destroyAllPopup()
    {
        destroyAllChildren(parentPopups);
        destroyAllChildren(parentBanner);
    }
    public void DOTextTmp(TextMeshProUGUI tmp, string text, float time = 0.5f)
    {
        GameObject lbTemp = new GameObject("lbTemp");
        Text lbText = lbTemp.AddComponent<Text>();
        lbText.DOText(text, time).OnUpdate(() =>
        {
            tmp.text = lbText.text;
        }).OnComplete(() =>
        {
            Destroy(lbTemp);
        });
    }
    public void updateAvatar()
    {
        if (ProfileView.instance != null)
        {
            ProfileView.instance.onChangeAvatar();
            showToast(getTextConfig("success_change_ava"));
        }
        lobbyView.updateAvatar();
    }

    public void updateVip()
    {
        lobbyView.updateAg();
        lobbyView.updateAgSafe();
        lobbyView.refreshUIFromConfig(true);
        if (gameView != null)
        {
            gameView.updateVip();
        }
    }
    public void updateInfo()
    {
        lobbyView.updateInfo();
    }
    public void updateCanInviteFriend()
    {
        lobbyView.updateCanInviteFriend();
    }
    public void updateAG()
    {
        lobbyView.updateAg();
        if (SendGiftView.instance && SendGiftView.instance.gameObject.activeSelf)
        {
            SendGiftView.instance.updateChip();
        }

        if (ProfileView.instance != null && ProfileView.instance.gameObject.activeSelf)
        {
            ProfileView.instance.updateAg();
        }
        if (ShopView.instance != null && ShopView.instance.gameObject.activeSelf)
        {
            ShopView.instance.updateAg();
        }
        if (gameView != null && gameView.gameObject.activeSelf)
        {
            gameView.thisPlayer.updateMoney();
        }
        if (TableView.instance != null && TableView.instance.gameObject.activeSelf)
        {
            TableView.instance.updateAg();
        }
        if (ExchangeView.instance != null && ExchangeView.instance.gameObject.activeSelf)
        {
            ExchangeView.instance.UpdateAg();
        }
    }
    public void updateAGSafe()
    {
        lobbyView.updateAgSafe();
    }

    Sequence seqPing;
    public void showLobbyScreen(bool isFromLogin = false)
    {
        Globals.Logging.Log("showLobbyScreen  ");
        loginView.hide(false);
        destroyAllChildren(parentPopups);
        lobbyView.show();
        lobbyView.updateInfo();
        SocketSend.getFarmInfo();
        if (gameView != null)
            Destroy(gameView.gameObject);
    }

    public void showAlert(bool isShow)
    {
        lobbyView.showAlert(isShow);
    }

    public void refreshUIFromConfig()
    {
        lobbyView.refreshUIFromConfig();
    }

    public void updateBotWithScrollShop(Vector2 value)
    {
        lobbyView.updateBotWithScrollShop(value);
    }

    public void setTimeOnline()
    {
        lobbyView.setTimeGetMoney();
    }

    public GameObject loadPrefabPopup(string name)
    {
        return loadPrefab("Popups/" + name);
    }

    public GameObject loadPrefabGame(string name)
    {
        return loadPrefab("GameView/" + name);
    }
    public Sprite LoadChipImage(int chipId)
    {
        return Resources.Load<Sprite>("Sprite Assets/Chips/chip_" + chipId);
    }
    public GameObject loadPrefab(string path)
    {
        return Resources.Load(path) as GameObject;
    }
    public SkeletonDataAsset loadSkeletonData(string path)
    {
        return Resources.Load<SkeletonDataAsset>(path);

    }
    public IEnumerator loadSkeletonDataAsync(string path, Action<SkeletonDataAsset> cb)
    {
        ResourceRequest resourceRequest = Resources.LoadAsync<SkeletonDataAsset>(path);
        yield return resourceRequest;
        cb(resourceRequest.asset as SkeletonDataAsset);
        //loadAsyncTask.Start();
    }

    void createMessageBox(GameObject prefab, string msg, Action callback1 = null, bool isHaveClose = false)
    {
        //new Thread(new ThreadStart(() =>
        //{
        DialogView dialog;
        if (dialogPool.Count == 0)
        {
            //messageBox = Instantiate(loadPrefabPopup("Dialog"), parentPopups).GetComponent<DialogView>();
            Debug.Log("-=-=listDialogOne  " + listDialogOne.Count);
            if (listDialogOne.FirstOrDefault(x => x.getMessage().Equals(msg)) == null)
            {
                dialog = Instantiate(prefab, parentPopups).GetComponent<DialogView>();
            }
            else return;
        }
        else
        {
            //dialog = dialogPool[0];
            //dialogPool.RemoveAt(0);
            //dialog.transform.parent = parentPopups;
            dialog = listDialogOne.FirstOrDefault(x => x.getMessage().Equals(msg));
            if (dialog == null)
            {
                dialog = dialogPool[0];
                dialogPool.RemoveAt(0);
                dialog.transform.parent = parentPopups;
            }
        }

        listDialogOne.Add(dialog);
        dialog.gameObject.SetActive(true);
        dialog.transform.localScale = Vector3.one;
        dialog.transform.SetAsLastSibling();
        dialog.setMessage(msg);
        dialog.setIsShowButton1(true, getTextConfig("ok"), callback1);
        dialog.setIsShowButton2(false, "", null);
        dialog.setIsShowClose(isHaveClose, null);

        if (Screen.width < Screen.height) dialog.transform.localRotation = Quaternion.Euler(dialog.transform.localRotation.x, dialog.transform.localRotation.y, 0);
        else
        {
            if (gameView != null)
            {
                dialog.transform.eulerAngles = gameView.transform.eulerAngles;
                if (dialog.transform.eulerAngles.z != 0)
                {
                    dialog.setLanscape();
                }
            }
        }
    }

    public void showMessageBox(string msg, Action callback1 = null, bool isHaveClose = false)
    {
        if (msg == "") return;
#if DEVGAME
        AssetBundleManager.instance.loadPrefab(Globals.AssetBundleName.POPUPS, "Dialog", (prefab) =>
        {
            createMessageBox(prefab, msg, callback1, isHaveClose);
        });
#else
        createMessageBox(loadPrefabPopup("Dialog"), msg, callback1, isHaveClose);
#endif
    }

    DialogView createDialog(GameObject prefab, string msg, string nameBtn1 = "", Action callback1 = null, string nameBtn2 = "", Action callback2 = null, bool isShowClose = false, Action callback3 = null)
    {
        DialogView dialog;
        if (dialogPool.Count == 0)
        {
            //dialog = Instantiate(loadPrefabPopup("Dialog"), parentPopups).GetComponent<DialogView>();
            dialog = listDialogOne.FirstOrDefault(x => x.getMessage().Equals(msg));
            if (dialog == null)
            {
                dialog = Instantiate(prefab, parentPopups).GetComponent<DialogView>();
                listDialogOne.Add(dialog);
            }
        }
        else
        {
            dialog = listDialogOne.FirstOrDefault(x => x.getMessage().Equals(msg));
            if (dialog == null)
            {
                dialog = dialogPool[0];
                dialogPool.RemoveAt(0);
                dialog.transform.parent = parentPopups;
                listDialogOne.Add(dialog);
            }
        }
        dialog.gameObject.SetActive(true);
        dialog.transform.localScale = Vector3.one;
        dialog.transform.SetAsLastSibling();
        dialog.setMessage(msg);
        dialog.setIsShowButton1(nameBtn1 != "", nameBtn1, callback1);
        dialog.setIsShowButton2(nameBtn2 != "", nameBtn2, callback2);
        dialog.setIsShowClose(isShowClose, callback3);
        if (Screen.width < Screen.height) dialog.transform.localRotation = Quaternion.Euler(dialog.transform.localRotation.x, dialog.transform.localRotation.y, 0);
        else
        {
            if (gameView != null)
            {
                dialog.transform.eulerAngles = gameView.transform.eulerAngles;
                if (dialog.transform.eulerAngles.z != 0)
                {
                    dialog.setLanscape();
                }
            }
        }
        return dialog;
    }
    public void showDialog(string msg, string nameBtn1 = "", Action callback1 = null, string nameBtn2 = "", Action callback2 = null, bool isShowClose = false, Action callback3 = null, Action<DialogView> callbaclReturn = null)
    {
#if DEVGAME
        AssetBundleManager.instance.loadPrefab(Globals.AssetBundleName.POPUPS, "Dialog", (prefab) =>
        {
            var dialog = createDialog(loadPrefabPopup("Dialog"), msg, nameBtn1 = "", callback1, nameBtn2, callback2, isShowClose, callback3);
            if (callbaclReturn != null)
            {
                callbaclReturn.Invoke(dialog);
            }
        });
#else 
        var dialog = createDialog(loadPrefabPopup("Dialog"), msg, nameBtn1, callback1, nameBtn2, callback2, isShowClose, callback3);
        if (callbaclReturn != null)
        {
            callbaclReturn.Invoke(dialog);
        }
#endif
    }

    public void showWebView(string url, string title = "")
    {
    }
    public void showToast(string msg, Transform tfParent)
    {
        showToast(msg, 2, tfParent);
    }
    public void showToast(string msg, float timeShow = 2, Transform tfParent = null)
    {
        Globals.Logging.Log("Show Toast:" + msg);
        var compToast = createSprite(sf_toast);
        compToast.transform.SetParent(tfParent != null ? tfParent : transform);
        compToast.transform.SetAsLastSibling();
        compToast.type = Image.Type.Sliced;
        compToast.rectTransform.sizeDelta = new Vector2(400, 80);
        compToast.rectTransform.localScale = Vector3.one;
        compToast.transform.localPosition = new Vector2(0, -Screen.height / 4);


        var lbCom = createLabel(msg, 30);
        lbCom.rectTransform.SetParent(compToast.rectTransform);
        lbCom.rectTransform.localScale = Vector3.one;
        lbCom.color = Color.white;
        lbCom.alignment = TextAlignmentOptions.Center;
        lbCom.enableWordWrapping = false;

        if (lbCom.preferredWidth > compToast.rectTransform.sizeDelta.x)
        {
            compToast.rectTransform.sizeDelta = new Vector2(lbCom.preferredWidth + 100, compToast.rectTransform.sizeDelta.y);
        }

        lbCom.rectTransform.sizeDelta = new Vector2(390, 50);
        lbCom.transform.localPosition = new Vector2(0, 5);

        if (gameView != null)
        {
            compToast.transform.eulerAngles = gameView.transform.eulerAngles;
            if (gameView.transform.eulerAngles.z == 0)
            {
                compToast.rectTransform.anchoredPosition = new Vector3(0, -150);
            }
            else
                compToast.rectTransform.anchoredPosition = new Vector3(0, 0, 0);
        }


        compToast.rectTransform.localScale = Vector3.zero;
        DOTween.Sequence().Append(compToast.rectTransform.DOScale(1, .5f).SetEase(Ease.OutBack)).Append(compToast.rectTransform.DOScale(0, .5f).SetEase(Ease.InBack).SetDelay(timeShow)).AppendCallback(() =>
        {
            Destroy(compToast.gameObject);
        }).SetAutoKill(true);
    }

    public void openShop()
    {
        return;
        var shopView = Instantiate(loadPrefabPopup("PopupShop"), parentPopups).GetComponent<ShopView>();
        shopView.init();
        shopView.transform.localScale = Vector3.one;
    }
    public LuckyNumberView OpenLuckyNumber()
    {
        LuckyNumberView script = Instantiate(loadPrefabPopup("PopupLuckyNumber"), parentPopups).GetComponent<LuckyNumberView>();
        script.transform.localScale = Vector3.one;
        return script;
    }
    public void openRuleJPBork()
    {
        Debug.Log("openRuleJPBork:");
        var ruleView = Instantiate(loadPrefab("GameView/Bork/JackpotRuleBork"), parentPopups).GetComponent<BaseView>();
        ruleView.transform.localScale = Vector3.one;
    }
    public void openRuleJPBinh()
    {
        var ruleView = Instantiate(loadPrefab("GameView/Binh/JackpotRuleBinh"), parentPopups).GetComponent<BaseView>();
        ruleView.transform.localScale = Vector3.one;
    }

    public void openVipFarm()
    {
        var subView = Instantiate(loadPrefabPopup("PopupVipFarm"), parentPopups);
        subView.transform.localScale = Vector3.one;
    }
    public void openConfirmVipFarm()
    {
        showDialog(getTextConfig("txt_rewards_vip_farm"), getTextConfig("ok"), () =>
        {
            if (VipFarmView.instance == null) openVipFarm();
        }, getTextConfig("label_cancel"));
    }
    public void openChatWorld()
    {
        var chatWorldView = Instantiate(loadPrefabPopup("PopupChatWorld"), parentPopups).GetComponent<ChatWorldView>();
        chatWorldView.transform.localScale = Vector3.one;
    }
    public void clickTabChatWorld()
    {
        lobbyView.onShowChatWorld(false);
    }
    public void clickTabFreeChip()
    {
        lobbyView.onClickFreechip();
    }
    public void openFriendInfo()
    {
        var friendInfoView = Instantiate(loadPrefabPopup("PopupFriendInfo"), parentPopups).GetComponent<FriendInfoView>();
        friendInfoView.transform.localScale = Vector3.one;
    }

    public void openEx()
    {
        var exchangeView = Instantiate(loadPrefabPopup("PopupExchange"), parentPopups).GetComponent<ExchangeView>();
        exchangeView.transform.localScale = Vector3.one;
    }

    bool isOnProfile = true;
    public void openProfile()
    {
        if (!isOnProfile) return;
        isOnProfile = false;
        DOTween.Sequence().AppendInterval(1).AppendCallback(() =>
        {
            isOnProfile = true;
        });
        var profileView = Instantiate(loadPrefabPopup("PopupProfile"), parentPopups).GetComponent<ProfileView>();
        profileView.transform.localScale = Vector3.one;
    }
    public void openGiftCode()
    {
        var giftCodeView = Instantiate(loadPrefabPopup("PopupGiftCode"), parentPopups).GetComponent<GiftCodeView>();
        giftCodeView.transform.localScale = Vector3.one;
    }
    public void openSetting()
    {
        //curGameId = (int)Globals.GAMEID.KEANG;
        //UIManager.instance.showGame();
        var settingView = Instantiate(loadPrefabPopup("PopupSetting"), parentPopups).GetComponent<SettingView>();
        settingView.transform.localScale = Vector3.one;
    }
    public void openLeaderBoard(int gameID = -1)
    {
        var leaderBoardView = Instantiate(loadPrefabPopup("PopupLeaderBoard"), parentPopups).GetComponent<LeaderBoardView>();
        leaderBoardView.transform.localScale = Vector3.one;
        leaderBoardView.openTabGameWithID(gameID);
    }
    public void openSendGift(string idPlayerInit = "")
    {
        var sendGiftView = Instantiate(loadPrefabPopup("PopupSendGift"), parentPopups).GetComponent<SendGiftView>();
        if (idPlayerInit != "")
        {
            sendGiftView.edbID.text = idPlayerInit;
        }
        sendGiftView.transform.localScale = Vector3.one;
    }
    public void openCreateTableView()
    {
        var createTableView = Instantiate(loadPrefabPopup("PopupCreateTable"), parentPopups).GetComponent<CreateTableView>();
        createTableView.transform.localScale = Vector3.one;
    }
    public void openChangePass()
    {
        var changePassView = Instantiate(loadPrefabPopup("PopupChangeName"), parentPopups).GetComponent<ChangeNameView>();
    }
    public void openMailDetail(JObject data)
    {
        var mailDetailView = Instantiate(loadPrefabPopup("PopupMailDetail"), parentPopups).GetComponent<MailDetailView>();
        mailDetailView.transform.localScale = Vector3.one;
        MailDetailView.instance.setInfo(data);
    }

    public void openInputPass(int tableID)
    {
        var inputPassView = Instantiate(loadPrefabPopup("PopupInputPass"), parentPopups).GetComponent<InputPassView>();
        inputPassView.setTableID(tableID);
        inputPassView.transform.localScale = Vector3.one;
    }


    public void openFeedback()
    {
        var inputPassView = Instantiate(loadPrefabPopup("PopupFeedBack"), parentPopups).GetComponent<PopupFeedBack>();
        inputPassView.transform.localScale = Vector3.one;
    }

    bool isOnSafe = true;
    public void openSafeView()
    {
        if (!isOnSafe) return;
        isOnSafe = false;
        DOTween.Sequence().AppendInterval(1).AppendCallback(() =>
        {
            isOnSafe = true;
        });
        //lobbyView.setTabSafe();
        var safeView = Instantiate(loadPrefabPopup("PopupSafe"), parentPopups).GetComponent<SafeView>();
        safeView.transform.localScale = Vector3.one;
    }
    public void openDailyBonus()
    {
        var dailyBonusView = Instantiate(loadPrefabPopup("PopupCountDownBonus"), parentPopups).GetComponent<DailyBonusView>();
        dailyBonusView.transform.localScale = Vector3.one;
        dailyBonusView.setInfo();
    }
    public void openTableView()
    {
        return;
        if (TableView.instance == null)
        {
            //if (curGameId == (int)Globals.GAMEID.KEANG || curGameId == (int)Globals.GAMEID.DUMMY || curGameId == (int)Globals.GAMEID.SICBO)
            //{
            //    Instantiate(loadPrefab("Table/TableViewHorizontal"), transform).GetComponent<TableView>();
            //}
            //else
            //{
            TableView tableView = Instantiate(loadPrefab("Table/TableView"), transform).GetComponent<TableView>();
            //}
        }
        else
        {
            TableView.instance.transform.SetParent(transform);
            TableView.instance.show();
        }
        TableView.instance.transform.SetSiblingIndex(2);
        TableView.instance.transform.localScale = Vector3.one;
        lobbyView.hide(false);
    }
    //public void lobbyHide()
    //{
    //    lobbyView.hide(false);
    //}
    public void openMailView()
    {
        var safeView = Instantiate(loadPrefabPopup("PopupMail"), parentPopups).GetComponent<MailView>();
        safeView.transform.localScale = Vector3.one;
    }
    public void openFreeChipView()
    {
        var subview = Instantiate(loadPrefabPopup("PopupFreeChip"), parentPopups).GetComponent<FreeChipView>();
        subview.transform.localScale = Vector3.one;
        subview.setShowBack(true);
    }

    public void showPopupWhenNotEnoughChip()
    {
        var isInGame = false;
        if (gameView != null) isInGame = true;
        //var typeBTN = isInGame ? DIALOG_TYPE.ONE_BTN : DIALOG_TYPE.TWO_BTN;
        var textShow = getTextConfig("txt_not_enough_money_gl");
        var textBtn1 = getTextConfig("txt_free_chip");
        var textBtn2 = getTextConfig("shop");
        var textBtn3 = getTextConfig("label_cancel");
        if (isInGame)
        {
            textShow = textShow.Split(",")[0];
            textBtn1 = textBtn3;
            textBtn2 = textBtn3;
        }
        if (Globals.User.userMain.nmAg > 0 || Globals.Promotion.countMailAg > 0 || Globals.Promotion.adminMoney > 0)
        {
            showDialog(textShow, textBtn1, () =>
            {
                if (!isInGame) openFreeChipView();
            }, textBtn2, () =>
            {
                openShop();
            }, true);
        }
        else
        {
            textShow = getTextConfig("txt_not_enough_money_gl");
            showDialog(textShow, textBtn2, () =>
            {
                if (!isInGame) openShop();
            }, textBtn3);
        }
    }

    public void showPopupWhenLostChip(bool isBackFromGame = false, bool isChooseGame = false)
    {
        Debug.Log("showPopupWhenLostChip");
        var money = Globals.User.userMain.AG;
        if (money <= 0)
        {
            var isInGame = false;
            if (gameView != null && !isBackFromGame) isInGame = true;
            //var typeBTN = isInGame ? DIALOG_TYPE.ONE_BTN : DIALOG_TYPE.TWO_BTN;
            var textShow = getTextConfig("has_mail_show_gold");
            var textBtn1 = getTextConfig("txt_free_chip");
            var textBtn2 = getTextConfig("shop");
            var textBtn3 = getTextConfig("label_cancel");
            if (isInGame)
            {
                textShow = textShow.Split(",")[0];
                textBtn1 = textBtn3;
                textBtn2 = textBtn3;
            }
            if (isChooseGame) textShow = getTextConfig("txt_not_enough_money_gl");
            if (Globals.User.userMain.nmAg > 0 || Globals.Promotion.countMailAg > 0 ||
                 Globals.Promotion.adminMoney > 0
            )
            {
                showDialog(textShow, textBtn1, () =>
                {
                    if (!isInGame)
                        openFreeChipView();
                }, textBtn2, () =>
                {
                    openShop();
                }, true);
            }
            else
            {
                textShow = getTextConfig("txt_not_enough_money_gl");
                showDialog(textShow, textBtn2, () =>
                {
                    if (!isInGame)
                        openShop();
                }, textBtn3);
            }
        }
    }

    public void showPopupListBanner()
    {
        var subview = Instantiate(loadPrefabPopup("ListBannerView"), parentPopups).GetComponent<ListBannerView>();
        subview.transform.localScale = Vector3.one;
    }

    public void showListBannerOnLobby()
    {
        lobbyView.showBanner();
    }

    public void updateBannerNews()
    {
        lobbyView.updateBannerNews();
    }

    public JArray arrayDataBannerIO;
    int indexCurrentDataBannerIO = 0;
    [SerializeField]
    GameObject banerTemp;

    List<BannerView> arrBannerNotShowGame = new List<BannerView>();
    void removeAllBanerShowGame()
    {
        //let length = arrBannerNotShowGame.length;
        //cc.NGWlog("push vao banner ko show game length " + length);
        //for (let i = 0; i < length; i++)
        //{
        //    let item = arrBannerNotShowGame[i];
        //    if (item.node != null) item.node.destroy();
        //}
        //arrBannerNotShowGame.length = 0;
    }

    void removeBanerShowGame(BannerView banner)
    {
        //let indexOf = arrBannerNotShowGame.indexOf(banner);
        //if (indexOf != -1) arrBannerNotShowGame.splice(indexOf, 1);
    }
    async void showBannerIO()
    {
        if (indexCurrentDataBannerIO < 0 || indexCurrentDataBannerIO >= arrayDataBannerIO.Count || gameView != null)
        {
            return;
        }

        var dataBanner = (JObject)arrayDataBannerIO[indexCurrentDataBannerIO];
        if (!dataBanner.ContainsKey("urlImg") || (string)dataBanner["urlImg"] == "")
        {
            indexCurrentDataBannerIO++;
            nextBanner();
            return;
        }

        var urlImg = (string)dataBanner["urlImg"];
        Debug.Log("showBannerIO");
        Texture2D texture = await Globals.Config.GetRemoteTexture(urlImg);
        if (texture == null)
        {
            nextBanner();
            return;
        }

        if (loginView.getIsShow()) return;
        var nodeBanner = Instantiate(banerTemp, parentBanner).GetComponent<BannerView>();

        //cc.NGWlog("push vao banner ko show game");
        if (!dataBanner.ContainsKey("isShowGameView") || !(bool)dataBanner["isShowGameView"])
        {
            arrBannerNotShowGame.Add(nodeBanner);
        }
        nodeBanner.setInfo(dataBanner, true, () =>
        {
            Destroy(nodeBanner.gameObject);
        });

    }
    public void nextBanner(bool isNotShow = false)
    {
        indexCurrentDataBannerIO++;
        if (isNotShow) return;
        showBannerIO();
    }
    public void handleBannerIO(JArray arrayData)
    {
        if (gameView != null) return;
        arrayDataBannerIO = arrayData;

        if (arrayDataBannerIO.Count > 0)
        {
            indexCurrentDataBannerIO = 0;
            showBannerIO();
        }
    }

    JArray arrayDataBanner;
    int indexCurrentDataBanner = 0;
    public void handleBanner(JArray arrayData)
    {
        arrayDataBanner = arrayData;
        if (arrayDataBanner.Count > 0)
        {
            indexCurrentDataBanner = 0;
            showBanner();
        }
    }
    public void showBanner()
    {

    }
    public void onShowListBaner()
    {
        //cc.NGWlog('onShowListBaner length', arrBanerOnList.length)
        //    if (arrBanerOnList.length <= 0) return;
        //let item = cc.instantiate(Global.ListBaner).getComponent("ListBaner");
        //item.node._tag = TAG.BANNER;
        //node.addChild(item.node);
        //item.init(arrBanerOnList);
    }
    public void forceDisconnect()
    {
        //SoundManager.instance.soundClick();
        //SocketSend.sendLogOut();
        //Config.typeLogin = LOGIN_TYPE.NONE;
        //PlayerPrefs.SetInt("type_login", (int)LOGIN_TYPE.NONE);
        //PlayerPrefs.Save();
        //UIManager.instance.showLoginScreen(false);
        //SocketIOManager.getInstance().emitSIOCCCNew("ClickLogOut");
        showLoginScreen();
    }

    public KeyboardController showKeyboardCustom(Transform tfParrent)
    {
        var inputKeyBoard = Instantiate(loadPrefabPopup("KeyboardController"), tfParrent).GetComponent<KeyboardController>();
        inputKeyBoard.transform.localScale = Vector3.one;
        return inputKeyBoard;

    }
    public void checkAlertMail(bool isEvt22 = true)
    {
        lobbyView.checkAlertMail(isEvt22);
    }

    public void userHasNewMailAdmin()
    {
        if (FreeChipView.instance != null && FreeChipView.instance.gameObject.activeSelf)
        {
            SocketSend.getMail(12);
        }
        //
        Globals.User.userMain.nmAg++;
        updateMailAndMessageNoti();
        if (FreeChipView.instance != null && FreeChipView.instance.gameObject.activeSelf)
        {
            showToast(getTextConfig("has_mail_show_gold").Split(",")[0]);
            return;
        }
        bool isIngame = (gameView != null && gameView.gameObject.activeSelf);
        string textShow = getTextConfig("has_mail_show_gold");
        if (isIngame)
        {
            textShow = textShow.Split(",")[0];
            showToast(textShow);
        }
        else
        {
            showDialog(textShow, getTextConfig("ok"), () =>
            {
                instance.destroyAllPopup();
                clickTabFreeChip();
            }, getTextConfig("label_cancel"));
        }

    }

    public void FixedUpdate()
    {
        foreach (var wwload in listDataLoad.ToArray())//new List<DataLoadImage>(listDataLoad))
        {
            if (wwload != null && !wwload.isDone && wwload.www.isDone)
            {
                wwload.isDone = true;
                if (wwload.sprite != null && !wwload.sprite.IsDestroyed())
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(wwload.www);
                    wwload.sprite.sprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                }

                if (wwload.callback != null)
                {
                    wwload.callback.Invoke(DownloadHandlerTexture.GetContent(wwload.www));
                }
                if (wwload.callback2 != null)
                {
                    wwload.callback2.Invoke();
                }
            }
        }

        for (var i = 0; i < listDataLoad.Count; i++)
        {
            if (listDataLoad[i].isDone)
            {
                listDataLoad.RemoveAt(i);
                i--;
            }
        }
    }

    List<DataLoadImage> listDataLoad = new List<DataLoadImage>();
    public void addJobLoadImage(DataLoadImage dataLoadImage)
    {
        listDataLoad.Add(dataLoadImage);
    }

    public void sendLog(string str, bool isDel)
    {
        return;
        //StartCoroutine(sendLog(str, isDel));
    }
}

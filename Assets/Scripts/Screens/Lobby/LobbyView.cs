﻿using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Spine.Unity;
using DG.Tweening;
using System;
using Globals;
using TS.PageSlider;
using System.Collections;

public class LobbyView : BaseView
{
    [SerializeField] private List<ItemGame> m_ConfigOffIGs;
    [SerializeField] List<Button> listTabs = new();
    [SerializeField]
    GameObject objDot, btnEx, gameItemObject, modelLobby, iconSafe, btnSafe, btnGiftCode, btnLeaderboard,
        btnSupport, icNotiMail, icNotiFree, icNotiMessage, bannerTemp, btnBannerNews, m_Lottery, m_ConfigOn, m_ConfigOff;
    [SerializeField] RectTransform tfBot, CenterNode;
    [SerializeField] TextMeshProUGUI lb_name, lb_id, lb_ag, lb_safe, lbTimeOnline, lbQuickGame;
    [SerializeField] Transform m_MiniGameIconTf, m_OnlySloticonTf;
    [SerializeField] Button btnNext, btnPrevious;
    [SerializeField] SkeletonGraphic animQuickPlay;
    [SerializeField] ScrollRect scrListGame;
    [SerializeField] Avatar avatar;
    [SerializeField] ButtonVipFarm m_VipFarmBVF;
    [SerializeField] PageSlider bannerLobbyContainer;
    [SerializeField] Material materialDefault;

    private List<ItemGame> _AllGameIGs = new List<ItemGame>();
    private List<string> listShowPopupNoti = new();
    private Coroutine _GetInfoPusoyJackPotC;
    private int TabGame = 0;
    private bool blockSpamTabGame, isHideBtnScroll, isRunStart;


    public void DoClickDownloadGame()
    {
        Application.OpenURL(Config.ApkFullUrl);
    }

    protected override void Start()
    {
        isRunStart = true;
        base.Start();
        refreshUIFromConfig(true);
        if (Config.is_dt)
        {
            for (var i = 0; i < listTabs.Count; i++)
            {
                var btn = listTabs[i];
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    OnClickTab(btn);
                });
            }
        }
    }
    public void setQuickPlayGame(int gameID)
    {
        lbQuickGame.gameObject.SetActive(true);
        lbQuickGame.text = Globals.Config.getTextConfig(gameID.ToString()).ToUpper();
        animQuickPlay.Initialize(true);
        animQuickPlay.AnimationState.SetAnimation(0, "coTag", true);
    }

    List<GAMEID> listSlot = new List<GAMEID>() { GAMEID.DOMINO, GAMEID.SLOT_SIXIANG, GAMEID.SLOTTARZAN, GAMEID.SLOT20FRUIT, GAMEID.SLOT_JUICY_GARDEN, GAMEID.SLOT_INCA, GAMEID.SLOTNOEL, GAMEID.SLOT_SIXIANG };

    void OnClickTab(Button btn)
    {
        SoundManager.instance.soundClick();
        if (!blockSpamTabGame)
        {
            var indexTab = 0;
            for (var i = 0; i < listTabs.Count; i++)
            {
                var gOn = listTabs[i].transform.GetChild(1);
                if (btn == listTabs[i])
                {
                    indexTab = i;
                    gOn.gameObject.SetActive(true);
                }
                else
                {
                    gOn.gameObject.SetActive(false);
                }
            }
            TabGame = indexTab;
            changeTabGame();
            blockSpamTabGame = true;
            DOTween.Kill("blockSpamTabGame");
            DOTween.Sequence().AppendInterval(1.0f).AppendCallback(() =>
            {
                blockSpamTabGame = false;
            }).SetId("blockSpamTabGame");
        }
    }
    private void changeTabGame()
    {
        ContentSizeFitter gameTabsCSF = scrListGame.content.GetComponent<ContentSizeFitter>();
        gameTabsCSF.enabled = true;
        if (TabGame == 0)
        {
            for (int i = 0; i < scrListGame.content.childCount; i++)
                scrListGame.content.GetChild(i).gameObject.SetActive(i != m_OnlySloticonTf.GetSiblingIndex());
        }
        else if (TabGame == 1)
        {
            for (int i = 0; i < scrListGame.content.childCount; i++)
                scrListGame.content.GetChild(i).gameObject.SetActive(i == m_OnlySloticonTf.GetSiblingIndex());
        }
        if (!gameObject.activeSelf) return;
        StartCoroutine(delay1FrameAndCheck()); //có trường hợp màn hình dài content nhỏ hơn viewport sẽ bị dồn lệch về 1 bên
        IEnumerator delay1FrameAndCheck()
        {
            yield return null;
            yield return null;
            if (scrListGame.content.rect.width < scrListGame.viewport.rect.width)
            {
                gameTabsCSF.enabled = false;
                scrListGame.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scrListGame.viewport.rect.width);
            }
            scrListGame.content.anchoredPosition = Vector2.zero;
        }
    }

    protected override void OnEnable()
    {
        CURRENT_VIEW.setCurView(CURRENT_VIEW.GAMELIST_VIEW);
        SoundManager.instance.playMusic();
        m_ConfigOn.SetActive(Config.is_dt);
        m_ConfigOff.SetActive(!Config.is_dt);
        if (Config.is_dt)
        {
            reloadListGame(); // clear button ondisable, bật lại ở đây cho nhẹ game, tăng performance khi chơi
            if (_AllGameIGs.Find(x => x.gameID == (int)GAMEID.PUSOY)) _GetInfoPusoyJackPotC = StartCoroutine(_GetJackpotPusoy());
            OnClickTab(listTabs[TabGame]);

            if (bannerLobbyContainer.pageCount > 0)
            {
                bannerLobbyContainer.gameObject.SetActive(true);
                setPosWhenBannerActive();
            }
        }
        else
        {
            foreach (ItemGame ig in m_ConfigOffIGs)
                ig.setInfo(int.Parse(ig.name), null, null, materialDefault, () => onClickGame(ig));
        }
        if (Config.isChangeTable)
        {
            Config.isChangeTable = false;
            if (Config.listGamePlaynow.Contains(Config.curGameId)) SocketSend.sendPlayNow(Config.curGameId);
            else SocketSend.sendChangeTable(Config.tableMark, Config.tableId);
        }
        if (AlertMessage.instance != null && AlertMessage.instance.gameObject.activeSelf)
            showAlert(true);
        if (Config.ket) updateAgSafe();
        if (isRunStart) onClickLobby();
    }
    private void OnDisable()
    {
        if (_GetInfoPusoyJackPotC != null) StopCoroutine(_GetInfoPusoyJackPotC);
        removeAllPopupNoti();
        ClearButtonGameOnDisable();
    }

    public void updateInfo()
    {
        //updateBannerNews();
        updateName();
        updateAg();
        updateAgSafe();
        updateAvatar();
        updateIdUser();
        //checkAlertMail();

        updateCanInviteFriend();

    }

    public void updateCanInviteFriend()
    {
        objDot.SetActive(User.userMain.canInputInvite);
    }
    private void setPosWhenBannerActive()
    {
        bool isActive = bannerLobbyContainer.gameObject.activeSelf;
        // modelLobby.transform.localPosition = new Vector2(isActive ? -65f : -250f, modelLobby.transform.localPosition.y);
        RectTransform rt = scrListGame.GetComponent<RectTransform>();
        if (isActive)
        {
            rt.offsetMin = new Vector2(370, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-70, rt.offsetMax.y);
        }
    }

    //Sequence seqLoopBanner;
    public async void showBanner()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
#else
        bannerLobbyContainer.Clear();
        bannerLobbyContainer.currentPage = 0;
        Debug.Log("Config.arrBannerLobby.Count==" + Config.arrBannerLobby.Count);
        bool isShow = Config.arrBannerLobby.Count > 0;
        bool updatePos = false;
        bannerLobbyContainer.gameObject.SetActive(isShow);
        if (!isShow) return;
        for (var i = 0; i < Config.arrBannerLobby.Count; i++)
        {
            var dataBanner = (JObject)Config.arrBannerLobby[i];
            dataBanner["isClose"] = false;
            var urlImg = (string)dataBanner["urlImg"];
            var index = i;
            Texture2D texture = await Globals.Config.GetRemoteTexture(urlImg);
            if (texture == null) return;
            var nodeBanner = Instantiate(bannerTemp).GetComponent<BannerView>();

            nodeBanner.isBannerType9 = true;
            nodeBanner.gameObject.SetActive(true);
            bannerLobbyContainer.AddPage(nodeBanner.GetComponent<RectTransform>());
            nodeBanner.setInfo(dataBanner, false);
            if (!updatePos)
            {
                setPosWhenBannerActive();
                updatePos = true;
            }
        }
#endif
    }

    float timeRun = 0;
    protected override void Update()
    {
        if (bannerLobbyContainer.pageCount > 1)
        {
            timeRun += Time.deltaTime;
            if (timeRun >= 5)
            {
                timeRun = 0;

                var page = bannerLobbyContainer.currentPage;
                page++;
                if (page >= bannerLobbyContainer.pageCount)
                {
                    page = 0;
                }
                bannerLobbyContainer.changeToPage(page);
            }
        }
    }

    Guid uid_action_center;
    public void showAlert(bool isShow)
    {
        //DOTween.Kill(uid_action_center);
        //uid_action_center = Guid.NewGuid();
        //DOTween.To(offsetNormal => CenterNode.offsetMax = new Vector2(0, offsetNormal), CenterNode.offsetMax.y, isShow ? -180 : -120, 0.3f).id = uid_action_center;
    }

    public void checkAlertMail(bool isEvt22 = true)
    {
        //Logging.Log("User.userMain.nmAg:" + User.userMain.nmAg);
        if ((User.userMain.nmAg > 0 || Promotion.countMailAg > 0) && !listShowPopupNoti.Contains("FREE_CHIP"))
        {
            listShowPopupNoti.Add("FREE_CHIP");
        }
        if (User.userMain.mailUnRead > 0 && !listShowPopupNoti.Contains("MAIL_ADMIN"))
        {
            listShowPopupNoti.Add("MAIL_ADMIN");
        }

        if (User.userMain.messageUnRead > 0 && !listShowPopupNoti.Contains("CHAT_PRIVATE"))
        {
            listShowPopupNoti.Add("CHAT_PRIVATE");
        }
        //if ((User.userMain.nmAg > 0 || Promotion.countMailAg > 0) && (FreeChipView.instance == null && ExchangeView.instance == null))// && User.userMain.isShowMailAg)
        //{
        //    //User.userMain.isShowMailAg = false;
        //    Action cb = null;
        //    if (User.userMain.mailUnRead > 0 && isEvt22)
        //    {
        //        cb = () =>
        //        {
        //            UIManager.instance.showDialog(Config.getTextConfig("has_mail_show_system"), Config.getTextConfig("txt_ok"), () =>
        //            {
        //                onClickMail();
        //            }, Config.getTextConfig("label_cancel"));
        //        };
        //    }

        //UIManager.instance.showDialog(Config.getTextConfig("has_mail_show_gold"), Config.getTextConfig("txt_free_chip"), () =>
        //{
        //    onClickFreechip();
        //}, Config.getTextConfig("label_cancel"), cb);
        checkShowPopupNoti();
        //}
    }
    public void removePopupNoti(string typePopup) //vao view do roi thi thoi khi back quay lai ko show popup nua.
    {
        if (listShowPopupNoti.Contains(typePopup))
        {
            listShowPopupNoti.Remove(typePopup);
        }
    }
    public void removeAllPopupNoti()
    {
        listShowPopupNoti.Clear();
    }
    public void checkShowPopupNoti()
    {
        if (listShowPopupNoti.Count > 0)
        {
            string typePopup = listShowPopupNoti[0];
            listShowPopupNoti.RemoveAt(0);
            switch (typePopup)
            {
                case "MAIL_ADMIN":
                    {

                        UIManager.instance.showDialog(Config.getTextConfig("has_mail_show_system"), Config.getTextConfig("txt_ok"), () =>
                        {
                            onClickMail();
                        }, Config.getTextConfig("label_cancel"), () =>
                        {
                            checkShowPopupNoti();
                        });
                        break;
                    }
                case "FREE_CHIP":
                    {
                        if (UIManager.instance.loginView.gameObject.activeSelf || UIManager.instance.gameView != null) return;
                        UIManager.instance.showDialog(Config.getTextConfig("has_mail_show_gold"), Config.getTextConfig("txt_free_chip"), () =>
                        {
                            onClickFreechip();
                        }, Config.getTextConfig("label_cancel"), () =>
                        {
                            checkShowPopupNoti();
                        });
                        break;
                    }
                case "CHAT_PRIVATE":
                    {
                        // UIManager.instance.showDialog(Config.getTextConfig("has_mail"), Config.getTextConfig("txt_ok"), () =>
                        // {
                        //     UIManager.instance.destroyAllPopup();
                        //     UIManager.instance.lobbyView.onShowChatWorld(true);
                        // }, Config.getTextConfig("label_cancel"), () =>
                        // {
                        //     checkShowPopupNoti();
                        // });
                        break;
                    }
            }
        }
    }
    public void updateName()
    {
        lb_name.text = User.userMain.displayName;
        Config.effectTextRunInMask(lb_name, true);
    }

    public void updateAg()
    {
        lb_ag.text = Config.FormatNumber(User.userMain.AG);
    }

    public void updateAgSafe()
    {
        lb_safe.text = Config.FormatNumber(User.userMain.agSafe);
    }
    public void updateIdUser()
    {
        lb_id.text = "ID:" + User.userMain.Userid;
    }
    public void updateAvatar()
    {
        //string fbId = "";
        //if (Config.typeLogin == LOGIN_TYPE.FACEBOOK)
        //{
        //    fbId = User.FacebookID;
        //}
        avatar.loadAvatar(User.userMain.Avatar, User.userMain.Username, User.FacebookID);
        avatar.setVip(User.userMain.VIP);
    }
    public void updateBannerNews()
    {
        bool isShow = Config.arrOnlistTrue.Count >= 1;
        Logging.LogWarning("updateBannerNews  " + isShow);
        Logging.LogWarning("arrOnlistTrue  " + Config.arrOnlistTrue.ToString());
        btnBannerNews.SetActive(isShow);
    }
    public void onClickNext(Button btnNex)
    {
        scrListGame.DOHorizontalNormalizedPos(1.0f, 0.2f).SetEase(Ease.OutSine);
        if (isHideBtnScroll) return;
        btnNex.gameObject.SetActive(false);
        btnPrevious.gameObject.SetActive(true);
    }
    public void onClickPrevious(Button btnPre)
    {
        scrListGame.DOHorizontalNormalizedPos(0.0f, 0.1f).SetEase(Ease.OutSine);
        if (isHideBtnScroll) return;
        btnPre.gameObject.SetActive(false);
        btnNext.gameObject.SetActive(true);
    }
    public void onScrollScrGame()
    {
        //Globals.Logging.Log(scrBet.horizontalNormalizedPosition);
        float posX = scrListGame.horizontalNormalizedPosition;
        if (isHideBtnScroll) return;
        float viewportWidth = scrListGame.viewport.GetComponent<RectTransform>().rect.width;
        float contentWidth = scrListGame.content.GetComponent<RectTransform>().rect.width;
        btnPrevious.gameObject.SetActive(viewportWidth < contentWidth && posX > 0.25f);
        btnNext.gameObject.SetActive(viewportWidth < contentWidth && posX < 0.75f);
    }
    public void onClickQuickPlay()
    {
        _AllGameIGs.ForEach(btnGame =>
        {
            if (btnGame.gameID == Globals.Config.lastGameIDSave)
            {
                Config.isPlayNowFromLobby = true;
                btnGame.onClick();
            }
        });
    }
    public void onClickBannerNews()
    {
        UIManager.instance.showPopupListBanner();
    }
    private void ClearButtonGameOnDisable()
    {
        foreach (Transform childTf in scrListGame.content) if (childTf != m_MiniGameIconTf && childTf != m_OnlySloticonTf) Destroy(childTf.gameObject);
        foreach (Transform childTf in m_MiniGameIconTf) Destroy(childTf.gameObject);
        foreach (Transform childTf in m_OnlySloticonTf) Destroy(childTf.gameObject);
    }
    void reloadListGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
#else
        ClearButtonGameOnDisable();
        for (int i = 0; i < Config.listGame.Count; i++)
        {
            JObject dt = new()
            {
                ["id"] = (int)Config.listGame[i]["id"],
                ["ip_dm"] = (string)Config.listGame[i]["ip_dm"],
            };
        }

        _AllGameIGs.Clear();
        Rect sizeCell = scrListGame.GetComponent<RectTransform>().rect;
        List<int> slotGames = new() { (int)GAMEID.DOMINO, (int)GAMEID.SLOT_SIXIANG, (int)GAMEID.SLOTTARZAN, (int)GAMEID.SLOTNOEL, (int)GAMEID.SLOT_INCA, (int)GAMEID.SLOT_JUICY_GARDEN, (int)GAMEID.SLOT20FRUIT };
        for (var i = 0; i < Config.listGame.Count; i++)
        {
            JObject data = (JObject)Config.listGame[i];
            int gameId = (int)data["id"];
            SkeletonDataAsset skeAsset = Resources.Load<SkeletonDataAsset>("AnimIconGame/" + gameId + "/skeleton_SkeletonData");
            if (skeAsset == null) continue;
            ItemGame item = null;
            switch (gameId)
            {
                case (int)GAMEID.LUCKY9:
                case (int)GAMEID.PUSOY:
                case (int)GAMEID.TONGITS_OLD:
                    item = Instantiate(gameItemObject, scrListGame.content).GetComponent<ItemGame>();
                    item.transform.SetSiblingIndex(0);
                    break;
                default:
                    item = Instantiate(gameItemObject, m_MiniGameIconTf).GetComponent<ItemGame>();

                    break;
            }
            item.name = gameId.ToString();
            item.transform.localScale = Vector3.one;
            item.transform.position = Vector3.zero;
            item.gameObject.SetActive(true);
            item.setInfo(gameId, skeAsset, null, materialDefault, () => onClickGame(item));
            if (gameId == (int)GAMEID.PUSOY && UIManager.instance.PusoyJackPot > 0) item.UpdateJackpot(UIManager.instance.PusoyJackPot);
            _AllGameIGs.Add(item);
        }
        foreach (ItemGame ig in _AllGameIGs)
        {
            if (!slotGames.Contains(ig.gameID)) continue;
            SkeletonDataAsset bigSlotGameSDA = Resources.Load<SkeletonDataAsset>("AnimIconGame/" + ig.gameID + "-big" + "/skeleton_SkeletonData");
            if (bigSlotGameSDA == null) continue;
            ItemGame bigSlotIconIG = Instantiate(gameItemObject, m_OnlySloticonTf).GetComponent<ItemGame>();

            bigSlotIconIG.name = ig.gameID.ToString();
            bigSlotIconIG.transform.localScale = Vector3.one;
            bigSlotIconIG.transform.position = Vector3.zero;
            bigSlotIconIG.gameObject.SetActive(true);
            bigSlotIconIG.setInfo(ig.gameID, bigSlotGameSDA, null, materialDefault, () => onClickGame(bigSlotIconIG));
        }
        changeTabGame();
#endif
    }
    private IEnumerator _GetJackpotPusoy()
    {
        while (User.userMain == null)
        {
            yield return new WaitForSeconds(.2f);
        }
        while (true)
        {
            SocketSend.sendUpdateJackpot((int)GAMEID.PUSOY);
            yield return new WaitForSeconds(5);
        }
    }

    public void onClickGameFromBanner(int gameID)
    {
        for (var i = 0; i < _AllGameIGs.Count; i++)
        {
            if (_AllGameIGs[i].gameID == gameID)
            {
                onClickGame(_AllGameIGs[i]);
                break;
            }
        }
    }

    public bool isClicked = false;
    void onClickGame(ItemGame itemGame)
    {
        // if (isClicked || (User.userMain.lastGameID != 0 && User.userMain.lastGameID != itemGame.gameID))
        if (isClicked)
        {
            Debug.Log(" Dang Click Game ròi " + isClicked + ", " + (User.userMain.lastGameID != 0)
            + ", " + (User.userMain.lastGameID != itemGame.gameID) + User.userMain.lastGameID + ", " + itemGame.gameID);
            return;
        }
        isClicked = true;
        DOTween.Sequence().AppendInterval(Config.curGameId != (int)GAMEID.SLOT_SIXIANG ? 0.5f : 2.8f).AppendCallback(() =>
        {
            isClicked = false;
            Globals.Config.isSendingSelectGame = false;

        });

        if (User.userMain.AG <= 0)
        {
            UIManager.instance.showPopupWhenLostChip(false, true);
            return;
        }
        Config.curGameId = itemGame.gameID;
        if (Config.curGameId != (int)GAMEID.SLOT_SIXIANG)
        {
            //game slot sixaing dùng service. k can select game. e đang thấy bị select game con này toàn bị treo trong bàn.
            Debug.Log("select game  " + itemGame.gameID);
            Globals.Config.isSendingSelectGame = false;
            SocketSend.sendSelectGame(itemGame.gameID);
        }
        else
        {
            UIManager.instance.playVideoSiXiang();
            SocketSend.sendSelectGame(itemGame.gameID);
        }
        if (Config.isShowTableWithGameId(Config.curGameId) && User.userMain.VIP >= 1)
        {
            if (Config.TELEGRAM_TOKEN.Equals("")) UIManager.instance.openTableView();
        }
    }


    public void onClickEX()
    {
        UIManager.instance.openEx();
    }

    public void onClickProfile()
    {
        UIManager.instance.openProfile();
    }

    public void onClickGiftcode()
    {
        onClickLobby();
        UIManager.instance.openGiftCode();
    }
    public void onClickSetting()
    {
        UIManager.instance.openSetting();
    }

    public void onClickLobby()
    {
        checkShowPopupNoti();
        CURRENT_VIEW.setCurView(CURRENT_VIEW.GAMELIST_VIEW);
    }
    public void onClickLeaderBoard()
    {
        UIManager.instance.openLeaderBoard();
    }

    public void onClickShop()
    {
        UIManager.instance.openShop();
    }

    public void onShowChatWorld(bool isTab)
    {
        UIManager.instance.openChatWorld();
    }
    public void onClickFreechip()
    {
        UIManager.instance.openFreeChipView();
    }
    public void onClickDailyBonus()
    {
        //if (isCheckOnButton(btnDailybonus)) return;
        //showTab(false, false, false, true, false, false);
        //UIManager.instance.openDailyBonus();
        UIManager.instance.showToast("COMMING SOON!");
    }
    public void onClickMail()
    {
        UIManager.instance.openMailView();
    }

    public void onClickSafe()
    {
        UIManager.instance.openSafeView();
    }
    public void OnCLickButtonLuckyNumber()
    {
        UIManager.instance.OpenLuckyNumber();
    }

    public void setTimeGetMoney()
    {
        if (Promotion.time <= 0)
        {
            lbTimeOnline.text = Config.getTextConfig("click_to_spin");
            SocketSend.sendPromotion();
        }
        else
        {
            lbTimeOnline.text = Config.convertTimeToString(Promotion.time);
            Promotion.time--;
            DOTween.Sequence().AppendInterval(1).AppendCallback(() =>
            {
                setTimeGetMoney();
            });
        }
    }
    public void updateMailandMessageNoti()
    {
        icNotiMail.SetActive(User.userMain.mailUnRead > 0);
        icNotiMessage.SetActive(User.userMain.messageUnRead > 0);
        icNotiFree.SetActive(User.userMain.nmAg > 0 || Promotion.countMailAg > 0);
    }
    public void UpdateJackpotPusoy()
    {
        ItemGame ig = _AllGameIGs.Find(x => x.gameID == (int)GAMEID.PUSOY);
        if (ig == null) return;
        ig.UpdateJackpot(UIManager.instance.PusoyJackPot);
    }
    public void setNotiMessage(bool state)
    {
        icNotiMessage.gameObject.SetActive(state);
    }

    public void refreshUIFromConfig(bool isStart = false)
    {
        btnEx.SetActive(Config.is_dt);
        var issket = Config.ket;
        if (User.userMain != null && User.userMain.VIP == 0)
        {
            issket = false;
        }
        iconSafe.SetActive(Config.ket);
        btnSafe.SetActive(issket);
        m_Lottery.SetActive(Config.enableLottery);
        lb_safe.transform.parent.gameObject.SetActive(issket);

        if (issket)
            updateAgSafe();
        btnLeaderboard.gameObject.SetActive(Config.listRankGame.Count > 0);
        btnGiftCode.SetActive(Config.ismaqt);
        if (User.userMain != null)
        {
            m_VipFarmBVF.gameObject.SetActive(User.userMain.VIP > 1);
        }
        if (!isStart)
            reloadListGame();
        if (!m_VipFarmBVF.gameObject.activeSelf && !btnEx.activeSelf && !btnBannerNews.activeSelf)
        {
            btnSupport.SetActive(false);
        }
        else
        {
            btnSupport.SetActive(true);
        }

        //setDefaultPosBtnMore();
    }
    public void onClickSupport()
    {
        SoundManager.instance.soundClick();
        if (!Config.fanpageID.Equals("") && Config.is_bl_fb)
        {
            Application.OpenURL("https://" + Config.u_chat_fb);
        }
        else
        {
            UIManager.instance.openFeedback();
        }
    }
    //bool isHideBot = false;
    public void updateBotWithScrollShop(Vector2 value)
    {
        //if (value.y <= 0.25f && !isHideBot)
        //{
        //    isHideBot = true;
        //    onShowHideBot(false);
        //}
        //else if (value.y >= 0.75f && isHideBot)
        //{
        //    isHideBot = false;
        //    onShowHideBot(true);
        //}
    }


    void onShowHideBot(bool isShow)
    {
        //tfBot.DOKill();
        //if (isShow)
        //{
        //    tfBot.DOLocalMoveY(BasePosBot.y, 0.2f);
        //}
        //else
        //{
        //    tfBot.DOLocalMoveY(BasePosBot.y - tfBot.rect.height, 0.2f);
        //}
    }

    public void resetLogout()
    {
        // modelLobby.SetActive(true);
        // modelLobby.GetComponent<SkeletonGraphic>().Initialize(true);
        // modelLobby.GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "animation", true);
        //scrollSnapView.gameObject.SetActive(false);
        bannerLobbyContainer.gameObject.SetActive(false);
        setPosWhenBannerActive();
        btnBannerNews.SetActive(false);
        TabGame = 0;
    }
}

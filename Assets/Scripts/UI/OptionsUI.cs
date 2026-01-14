using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    public static OptionsUI Instance { get; private set; }

    [SerializeField] private Button soundBtn;
    [SerializeField] private Button musicBtn;
    [SerializeField] private Button quitBtn;

    [SerializeField] private Text SoundText;
    [SerializeField] private Text musicText;

    [SerializeField] private Text moveUpTxt;
    [SerializeField] private Text moveDownTxt;
    [SerializeField] private Text moveLeftTxt;
    [SerializeField] private Text moveRightTxt;
    [SerializeField] private Text InteractTxt;
    [SerializeField] private Text InteractAltTxt;
    [SerializeField] private Text pauseTxt;

    [SerializeField] private Button moveUpBtn;
    [SerializeField] private Button moveDownBtn;
    [SerializeField] private Button moveLeftBtn;
    [SerializeField] private Button moveRightBtn;
    [SerializeField] private Button InteractBtn;
    [SerializeField] private Button InteractAltBtn;
    [SerializeField] private Button pauseBtn;

    [SerializeField] private Transform presstoRebindKeyTransform;

    private Action onCloseButtonAction;



    private void Awake()
    {
        Instance = this;
        soundBtn.onClick.AddListener(() =>
        {
            SoundMgr.GetInstance().ChangeVolume();
            UpdateVisual();

        });

        musicBtn.onClick.AddListener(() =>
        {
            MusMgr.GetInstance().ChangeVolume();
            UpdateVisual();

        });

        quitBtn.onClick.AddListener(() =>
        {
            Hide();
            onCloseButtonAction.Invoke();
        });

        moveUpBtn.onClick.AddListener(() => { RebindBinding(GameInputMgr.Binding.Move_Up); });
        moveDownBtn.onClick.AddListener(() => { RebindBinding(GameInputMgr.Binding.Move_Down); });
        moveLeftBtn.onClick.AddListener(() => { RebindBinding(GameInputMgr.Binding.Move_Left); });
        moveRightBtn.onClick.AddListener(() => { RebindBinding(GameInputMgr.Binding.Move_Right); });
        InteractBtn.onClick.AddListener(() => { RebindBinding(GameInputMgr.Binding.Interact); });
        InteractAltBtn.onClick.AddListener(() => { RebindBinding(GameInputMgr.Binding.InteractAlt); });
        pauseBtn.onClick.AddListener(() => { RebindBinding(GameInputMgr.Binding.Pause); });


    }

    private void Start()
    {
        GameMgr.GetInstance().OnGameUnpaused += GameMgr_OnGameUnpaused;
        UpdateVisual();
        Hide();
        HidePressToRebindKey();
    }

    private void GameMgr_OnGameUnpaused(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void UpdateVisual()
    {
        SoundText.text = "ÒôÐ§£º" + Mathf.Round(SoundMgr.GetInstance().GetVolume() * 10f);
        musicText.text = "ÒôÁ¿£º" + Mathf.Round(MusMgr.GetInstance().GetVolume() * 10f);


        moveUpTxt.text = GameInputMgr.GetInstance().GetBindingTxt(GameInputMgr.Binding.Move_Up);
        moveDownTxt.text = GameInputMgr.GetInstance().GetBindingTxt(GameInputMgr.Binding.Move_Down);
        moveLeftTxt.text = GameInputMgr.GetInstance().GetBindingTxt(GameInputMgr.Binding.Move_Left);
        moveRightTxt.text = GameInputMgr.GetInstance().GetBindingTxt(GameInputMgr.Binding.Move_Right);
        InteractTxt.text = GameInputMgr.GetInstance().GetBindingTxt(GameInputMgr.Binding.Interact);
        InteractAltTxt.text = GameInputMgr.GetInstance().GetBindingTxt(GameInputMgr.Binding.InteractAlt);
        pauseTxt.text = GameInputMgr.GetInstance().GetBindingTxt(GameInputMgr.Binding.Pause);

    }

    public void Show(Action onCloseButtonAction)
    {
        this.onCloseButtonAction = onCloseButtonAction;

        gameObject.SetActive(true);

        //musicBtn.Select();
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ShowPressToRebindKey()
    {
        presstoRebindKeyTransform.gameObject.SetActive(true);
    }

    private void HidePressToRebindKey()
    {
        presstoRebindKeyTransform.gameObject.SetActive(false);

    }

    private void RebindBinding(GameInputMgr.Binding binding)
    {
        ShowPressToRebindKey();
        GameInputMgr.GetInstance().RebindBinding(binding, () =>{
            HidePressToRebindKey();
            UpdateVisual();
                });
    }
}

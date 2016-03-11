﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ConfigBoardManager : MonoBehaviour {

    public Dropdown screenModeDropdown;
    public Slider bgmVolumeSlider;
    public Slider seVolumeSlider;
    public Slider voiceVolumeSlider;
    public Slider textSpeedSlider;
    public Slider autoSpeedSlider;

	// Use this for initialization
	void Start () {
        int screenMode = PlayerPrefs.GetInt(GameConstants.CONFIG_SCREEN_MODE, 0);
        float bgmVolume = PlayerPrefs.GetFloat(GameConstants.CONFIG_BGM_VOLUME, 0.5f);
        float seVolume = PlayerPrefs.GetFloat(GameConstants.CONFIG_SE_VOLUME, 0.5f);
        float voiceVolume = PlayerPrefs.GetFloat(GameConstants.CONFIG_VOICE_VOLUME, 0.5f);
        float textSpeed = PlayerPrefs.GetFloat(GameConstants.CONFIG_TEXT_SPEED, 0.5f);
        float autoSpeed = PlayerPrefs.GetFloat(GameConstants.CONFIG_AUTO_SPEED, 0.5f);

        screenModeDropdown.value = screenMode;
        bgmVolumeSlider.value = bgmVolume;
        seVolumeSlider.value = seVolume;
        voiceVolumeSlider.value = voiceVolume;
        textSpeedSlider.value = textSpeed;
        autoSpeedSlider.value = autoSpeed;
	}
}

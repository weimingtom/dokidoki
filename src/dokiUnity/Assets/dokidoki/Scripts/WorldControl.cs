﻿using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class WorldControl : MonoBehaviour {
    private ScriptReader scriptReader;

    private GameObject focusGameObject;

    public GameObject world;
    //In play UI gameobjects
    public GameObject dialogUI;
    public GameObject quickButtonsUI;
    public GameObject backLogUI;
    public GameObject saveBoardUI;
    public GameObject loadBoardUI;


    public GameObject characterPrefab;
    public GameObject logTextPrefab;
    public GameObject saveTextPrefab;
    public GameObject loadTextPrefab;

	public GameObject backLogContent;
    public GameObject saveContent;
    public GameObject loadContent;
	public GameObject dialogContent;
    public Dictionary<string, GameObject> characters;

    private List<Action> currentActions;
    private Action lastAction;

	public string currentGameState = NORMAL;
	const string NORMAL = "Normal";
	const string BACKLOG = "BackLog";
	const string SAVE = "Save";
	const string LOAD = "Load";
	const string AUTO = "Auto";
	const string SKIP = "Skip";
    const string HIDE = "Hide";

    public float nextAutoClickTime = 0f;

    public WorldControlData worldControlData = new WorldControlData();

    void Start() {
        //set up scriptReader, new game and load game
        if (scriptReader == null)
        {
            scriptReader = new ScriptReader();
        }

        if (world == null) {
            Debug.LogError(ScriptError.NOT_ASSIGN_GAMEOBJECT);
            Application.Quit();
        }

        characters = new Dictionary<string, GameObject>();

        if (worldControlData == null)
        {
            worldControlData = new WorldControlData();
        }
    }

    void FixedUpdate() {
        if(currentGameState == AUTO){
            float currentTime = Time.realtimeSinceStartup;
            //Auto click
            if (currentTime > nextAutoClickTime)
            {
                //Debug.Log("Auto click");
                //Click once, wait for next time update
                nextAutoClickTime = Mathf.Infinity;
                step();
            }
        }
    }

    //public int count = 0;
    /// <summary>
    /// Game click
    /// </summary>
    public void step() {
        //Debug.Log(++count);
        //Check game state first
        if(currentGameState == BACKLOG){
            clickBackLogButton();
            return;
        }
        else if (currentGameState == AUTO)
        {
            nextAutoClickTime = Mathf.Infinity;
        }else if(currentGameState == SAVE){
            clickSaveButton();
            return;
        }else if(currentGameState == LOAD){
            clickLoadButton();
            return;
        }else if(currentGameState == HIDE){
            clickHideButton();
            return;
        }

        //If in NORMAL state, plays the game normally
        if (currentActions == null || currentActions.Count < 1) {
            //To be done
            currentActions = scriptReader.testReadNextActions();
        }
        if (lastAction != null && lastAction.tag == ScriptKeyword.VIDEO) {
            world.GetComponent<World>().skipVideoAction();
            showInPlayUI();
        }
        while (currentActions.Count > 0)
        {
            Action currentAction = currentActions[0];
            if (currentAction.tag == ScriptKeyword.FOCUS)
            {
                this.takeFocusAction(currentAction);
            }
            if (focusGameObject == null) {
                Debug.LogError(ScriptError.NOT_FOCUS_OBJECT);
                return;
            }
            if (currentAction.tag == ScriptKeyword.BACKGROUND) {
                focusGameObject.GetComponent<World>().takeBackgroundAction(currentAction);
            }
            else if (currentAction.tag == ScriptKeyword.WEATHER) {
                focusGameObject.GetComponent<World>().takeWeatherAction(currentAction);
            }
            else if (currentAction.tag == ScriptKeyword.SOUND)
            {
                focusGameObject.GetComponent<World>().takeSoundAction(currentAction);
            }
            else if (currentAction.tag == ScriptKeyword.BGM)
            {
                focusGameObject.GetComponent<World>().takeBgmAction(currentAction);
            }
            else if (currentAction.tag == ScriptKeyword.VIDEO)
            {
                hideInPlayUI();
                updateNextAutoClickTime( focusGameObject.GetComponent<World>().takeVideoAction(currentAction));
            }
            else if (currentAction.tag == ScriptKeyword.TEXT)
            {
                if (focusGameObject.GetComponent<World>() != null) {
                    updateNextAutoClickTime( focusGameObject.GetComponent<World>().takeTextAction(currentAction));
                }
                if (focusGameObject.GetComponent<Character>() != null)
                {
                    updateNextAutoClickTime(focusGameObject.GetComponent<Character>().takeTextAction(currentAction));
                }
            }
            else if (currentAction.tag == ScriptKeyword.MOVE)
            {
                focusGameObject.GetComponent<Character>().takeMoveAction(currentAction);
            }
            else if (currentAction.tag == ScriptKeyword.POSTURE)
            {
                focusGameObject.GetComponent<Character>().takePostureAction(currentAction);
            }
            else if (currentAction.tag == ScriptKeyword.FACE)
            {
                focusGameObject.GetComponent<Character>().takeFaceAction(currentAction);
            }
            else if (currentAction.tag == ScriptKeyword.VOICE)
            {
                updateNextAutoClickTime( focusGameObject.GetComponent<Character>().takeVoiceAction(currentAction));
            }
            else if (currentAction.tag == ScriptKeyword.ROLE)
            {
                focusGameObject.GetComponent<Character>().takeRoleAction(currentAction);
            }
            //store last action
            lastAction = currentAction;
            if(currentAction.tag == ScriptKeyword.TEXT){
                worldControlData.textContent = currentAction.parameters[ScriptKeyword.CONTENT];
            }
            //remove already completed action
            currentActions.RemoveAt(0);
        }
    }

    public void takeFocusAction(Action focusAction) {

        worldControlData.focusGameObjectId = focusAction.parameters[ScriptKeyword.ID];

        focusGameObject = null;
        //focus on object to take further actions
        if (focusAction.parameters[ScriptKeyword.ID] == ScriptKeyword.WORLD)
        {
            focusGameObject = world;
        }
        else {
            if (!characters.ContainsKey(focusAction.parameters[ScriptKeyword.ID]))
            {
                //there is no character on this id, create one
                characters.Add(focusAction.parameters[ScriptKeyword.ID], createNewCharacter(focusAction.parameters[ScriptKeyword.ID]));
            }
            focusGameObject = characters[focusAction.parameters[ScriptKeyword.ID]];
        }
    }

    /// <summary>
    /// Create new character GameObject with id
    /// </summary>
    /// <param name="id">the id of new character in scripts</param>
    /// <returns>The GameObject reference of the new character</returns>
    public GameObject createNewCharacter(string id) {
        GameObject newCharacter = Instantiate(characterPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        newCharacter.transform.parent = this.world.transform;
        newCharacter.GetComponent<Character>().characterData.id = id;
        newCharacter.GetComponent<Character>().dialogText = this.world.GetComponent<World>().dialogText;
        return newCharacter;
    }

    public void hideInPlayUI() {
        dialogUI.SetActive(false);
        quickButtonsUI.SetActive(false);
    }

	public void showInPlayUI() {
        dialogUI.SetActive(true);
        quickButtonsUI.SetActive(true);
    }

    public void loadData(WorldControlData worldControlData) {
        this.worldControlData = worldControlData;

        Action loadedFocusAction = new Action(ScriptKeyword.FOCUS, new Dictionary<string, string>(){
            {ScriptKeyword.ID, worldControlData.focusGameObjectId}
        });
        this.takeFocusAction(loadedFocusAction);

        Action loadedTextAction = new Action(ScriptKeyword.TEXT, new Dictionary<string, string>(){
            {ScriptKeyword.CONTENT, worldControlData.textContent},
            {ScriptKeyword.TYPE, ScriptKeyword.CLICK_NEXT_DIALOGUE_PAGE}
        });
        if (focusGameObject.GetComponent<World>() != null)
        {
            updateNextAutoClickTime(focusGameObject.GetComponent<World>().takeTextAction(loadedTextAction));
        }
        if (focusGameObject.GetComponent<Character>() != null)
        {
            updateNextAutoClickTime(focusGameObject.GetComponent<Character>().takeTextAction(loadedTextAction));
        }
    }

    public GameObject createLogTextButton(Dialog dialog) { 
        GameObject newLogTextButton = Instantiate(logTextPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        newLogTextButton.transform.SetParent(this.backLogContent.transform);
        newLogTextButton.transform.localPosition = new Vector3(0, -newLogTextButton.GetComponent<RectTransform>().rect.height, 0);
        string dialogText = "";
        if (dialog.shownName != "")
        {
            dialogText = dialogText + dialog.shownName + ": ";
        }
        dialogText = dialogText + dialog.content;
        newLogTextButton.GetComponentInChildren<Text>().text = dialogText;
        return newLogTextButton;
    }

    public GameObject createTextButton(string text, GameObject prefab, GameObject parentGameObject, Func<System.Object, int> onclick, System.Object parameter)
    {

        GameObject newTextButton = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        newTextButton.transform.SetParent(parentGameObject.transform);
        newTextButton.transform.localPosition = new Vector3(0, -newTextButton.GetComponent<RectTransform>().rect.height, 0);
        newTextButton.GetComponentInChildren<Text>().text = text;
        newTextButton.GetComponent<Button>().onClick.AddListener(() => { onclick(parameter); });
        return newTextButton;
    }

    public void setupTextButtonBoard(List<string> texts, GameObject buttonPrefab, GameObject contentGameObject, bool toBottom, Func<System.Object, int> onclick, List<System.Object> parameters)
    {
        //Destroy all previous text buttons
        for (int i = 0; i < contentGameObject.transform.childCount; i++){
            GameObject.Destroy(contentGameObject.transform.GetChild(i).gameObject);
        }

        //Create a list of log text button
        List<GameObject> textButtons = new List<GameObject>();
        for (int i = 0; i < texts.Count; i++)
        {
            GameObject newTextButton = this.createTextButton(texts[i], buttonPrefab, contentGameObject, onclick, parameters[i]);
            if (textButtons.Count > 0){
                newTextButton.transform.localPosition = textButtons[textButtons.Count - 1].transform.localPosition;
                newTextButton.transform.localPosition = new Vector3(newTextButton.transform.localPosition.x,
                                                                newTextButton.transform.localPosition.y - newTextButton.GetComponent<RectTransform>().rect.height,
                                                                newTextButton.transform.localPosition.z);
            }
            textButtons.Add(newTextButton);
        }

        //Resize the backLogText
        float height = (textButtons.Count + 1) * textButtons[0].GetComponent<RectTransform>().rect.height;
        float width = contentGameObject.GetComponent<RectTransform>().rect.width;
        contentGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

        //Scroll to the bottom
        if(toBottom){
            contentGameObject.GetComponentInParent<ScrollRect>().normalizedPosition = new Vector2(0, 0);
        }
    }

//Quick button functions
	public void clickBackLogButton(){
        if (currentGameState == NORMAL)
        {
            //Open backlog window
            currentGameState = BACKLOG;
            backLogUI.SetActive(true);

            //Get texts to display
            List<Dialog> historyDialogs = dialogContent.GetComponent<DialogManage>().historyDialogs;
            List<string> texts = new List<string>();
            for (int i = 0; i < historyDialogs.Count; i++)
            {
                string dialogText = "";
                if (historyDialogs[i].shownName != "")
                {
                    dialogText = dialogText + historyDialogs[i].shownName + ": ";
                }
                dialogText = dialogText + historyDialogs[i].content;
                texts.Add(dialogText);
            }
            List<System.Object> parameters = new List<object>();
            for (int i = 0; i < historyDialogs.Count; i++)
            {
                parameters.Add(historyDialogs[i].voiceSrc);
            }
            //Set up log text buttons board
            setupTextButtonBoard(texts, logTextPrefab, backLogContent, true, onLogTextButtonClick, parameters); 
        }
        else if (currentGameState == BACKLOG)
        { 
            //close backlog window
            backLogUI.SetActive(false);
            currentGameState = NORMAL;
        }
	}

    public int onLogTextButtonClick(System.Object voiceSrc)
    {
        Debug.Log("voiceSrc: " + voiceSrc);
        return 0;
    }

    public void clickQuickSaveButton() {
        if (!EditorUtility.DisplayDialog("Do you want to quick save?", "This action would overwrite the original saved data.", "yes", "no")) {
            return;
        }
        saveTo(0);
    }

    public void clickQuickLoadButton() {
        if (!EditorUtility.DisplayDialog("Do you want to quick load?", "This action would lose current game data.", "yes", "no"))
        {
            return;
        }
        dialogContent.GetComponent<DialogManage>().clear();
        loadFrom(0);
    }

    public void clickSaveButton() {
        if (currentGameState == NORMAL)
        {
            currentGameState = SAVE;
            saveBoardUI.SetActive(true);

            List<string> texts = new List<string>();
            for (int i = 0; i < GameConstants.SAVE_SIZE; i++)
            {
                string text = "No." + (i + 1) + "\n" + GameConstants.SAVE_DEFAULT;
                texts.Add(text);
            }
            List<System.Object> parameters = new List<object>();
            for (int i = 0; i < GameConstants.SAVE_SIZE; i++)
            {
                parameters.Add(i + 1);
            }

            checkSavedData(texts);

            setupTextButtonBoard(texts, saveTextPrefab, saveContent, false, onSaveTextButtonClick, parameters);
        }
        else if (currentGameState == SAVE)
        {
            saveBoardUI.SetActive(false);
            currentGameState = NORMAL;
        }
    }

    public int onSaveTextButtonClick(System.Object position) {
        if (!EditorUtility.DisplayDialog("Do you want to quick save?", "This action would overwrite the original saved data.", "yes", "no"))
        {
            return 0;
        }
        saveTo((int)position);
        clickSaveButton();
        return 0;
    }

    public void clickLoadButton() {
        if (currentGameState == NORMAL)
        {
            currentGameState = LOAD;
            loadBoardUI.SetActive(true);

            List<string> texts = new List<string>();
            for (int i = 0; i < GameConstants.SAVE_SIZE; i++)
            {
                string text = "No." + (i + 1) + "\n" + GameConstants.SAVE_DEFAULT;
                texts.Add(text);
            }
            List<System.Object> parameters = new List<object>();
            for (int i = 0; i < GameConstants.SAVE_SIZE; i++)
            {
                parameters.Add(i + 1);
            }

            checkSavedData(texts);

            setupTextButtonBoard(texts, loadTextPrefab, loadContent, false, onLoadTextButtonClick, parameters);
        }
        else if (currentGameState == LOAD)
        {
            loadBoardUI.SetActive(false);
            currentGameState = NORMAL;
        }
    }

    public int onLoadTextButtonClick(System.Object position)
    {
        if (!EditorUtility.DisplayDialog("Do you want to load?", "This action would lose current game data.", "yes", "no"))
        {
            return 0;
        }
        loadFrom((int)position);
        clickLoadButton();
        return 0;
    }

    public void checkSavedData(List<string> texts) {
        //Check saved data
        string dirPath = Application.persistentDataPath + "/" + GameConstants.SAVE_DIRECTORY;
        string[] filePaths = Directory.GetDirectories(dirPath);
        for (int i = 0; i < filePaths.Length; i++)
        {
            string fileName = Path.GetFileName(filePaths[i]);
            int label;
            if (!Int32.TryParse(fileName, out label))
            {
                Debug.LogError("Saved directory name is modified");
            }
            if (label == 0)
            {
                continue;
            }
            //Read saved time from WorldControl.dat file
            try
            {
                BinaryFormatter bf = new BinaryFormatter();

                FileStream worldControlFile = File.Open(dirPath + "/" + label + "/" + GameConstants.WORLD_CONTROL + GameConstants.SAVE_FILE_EXTENSION, FileMode.Open);
                WorldControlData worldControlData = (WorldControlData)bf.Deserialize(worldControlFile);
                worldControlFile.Close();

                texts[label - 1] = "No." + (label) + "\n" + worldControlData.saveTime;
            }
            catch (IOException ex)
            {
                Debug.LogError("IO error when saving: " + ex.Message);
            }
        }
    }

    public void saveTo(int label) {
        this.worldControlData.saveTime = DateTime.Now.ToString("yyyy/MM/dd h:mm tt");

        string dirPath = Application.persistentDataPath + "/" + GameConstants.SAVE_DIRECTORY + "/" + label;
        Debug.Log("dirPath: " + dirPath);
        try
        {
            if (Directory.Exists(dirPath))
            {
                //Delete original saved files, then create new directory
                FileUtil.DeleteFileOrDirectory(dirPath);
            }
            Directory.CreateDirectory(dirPath);

            BinaryFormatter bf = new BinaryFormatter();

            WorldControlData worldControlData = this.GetComponent<WorldControl>().worldControlData;
            FileStream worldControlFile = File.Create(dirPath + "/" + GameConstants.WORLD_CONTROL + GameConstants.SAVE_FILE_EXTENSION);
            bf.Serialize(worldControlFile, worldControlData);
            worldControlFile.Close();

            WorldData worldData = world.GetComponent<World>().worldData;
            FileStream worldFile = File.Create(dirPath + "/" + ScriptKeyword.WORLD + GameConstants.SAVE_FILE_EXTENSION);
            bf.Serialize(worldFile, worldData);
            worldFile.Close();

            foreach (KeyValuePair<string, GameObject> idCharacterPair in characters)
            {
                CharacterData characterData = idCharacterPair.Value.GetComponent<Character>().characterData;
                FileStream characterFile = File.Create(dirPath + "/" + idCharacterPair.Key + GameConstants.SAVE_FILE_EXTENSION);
                bf.Serialize(characterFile, characterData);
                characterFile.Close();
            }
        }
        catch (IOException ex)
        {
            Debug.LogError("IO error when saving: " + ex.Message);
            EditorUtility.DisplayDialog("Save failed", "Please try again", "yes", "");
        }
    }

    public void loadFrom(int label) {
        string dirPath = Application.persistentDataPath + "/" + GameConstants.SAVE_DIRECTORY + "/" + label;
        try
        {
            if (!Directory.Exists(dirPath))
            {
                return;
            }

            BinaryFormatter bf = new BinaryFormatter();

            FileStream worldFile = File.Open(dirPath + "/" + ScriptKeyword.WORLD + GameConstants.SAVE_FILE_EXTENSION, FileMode.Open);
            WorldData worldData = (WorldData)bf.Deserialize(worldFile);
            worldFile.Close();

            //recover the world
            world.GetComponent<World>().loadData(worldData);

            List<CharacterData> characterDatas = new List<CharacterData>();
            Debug.Log("dirPath: " + dirPath);
            string[] filePaths = Directory.GetFiles(dirPath);

            for(int i=0; i<filePaths.Length; i++){
                if (filePaths[i].EndsWith(ScriptKeyword.WORLD + GameConstants.SAVE_FILE_EXTENSION) ||
                    filePaths[i].EndsWith(GameConstants.WORLD_CONTROL + GameConstants.SAVE_FILE_EXTENSION))
                {
                    continue;
                }
                FileStream characterFile = File.Open(filePaths[i], FileMode.Open);
                CharacterData characterData = (CharacterData)bf.Deserialize(characterFile);
                characterFile.Close();

                characterDatas.Add(characterData);
            }

            //recover characters in game
            foreach (KeyValuePair<string, GameObject> idCharacterPair in characters){
                Destroy(idCharacterPair.Value, 0f);
            }
            characters.Clear();

            //Create new needed characters
            for (int i = 0; i < characterDatas.Count; i++ )
            {
                GameObject newCharacter = createNewCharacter(characterDatas[i].id);
                this.characters.Add(characterDatas[i].id, newCharacter);

                newCharacter.GetComponent<Character>().loadData(characterDatas[i]);
            }

            //recover world control setting
            FileStream worldControlFile = File.Open(dirPath + "/" + GameConstants.WORLD_CONTROL + GameConstants.SAVE_FILE_EXTENSION, FileMode.Open);
            WorldControlData worldControlData = (WorldControlData)bf.Deserialize(worldControlFile);
            worldControlFile.Close();

            this.loadData(worldControlData);
        }
        catch (IOException ex)
        {
            Debug.LogError("IO error when saving: " + ex.Message);
            EditorUtility.DisplayDialog("Load failed", "Please try again", "yes", "");
        }
    }

    public void clickAutoButton() {
        if (currentGameState == NORMAL)
        {
            //Enter AUTO state
            currentGameState = AUTO;
            //Click once now
            nextAutoClickTime = 0f;
        }
        else if(currentGameState == AUTO){ 
            //Leave AUTO state
            currentGameState = NORMAL;
        }
    }

    /// <summary>
    /// Update the most long next auto click time, except for Mathf.Infinity
    /// </summary>
    /// <param name="newNextAutoClickTime">
    /// New next auto click time
    /// </param>
    public void updateNextAutoClickTime(float newNextAutoClickTime) {
        if (this.nextAutoClickTime == Mathf.Infinity)
        {
            this.nextAutoClickTime = newNextAutoClickTime;
        }
        else if (this.nextAutoClickTime < Mathf.Infinity)
        {
            this.nextAutoClickTime = Mathf.Max(this.nextAutoClickTime, newNextAutoClickTime);
        }
    }

    public void clickHideButton() { 
        if(currentGameState == NORMAL){
            currentGameState = HIDE;
            hideInPlayUI();
        }else if(currentGameState == HIDE){
            currentGameState = NORMAL;
            showInPlayUI();
        }
    }
}

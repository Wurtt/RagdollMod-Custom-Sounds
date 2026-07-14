using BepInEx;
using GorillaExtensions;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using Photon.Voice.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using GorillaLocomotion;
using RagdollMod.Patches;

namespace RagdollMod
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;

        public void Start()
        {
            instance = this;
            HarmonyPatches.ApplyHarmonyPatches();
        }

        private static AssetBundle assetBundle;
        public static GameObject LoadAsset(string assetName)
        {
            GameObject gameObject = null;

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RagdollMod.Resources.ragdoll");
            if (stream != null)
            {
                if (assetBundle == null)
                    assetBundle = AssetBundle.LoadFromStream(stream);

                gameObject = Instantiate<GameObject>(assetBundle.LoadAsset<GameObject>(assetName));
            }
            else
            {
                Debug.LogError("Failed to load asset from resource: " + assetName);
            }

            return gameObject;
        }

        public static Dictionary<string, AudioClip> audioPool = new Dictionary<string, AudioClip> { };
        public static AudioClip LoadSoundFromResource(string resourcePath)
        {
            AudioClip sound = null;

            if (!audioPool.ContainsKey(resourcePath))
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RagdollMod.Resources.ragdoll");
                if (stream != null)
                {
                    if (assetBundle == null)
                    {
                        assetBundle = AssetBundle.LoadFromStream(stream);
                    }
                    sound = assetBundle.LoadAsset(resourcePath) as AudioClip;
                    audioPool.Add(resourcePath, sound);
                }
                else
                {
                    Debug.LogError("Failed to load sound from resource: " + resourcePath);
                }
            }
            else
            {
                sound = audioPool[resourcePath];
            }

            return sound;
        }

        public static AudioClip LoadWavFromResource(string fileName)
        {
            AudioClip sound = null;

            try
            {
                if (!audioPool.ContainsKey(fileName))
                {
                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RagdollMod.Resources." + fileName);
                    if (stream != null)
                    {
                        byte[] wavData = new byte[stream.Length];
                        stream.Read(wavData, 0, (int)stream.Length);

                        AudioClip audioClip = WavUtility.ToAudioClip(wavData);
                        if (audioClip != null)
                        {
                            audioPool.Add(fileName, audioClip);
                            sound = audioClip;
                        }
                        else
                        {
                            Debug.LogError("Failed to convert WAV data to AudioClip: " + fileName);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to load WAV file from resource: " + fileName);
                    }
                }
                else
                {
                    sound = audioPool[fileName];
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception loading WAV file {fileName}: {e.Message}\n{e.StackTrace}");
            }

            return sound;
        }

        public static AudioClip LoadCustomDeathSound()
        {
            try
            {
                // Try to find the Gorilla Tag executable directory
                string gameDirectory = "";

                // Method 1: Try to get from game executable path
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                string exePath = currentProcess.MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    gameDirectory = System.IO.Path.GetDirectoryName(exePath);
                }

                // Method 2: Fallback - use Application.persistentDataPath and go up directories
                if (string.IsNullOrEmpty(gameDirectory) || !System.IO.Directory.Exists(gameDirectory))
                {
                    gameDirectory = System.IO.Directory.GetParent(Application.persistentDataPath)?.Parent?.FullName ?? "";
                }

                // Create mod folder path
                string modFolderPath = System.IO.Path.Combine(gameDirectory, "RagdollMod");

                // Create the folder if it doesn't exist
                if (!System.IO.Directory.Exists(modFolderPath))
                {
                    System.IO.Directory.CreateDirectory(modFolderPath);
                    Debug.Log($"Created RagdollMod folder at: {modFolderPath}");
                    return null; // Folder just created, no sound file yet
                }

                // Look for any .wav file in the folder
                string[] wavFiles = System.IO.Directory.GetFiles(modFolderPath, "*.wav");

                if (wavFiles.Length == 0)
                {
                    Debug.Log($"No WAV files found in RagdollMod folder: {modFolderPath}");
                    return null;
                }

                // Use the selected sound file, or default to first if index is invalid
                int indexToUse = Mathf.Clamp(selectedSoundIndex, 0, wavFiles.Length - 1);
                string soundFilePath = wavFiles[indexToUse];
                Debug.Log($"Loading custom death sound: {soundFilePath}");

                // Read the WAV file
                byte[] wavData = System.IO.File.ReadAllBytes(soundFilePath);
                AudioClip audioClip = WavUtility.ToAudioClip(wavData);

                if (audioClip != null)
                {
                    Debug.Log($"Successfully loaded custom death sound from: {System.IO.Path.GetFileName(soundFilePath)}");
                }
                else
                {
                    Debug.LogError($"Failed to convert custom sound to AudioClip: {soundFilePath}");
                }

                return audioClip;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading custom death sound: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        public static void RefreshAvailableSounds()
        {
            try
            {
                // Try to find the Gorilla Tag executable directory
                string gameDirectory = "";

                // Method 1: Try to get from game executable path
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                string exePath = currentProcess.MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    gameDirectory = System.IO.Path.GetDirectoryName(exePath);
                }

                // Method 2: Fallback - use Application.persistentDataPath and go up directories
                if (string.IsNullOrEmpty(gameDirectory) || !System.IO.Directory.Exists(gameDirectory))
                {
                    gameDirectory = System.IO.Directory.GetParent(Application.persistentDataPath)?.Parent?.FullName ?? "";
                }

                // Create mod folder path
                string modFolderPath = System.IO.Path.Combine(gameDirectory, "RagdollMod");

                // Create the folder if it doesn't exist
                if (!System.IO.Directory.Exists(modFolderPath))
                {
                    System.IO.Directory.CreateDirectory(modFolderPath);
                }

                // Look for any .wav file in the folder
                string[] wavFiles = System.IO.Directory.GetFiles(modFolderPath, "*.wav");

                // Convert full paths to just filenames
                availableSounds = new string[wavFiles.Length];
                for (int i = 0; i < wavFiles.Length; i++)
                {
                    availableSounds[i] = System.IO.Path.GetFileNameWithoutExtension(wavFiles[i]);
                }

                // Clamp selected index
                if (selectedSoundIndex >= availableSounds.Length)
                {
                    selectedSoundIndex = Mathf.Max(0, availableSounds.Length - 1);
                }

                Debug.Log($"Found {availableSounds.Length} sound files in RagdollMod folder");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error refreshing available sounds: {e.Message}");
                availableSounds = new string[] { };
            }
        }

        private static List<GameObject> portedCosmetics = new List<GameObject> { };
        public static void DisableCosmetics()
        {
            try
            {
                VRRig.LocalRig.transform.Find("rig/body_pivot/TransferrableItemLeftShoulder").gameObject.SetActive(false);
                VRRig.LocalRig.transform.Find("rig/body_pivot/TransferrableItemRightShoulder").gameObject.SetActive(false);
                VRRig.LocalRig.transform.Find("rig/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("Default");

                foreach (GameObject Cosmetic in VRRig.LocalRig.cosmetics)
                {
                    if (Cosmetic.activeSelf && Cosmetic.transform.parent == VRRig.LocalRig.mainCamera.transform.Find("HeadCosmetics"))
                    {
                        portedCosmetics.Add(Cosmetic);
                        Cosmetic.transform.SetParent(VRRig.LocalRig.headMesh.transform, false);
                        Cosmetic.transform.localPosition += new Vector3(0f, 0.1333f, 0.1f);
                    }
                }
            }
            catch { }
        }

        public static void EnableCosmetics()
        {
            VRRig.LocalRig.transform.Find("rig/body_pivot/TransferrableItemLeftShoulder").gameObject.SetActive(true);
            VRRig.LocalRig.transform.Find("rig/body_pivot/TransferrableItemRightShoulder").gameObject.SetActive(true);

            VRRig.LocalRig.transform.Find("rig/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("MirrorOnly");
            foreach (GameObject Cosmetic in portedCosmetics)
            {
                Cosmetic.transform.SetParent(VRRig.LocalRig.mainCamera.transform.Find("HeadCosmetics"), false);
                Cosmetic.transform.localPosition -= new Vector3(0f, 0.1333f, 0.1f);
            }

            portedCosmetics.Clear();
        }

        private Queue<Vector3> posHistory = new Queue<Vector3>();
        private Queue<float> posTimes = new Queue<float>();

        private void TrackVelocity()
        {
            posHistory.Enqueue(GorillaLocomotion.GTPlayer.Instance.transform.position);
            posTimes.Enqueue(Time.time);
            while (posTimes.Count > 0 && Time.time - posTimes.Peek() > 0.3f)
            {
                posHistory.Dequeue();
                posTimes.Dequeue();
            }
        }

        public Vector3 GetAverageVelocity()
        {
            if (posHistory.Count < 2) return Vector3.zero;
            float timeSpan = posTimes.ToArray()[posTimes.Count - 1] - posTimes.ToArray()[0];
            if (timeSpan <= 0f) return Vector3.zero;
            Vector3 lastPos = posHistory.ToArray()[posHistory.Count - 1];
            Vector3 firstPos = posHistory.ToArray()[0];
            return (lastPos - firstPos) / timeSpan;
        }

        public void Die()
        {
            if (Ragdoll != null)
                Destroy(Ragdoll);

            DisableCosmetics();

            endDeathSoundTime = Time.time + 5.265f;

            Ragdoll = LoadAsset("ragdoll");

            // Disable all audio sources in the ragdoll to prevent the old death sound
            AudioSource[] audioSources = Ragdoll.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource audio in audioSources)
            {
                audio.enabled = false;
            }

            Ragdoll.transform.Find("Stand/Gorilla Rig/body").transform.position = VRRig.LocalRig.transform.Find("rig/body_pivot").position;
            Ragdoll.transform.Find("Stand/Gorilla Rig/body").transform.rotation = VRRig.LocalRig.transform.Find("rig/body_pivot").rotation;

            Ragdoll.transform.Find("Stand/Gorilla Rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L").transform.position = VRRig.LocalRig.leftHand.rigTarget.transform.position;
            Ragdoll.transform.Find("Stand/Gorilla Rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L").transform.rotation = VRRig.LocalRig.leftHand.rigTarget.transform.rotation;

            Ragdoll.transform.Find("Stand/Gorilla Rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R").transform.position = VRRig.LocalRig.rightHand.rigTarget.transform.position;
            Ragdoll.transform.Find("Stand/Gorilla Rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R").transform.rotation = VRRig.LocalRig.rightHand.rigTarget.transform.rotation;

            if (ragdollVelocityEnabled)
            {
                Vector3 bodyVel = GetAverageVelocity();
                string[] velocitySets = new string[]
                {
                    "Stand/Gorilla Rig/body",
                    "Stand/Gorilla Rig/body/head",
                    "Stand/Gorilla Rig/body/shoulder.L",
                    "Stand/Gorilla Rig/body/shoulder.R",
                    "Stand/Gorilla Rig/body/shoulder.L/upper_arm.L",
                    "Stand/Gorilla Rig/body/shoulder.R/upper_arm.R",
                    "Stand/Gorilla Rig/body/shoulder.L/upper_arm.L/forearm.L",
                    "Stand/Gorilla Rig/body/shoulder.R/upper_arm.R/forearm.R",
                };
                foreach (string velocity in velocitySets)
                {
                    Ragdoll.transform.Find(velocity).GetComponent<Rigidbody>().linearVelocity = bodyVel;
                }

                Ragdoll.transform.Find("Stand/Gorilla Rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L").GetComponent<Rigidbody>().linearVelocity = GorillaLocomotion.GTPlayer.Instance.LeftHand.velocityTracker.GetAverageVelocity(true, 0);
                Ragdoll.transform.Find("Stand/Gorilla Rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L").GetComponent<Rigidbody>().angularVelocity = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/LeftHand Controller").GetOrAddComponent<GorillaVelocityEstimator>().angularVelocity;

                Ragdoll.transform.Find("Stand/Gorilla Rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R").GetComponent<Rigidbody>().linearVelocity = GorillaLocomotion.GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0);
                Ragdoll.transform.Find("Stand/Gorilla Rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R").GetComponent<Rigidbody>().angularVelocity = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/RightHand Controller").GetOrAddComponent<GorillaVelocityEstimator>().angularVelocity;
            }

            Ragdoll.transform.Find("Stand/Gorilla Rig/body/head").transform.rotation = GorillaTagger.Instance.headCollider.transform.rotation;

            VRRig.LocalRig.head.rigTarget.transform.rotation = Ragdoll.transform.Find("Stand/Gorilla Rig/body/head").transform.rotation;

            Transform standMesh = Ragdoll.transform.Find("Stand/Mesh");
            if (standMesh != null)
                standMesh.gameObject.SetActive(false);

            foreach (Renderer r in Ragdoll.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.materials)
                    m.renderQueue = 3000;
            }

            startForward = Ragdoll.transform.forward;

            if (uiCoroutine != null)
            {
                StopCoroutine(uiCoroutine);
                uiCoroutine = null;
            } else
            {
                uiCoroutine = StartCoroutine(ShowGModUI());
            }

            // Load and play custom death sound from RagdollMod folder
            try
            {
                AudioClip deathSound = LoadCustomDeathSound();

                if (deathSound != null)
                {
                    // Play sound locally so you can hear it
                    GameObject audioSourceObj = new GameObject("DeathSoundSource");
                    audioSourceObj.transform.SetParent(GorillaTagger.Instance.transform);
                    audioSourceObj.transform.localPosition = Vector3.zero;

                    AudioSource audioSource = audioSourceObj.AddComponent<AudioSource>();
                    audioSource.clip = deathSound;
                    audioSource.spatialBlend = 0f; // 2D sound so you always hear it clearly
                    audioSource.volume = 1f;
                    audioSource.pitch = 1f; // Normal pitch
                    audioSource.Play();

                    // Destroy after sound finishes
                    Destroy(audioSourceObj, deathSound.length);

                    // Set up voice recorder to broadcast the death sound to others
                    if (GorillaTagger.Instance.myRecorder != null)
                    {
                        GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.AudioClip;
                        GorillaTagger.Instance.myRecorder.AudioClip = deathSound;

                        // Play through voice so everyone hears it
                        GorillaTagger.Instance.myRecorder.RestartRecording(true);

                        // Calculate the end time based on sound length
                        endDeathSoundTime = Time.time + deathSound.length;

                        Debug.Log($"Custom death sound playing for {deathSound.length} seconds - Local + Voice");
                    }
                }
                else
                {
                    Debug.Log("No custom death sound loaded - ragdoll will be silent");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error playing death sound: {e.Message}\n{e.StackTrace}");
            }
        }

        public static Vector3 World2Player(Vector3 world)
        {
            return world - GorillaTagger.Instance.bodyCollider.transform.position + GorillaTagger.Instance.transform.position;
        }

        public bool GetRightJoystickDown()
        {
            if (IsSteam)
                return SteamVR_Actions.gorillaTag_RightJoystickClick.GetState(SteamVR_Input_Sources.RightHand);
            else
            {
                bool rightJoystickClick;
                ControllerInputPoller.instance.rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out rightJoystickClick);
                return rightJoystickClick;
            }
        }

        public bool hasInit;
        public bool IsSteam;
        public float endDeathSoundTime = -1f;
        public bool lastLeftHeld;
        public GameObject ui;
        public Coroutine uiCoroutine;

        public IEnumerator ShowGModUI()
        {
            ui = LoadAsset("UI");
            ui.transform.parent = GameObject.Find("Main Camera").transform;
            ui.transform.localPosition = Vector3.zero;
            ui.transform.localRotation = Quaternion.identity;

            ui.transform.Find("Cube/Canvas/Name").GetComponent<Text>().text = PhotonNetwork.NickName;
            ui.transform.Find("Cube/Canvas/Name/Shadow").GetComponent<Text>().text = PhotonNetwork.NickName;

            float startTime = Time.time + 5f;
            while (Time.time < startTime)
            {
                ui.transform.Find("Cube").gameObject.GetComponent<Renderer>().material.color = new Color(0.8980392157f, 0.2274509804f, 0.1294117647f, Mathf.Lerp(0f, 0.15f, (startTime - Time.time) / 5f));
                yield return null;
            }

            ui.transform.Find("Cube").gameObject.GetComponent<Renderer>().material.color = Color.clear;
            yield return new WaitForSeconds(5f);
            Destroy(ui);

            Coroutine thisCoroutine = uiCoroutine;
            uiCoroutine = null;
            StopCoroutine(thisCoroutine);
        }

        public Vector2 GetLeftJoystickAxis()
        {
            if (IsSteam)
                return SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.GetAxis(SteamVR_Input_Sources.LeftHand);
            else
            {
                Vector2 leftJoystick;
                ControllerInputPoller.instance.leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out leftJoystick);
                return leftJoystick;
            }
        }

        public void Update()
        {
            if (GorillaLocomotion.GTPlayer.Instance == null)
                return;

            if (!hasInit)
            {
                hasInit = true;
                IsSteam = Traverse.Create(PlayFabAuthenticator.instance).Field("platform").GetValue().ToString().ToLower() == "steam";
            }

            TrackVelocity();

            bool dying = GetRightJoystickDown() || UnityInput.Current.GetKey(KeyCode.B);
            if (dying && !lastLeftHeld)
            {
                isDead = !isDead;

                if (isDead)
                    Die();
            }

            lastLeftHeld = dying;

            if (UnityInput.Current.GetKeyDown(KeyCode.P))
            {
                showGui = !showGui;
            }

            // After the oof sound ends, restore microphone input
            if (Time.time > endDeathSoundTime && endDeathSoundTime > 0)
            {
                if (GorillaTagger.Instance.myRecorder != null)
                {
                    // Stop recording the audio clip
                    GorillaTagger.Instance.myRecorder.StopRecording();

                    // Switch back to microphone
                    GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.Microphone;
                    GorillaTagger.Instance.myRecorder.AudioClip = null;

                    // Restart recording with microphone input
                    GorillaTagger.Instance.myRecorder.RestartRecording(true);

                    Debug.Log("Microphone restored after oof sound");
                }
                endDeathSoundTime = -1;
            }

            if (isDead)
            {
                if (Ragdoll != null)
                {
                    UpdateRigPos();
                }
            }
            else
            {
                if (Ragdoll != null)
                {
                    EnableCosmetics();

                    posHistory.Clear();
                    posTimes.Clear();

                    Vector3 revivePos = Ragdoll.transform.Find("Stand/Gorilla Rig/body").position;
                    Destroy(Ragdoll);
                    Ragdoll = null;

                    if (GorillaTagger.Instance.myRecorder != null)
                    {
                        GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.Microphone;
                        GorillaTagger.Instance.myRecorder.AudioClip = null;
                    }

                    if (uiCoroutine != null)
                    {
                        StopCoroutine(uiCoroutine);
                        uiCoroutine = null;
                    }

                    if (ui != null)
                        Destroy(ui);

                    if (revivePos.y >= -10f)
                    {
                        GorillaLocomotion.GTPlayer.Instance.TeleportTo(World2Player(revivePos), GorillaLocomotion.GTPlayer.Instance.transform.rotation);
                    }
                }
            }
        }

        public void UpdateRigPos()
        {
            if (Ragdoll == null) return;

            Transform ragdollBody = Ragdoll.transform.Find("Stand/Gorilla Rig/body");
            if (ragdollBody == null) return;

            Vector3 bodyPos = ragdollBody.position;
            if (bodyPos.y < -10f) return;

            VRRig.LocalRig.transform.position = bodyPos;
            VRRig.LocalRig.transform.rotation = ragdollBody.rotation;

            Transform handL = ragdollBody.Find("shoulder.L/upper_arm.L/forearm.L/hand.L");
            Transform handR = ragdollBody.Find("shoulder.R/upper_arm.R/forearm.R/hand.R");
            Transform head = ragdollBody.Find("head");

            if (handL != null)
            {
                VRRig.LocalRig.leftHand.rigTarget.transform.position = handL.position;
                VRRig.LocalRig.leftHand.rigTarget.transform.rotation = handL.rotation * Quaternion.Euler(0f, 0f, 75f);
            }
            if (handR != null)
            {
                VRRig.LocalRig.rightHand.rigTarget.transform.position = handR.position;
                VRRig.LocalRig.rightHand.rigTarget.transform.rotation = handR.rotation * Quaternion.Euler(180f, 0f, -75f);
            }
            if (head != null)
            {
                VRRig.LocalRig.head.rigTarget.transform.position = head.position;
                VRRig.LocalRig.head.rigTarget.transform.rotation = head.rotation;
            }

            if (!freeMoveEnabled)
            {
                GorillaLocomotion.GTPlayer.Instance.TeleportTo(World2Player(bodyPos + startForward * 2f + new Vector3(0f, 2f, 0f)), GorillaLocomotion.GTPlayer.Instance.transform.rotation);
                GorillaTagger.Instance.leftHandTransform.position = GorillaTagger.Instance.bodyCollider.transform.position;
                GorillaTagger.Instance.rightHandTransform.position = GorillaTagger.Instance.bodyCollider.transform.position;
                GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
            }
        }

        public static void SyncRigToRagdoll(VRRig rig)
        {
            if (Ragdoll == null) return;

            Transform ragdollBody = Ragdoll.transform.Find("Stand/Gorilla Rig/body");
            if (ragdollBody == null) return;

            Vector3 bodyPos = ragdollBody.position;
            if (bodyPos.y < -10f) return;

            rig.transform.position = bodyPos;
            rig.transform.rotation = ragdollBody.rotation;

            Transform handL = ragdollBody.Find("shoulder.L/upper_arm.L/forearm.L/hand.L");
            Transform handR = ragdollBody.Find("shoulder.R/upper_arm.R/forearm.R/hand.R");
            Transform head = ragdollBody.Find("head");

            if (handL != null)
            {
                rig.leftHand.rigTarget.transform.position = handL.position;
                rig.leftHand.rigTarget.transform.rotation = handL.rotation * Quaternion.Euler(0f, 0f, 75f);
            }
            if (handR != null)
            {
                rig.rightHand.rigTarget.transform.position = handR.position;
                rig.rightHand.rigTarget.transform.rotation = handR.rotation * Quaternion.Euler(180f, 0f, -75f);
            }
            if (head != null)
            {
                rig.head.rigTarget.transform.position = head.position;
                rig.head.rigTarget.transform.rotation = head.rotation;
            }
        }

        public static Vector3 startForward;
        public static bool isDead;

        public static GameObject Ragdoll;

        public static bool showGui;
        public static bool showHintText = true;
        public static bool fbtEnabled = true;
        public static bool freeMoveEnabled = true;
        public static bool ragdollVelocityEnabled = true;

        // Sound selection variables
        public static string[] availableSounds = new string[] { };
        public static int selectedSoundIndex = 0;
        private static float lastSoundRefreshTime = 0f;

        public void OnGUI()
        {
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUI.Label(new Rect(0f, Screen.height - 20f, Screen.width, 20f), "Ragdoll Fix By: Inoxi");
            GUI.color = Color.white;

            if (showHintText && !showGui)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);
                GUI.Label(new Rect(Screen.width / 2f - 150f, 10f, 300f, 30f), "PRESS 'P' KEY TO OPEN GUI");
                GUI.color = Color.white;
            }

            if (!showGui) return;

            // Refresh sound list every 2 seconds
            if (Time.time - lastSoundRefreshTime > 2f)
            {
                RefreshAvailableSounds();
                lastSoundRefreshTime = Time.time;
            }

            float boxWidth = 350f;
            float boxHeight = availableSounds.Length > 0 ? 280f : 200f;
            float boxX = Screen.width / 2f - boxWidth / 2f;
            float boxY = Screen.height / 2f - boxHeight / 2f;

            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "RagdollMod Settings");

            ragdollVelocityEnabled = GUI.Toggle(new Rect(boxX + 20f, boxY + 30f, boxWidth - 40f, 30f), ragdollVelocityEnabled, " Ragdoll Velocity");
            freeMoveEnabled = GUI.Toggle(new Rect(boxX + 20f, boxY + 60f, boxWidth - 40f, 30f), freeMoveEnabled, " Free Move (walk while ragdolled)");
            showHintText = GUI.Toggle(new Rect(boxX + 20f, boxY + 90f, boxWidth - 40f, 30f), showHintText, " Show Hint Text");

            // Sound selection section
            if (availableSounds.Length > 0)
            {
                GUI.Label(new Rect(boxX + 20f, boxY + 125f, boxWidth - 40f, 25f), "Death Sound:");

                // Previous button
                if (GUI.Button(new Rect(boxX + 20f, boxY + 150f, 50f, 25f), "< Prev"))
                {
                    selectedSoundIndex--;
                    if (selectedSoundIndex < 0)
                        selectedSoundIndex = availableSounds.Length - 1;
                }

                // Display current sound name
                GUI.Label(new Rect(boxX + 75f, boxY + 150f, boxWidth - 150f, 25f), availableSounds[selectedSoundIndex]);

                // Next button
                if (GUI.Button(new Rect(boxX + boxWidth - 70f, boxY + 150f, 50f, 25f), "Next >"))
                {
                    selectedSoundIndex++;
                    if (selectedSoundIndex >= availableSounds.Length)
                        selectedSoundIndex = 0;
                }

                if (GUI.Button(new Rect(boxX + 100f, boxY + 235f, 100f, 25f), "Close (P)"))
                {
                    showGui = false;
                }
            }
            else
            {
                GUI.Label(new Rect(boxX + 20f, boxY + 125f, boxWidth - 40f, 60f), "No death sounds found.\nAdd .wav files to the RagdollMod folder.");

                if (GUI.Button(new Rect(boxX + 100f, boxY + 155f, 100f, 25f), "Close (P)"))
                {
                    showGui = false;
                }
            }
        }
    }
}

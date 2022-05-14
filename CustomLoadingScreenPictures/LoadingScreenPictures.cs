using CustomLoadingScreenPictures;
using MelonLoader;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;

[assembly: MelonInfo(typeof(LoadingScreenPictures), "CustomLoadingScreenPictures", "1.0.0", "Marijn", "https://github.com/")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace CustomLoadingScreenPictures
{
    internal class LoadingScreenPictures : MelonMod
    {
        private GameObject mainFrame;
        private GameObject cube;
        private Texture lastTexture;
        private Renderer screenRender, pic;
        private string folder_dir;
        private const string title = "CustomLoadingScreenPictures";
        private const string screenLocation = "/UserInterface/MenuContent/Popups/LoadingPopup/3DElements/LoadingInfoPanel/InfoPanel_Template_ANIM";
        private bool initUI = false;
        private bool enabled = true;
        private bool hidden = false;
        private float wait = 0.0f;
        private Vector3 originalSize;

        public override void OnApplicationStart()
        {
            MelonCoroutines.Start(UiManagerInitializer());
            // Select last created folder
            string default_dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\VRChat")
                .GetDirectories()
                .OrderByDescending(d => d.LastWriteTimeUtc)
                .First()
                .FullName;
            MelonPreferences.CreateCategory(title, "Loading Screen Pictures");
            MelonPreferences.CreateEntry(title, "directory", default_dir, "Folder to get pictures from");
            MelonPreferences.CreateEntry(title, "enabled", true, "Enable");

            if (default_dir != folder_dir && !Directory.Exists(folder_dir))
            {
                folder_dir = default_dir;
                MelonLogger.Msg("Couldn't find configured directory, using default directory");
            }
        }

        public IEnumerator UiManagerInitializer()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null) yield return null;
            Setup();
            initUI = true;
        }

        public override void OnPreferencesSaved()
        {
            enabled = MelonPreferences.GetEntryValue<bool>(title, "enabled");
            if (enabled) Setup();
            else Disable();
        }

        public override void OnUpdate()
        {
            if (!enabled) return;

            if (Time.time > wait)
            {
                wait += 5f;
                if (hidden)
                {
                    hidden = false;
                    Setup();
                }
            }

            if (lastTexture == null) return;
            if (lastTexture == screenRender.material.mainTexture) return;
            lastTexture = screenRender.material.mainTexture;
            ChangePic();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            switch (buildIndex)
            {
                case 1:
                case 2:
                default: // Causes this to run only once instead of multiple times
                    if (initUI && lastTexture == null) Setup();
                    break;
            }
        }

        private void ChangePic()
        {
            Texture2D texture = new Texture2D(2, 2);
            ImageConversion.LoadImage(texture, File.ReadAllBytes(randImage()));
            pic.material.mainTexture = texture;
            if (pic.material.mainTexture.height > pic.material.mainTexture.width)
            {
                cube.transform.localScale = new Vector3(0.099f, 1, 0.175f);
                mainFrame.transform.localScale = new Vector3(10.80f, 19.20f, 1);
            }
            else
            {
                cube.transform.localScale = new Vector3(0.175f, 1, 0.099f);
                mainFrame.transform.localScale = new Vector3(19.20f, 10.80f, 1);
            }
        }

        private void Disable()
        {
            MelonLogger.Msg("Disabled");
            if (mainFrame) mainFrame.transform.localScale = originalSize;
            if (screenRender) screenRender.enabled = true;
            if (cube) GameObject.Destroy(cube);
            lastTexture = null;
        }

        private void Setup()
        {
            if (!enabled || lastTexture != null) return;

            mainFrame = GameObject.Find($"{screenLocation}/SCREEN");
            originalSize = mainFrame.transform.localScale;

            GameObject screen = GameObject.Find($"{screenLocation}/mainScreen");
            // Check if folder is empty
            string imageLink = randImage();
            if (imageLink == null)
            {
                MelonLogger.Msg($"No screenshots found in: {folder_dir}");
                return;
            }

            GameObject parentScreen = GameObject.Find($"{screenLocation}/SCREEN");
            screenRender = screen.GetComponent<Renderer>();
            lastTexture = screenRender.material.mainTexture;

            // Create a new image
            cube = GameObject.CreatePrimitive(PrimitiveType.Plane);
            cube.transform.SetParent(parentScreen.transform);
            cube.transform.rotation = screen.transform.rotation;
            cube.transform.localPosition = new Vector3(0, 0, -0.19f);
            cube.GetComponent<Collider>().enabled = false;
            cube.layer = LayerMask.NameToLayer("InternalUI");
            Texture2D texture = new Texture2D(2, 2);
            ImageConversion.LoadImage(texture, File.ReadAllBytes(imageLink));
            pic = cube.GetComponent<Renderer>();
            pic.material.mainTexture = texture;

            // Disable original picture
            screenRender.enabled = false;

            // Resize frame
            if (pic.material.mainTexture.height > pic.material.mainTexture.width)
            {
                cube.transform.localScale = new Vector3(0.099f, 1, 0.175f);
                mainFrame.transform.localScale = new Vector3(10.80f, 19.20f, 1);
            }
            else
            {
                cube.transform.localScale = new Vector3(0.175f, 1, 0.099f);
                mainFrame.transform.localScale = new Vector3(19.20f, 10.80f, 1);
            }

            // Hide icon & title
            GameObject.Find($"{screenLocation}/ICON").active = false;
            GameObject.Find($"{screenLocation}/TITLE").active = false;

            MelonLogger.Msg("Setup Game Objects.");
        }

        private string randImage()
        {
            if (!Directory.Exists(folder_dir)) return null;
            string[] pics = Directory.GetFiles(folder_dir, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".png") || s.EndsWith(".jpeg")).ToArray();
            if (pics.Length == 0) return null;
            int randPic = new Il2CppSystem.Random().Next(0, pics.Length);
            return pics[randPic].ToString();
        }
    }
}
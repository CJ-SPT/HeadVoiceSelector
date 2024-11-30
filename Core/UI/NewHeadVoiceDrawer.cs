#if !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HeadVoiceSelector.Utils;
using SPT.Reflection.Utils;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace HeadVoiceSelector.Core.UI
{
    internal abstract class NewVoiceHeadDrawers
    {
        private static bool customizationDrawersCloned;
        private static readonly List<EquipmentSlot> _hiddenSlots =
        [
            EquipmentSlot.Earpiece,
            EquipmentSlot.Eyewear,
            EquipmentSlot.FaceCover,
            EquipmentSlot.Headwear
        ];

        private static readonly CompositeDisposableClass compositeDisposableClass = new CompositeDisposableClass();
        private static MongoID[] _availableCustomizations;
        private static readonly Dictionary<int, TagBank> _voices = new Dictionary<int, TagBank>();
        private static int _selectedHeadIndex;
        private static int _selectedVoiceIndex;
        private static GClass786<EquipmentSlot, PlayerBody.GClass2076> slotViews;
        private static List<KeyValuePair<string, GClass3317>> _headTemplates;
        private static List<KeyValuePair<string, GClass3321>> _voiceTemplates;
        private static GameObject _overallScreen;

        
        public static async Task AddCustomizationDrawers(OverallScreen overallScreen)
        {
            try
            {
                if (customizationDrawersCloned)
                {
        #if DEBUG
                    Console.WriteLine("Customization drawers already cloned.");
        #endif
                    return;
                }

                GameObject overallScreenGameObject = overallScreen.gameObject;
                _overallScreen = overallScreenGameObject;
                Transform leftSide = overallScreenGameObject.transform.Find("LeftSide");
                Transform clothingPanel = leftSide.transform.Find("ClothingPanel");

                if (clothingPanel == null || leftSide == null)
                {
                    Console.WriteLine("customizationDrawersPrefab or overallParent not found");
                    return;
                }

                GameObject clonedCustomizationDrawers = Object.Instantiate(clothingPanel.gameObject, leftSide);
                clonedCustomizationDrawers.gameObject.name = "NewHeadVoiceCustomizationDrawers";

                Vector3 newPosition = clonedCustomizationDrawers.transform.localPosition;
                newPosition.y -= 50f;
                clonedCustomizationDrawers.transform.localPosition = newPosition;

                customizationDrawersCloned = true;

                if (clonedCustomizationDrawers == null)
                {
                    Console.WriteLine("clonedCustomizationDrawers is null");
                    return;
                }

                Transform headTransform = clonedCustomizationDrawers.transform.Find("Upper");
                Transform voiceTransform = clonedCustomizationDrawers.transform.Find("Lower");

                if (headTransform == null || voiceTransform == null)
                {
                    Console.WriteLine("headTransform or voiceTransform are null");
                    return;
                }

                headTransform.gameObject.name = "Head";
                voiceTransform.gameObject.name = "Voice";

                Transform headIconTransform = clonedCustomizationDrawers.transform.Find("Head/Icon");
                Transform voiceIconTransform = clonedCustomizationDrawers.transform.Find("Voice/Icon");

                if (headIconTransform != null && voiceIconTransform != null)
                {
                    Image headIcon = headIconTransform.GetComponent<Image>();
                    Image voiceIcon = voiceIconTransform.GetComponent<Image>();

                    var headIconPng = Path.Combine(HeadVoiceSelector.pluginPath, "WTT-HeadVoiceSelector", "Icons", "icon_face_selector.png");
                    var voiceIconPng = Path.Combine(HeadVoiceSelector.pluginPath, "WTT-HeadVoiceSelector", "Icons", "icon_voice_selector.png");

                    byte[] headIconByte = File.ReadAllBytes(headIconPng);
                    byte[] voiceIconByte = File.ReadAllBytes(voiceIconPng);

                    Texture2D headIconTexture = new Texture2D(2, 2);
                    Texture2D voiceIconTexture = new Texture2D(2, 2);

                    headIconTexture.LoadImage(headIconByte);
                    voiceIconTexture.LoadImage(voiceIconByte);

                    Sprite headIconSprite = Sprite.Create(headIconTexture, new Rect(0, 0, headIconTexture.width, headIconTexture.height), Vector2.zero);
                    Sprite voiceIconSprite = Sprite.Create(voiceIconTexture, new Rect(0, 0, voiceIconTexture.width, voiceIconTexture.height), Vector2.zero);

                    headIcon.sprite = headIconSprite;
                    voiceIcon.sprite = voiceIconSprite;
                }

                Transform headSelectorTransform = clonedCustomizationDrawers.transform.Find("Head/ClothingSelector");
                Transform voiceSelectorTransform = clonedCustomizationDrawers.transform.Find("Voice/ClothingSelector");

                if (headSelectorTransform == null || voiceSelectorTransform == null)
                {
                    Console.WriteLine("headSelectorTransform or voiceSelectorTransform is null");
                    return;
                }

                headSelectorTransform.gameObject.name = "HeadSelector";
                voiceSelectorTransform.gameObject.name = "VoiceSelector";

                DropDownBox headDropDownBox = headSelectorTransform.GetComponent<DropDownBox>();
                DropDownBox voiceDropDownBox = voiceSelectorTransform.GetComponent<DropDownBox>();

                if (headDropDownBox == null || voiceDropDownBox == null)
                {
                    Console.WriteLine("headDropdownBox or voiceDropdownBox is null");
                    return;
                }

                await getAvailableCustomizations(Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession());
                InitCustomizationDropdowns(_availableCustomizations, headDropDownBox, voiceDropDownBox);
                setupCustomizationDrawers(headDropDownBox, voiceDropDownBox);

                clonedCustomizationDrawers.gameObject.SetActive(true);

        #if DEBUG
                Console.WriteLine("Successfully cloned and setup new customization dropdowns!");
        #endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void InitCustomizationDropdowns(MongoID[] availableCustomizations, DropDownBox _headSelector, DropDownBox _voiceSelector)
        {
            try
            {

                compositeDisposableClass.Dispose();

                _availableCustomizations = availableCustomizations;

                compositeDisposableClass.SubscribeEvent(_headSelector.OnValueChanged, selectHeadEvent);

                compositeDisposableClass.SubscribeEvent(_voiceSelector.OnValueChanged, selectVoiceEvent);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during initialization: {ex.Message}");
            }
        }

        private static async Task getAvailableCustomizations(ISession session)
        {
            try
            {
                MongoID[] result = await session.GetAvailableAccountCustomization();
                _availableCustomizations = result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void setupCustomizationDrawers(DropDownBox _headSelector, DropDownBox _voiceSelector)
        {
            try
            {
                GClass1597 instance = Singleton<GClass1597>.Instance;

                if (instance == null)
                {
                    Console.WriteLine("GClass1597 instance is null.");
                    return;
                }

                _headTemplates = [];
                _voiceTemplates = [];

                if (_availableCustomizations == null)
                {
                    Console.WriteLine("_availableCustomizations is null.");
                    return;
                }

                foreach (MongoID mongoID in _availableCustomizations)
                {
                    GClass3316 anyCustomizationItem = instance.GetAnyCustomizationItem(mongoID);
                    if (anyCustomizationItem == null)
                    {
                        Console.WriteLine("anyCustomizationItem is null.");
                        continue;
                    }

                    if (anyCustomizationItem.Side == null)
                    {
                        Console.WriteLine("anyCustomizationItem.Side is null.");
                        continue;
                    }

                    if (PatchConstants.BackEndSession.Profile == null)
                    {
                        Console.WriteLine("profile is null.");
                        continue;
                    }

                    if (!anyCustomizationItem.Side.Contains(PatchConstants.BackEndSession.Profile.Side))
                    {
        #if DEBUG
                        Console.WriteLine($"Player side {PatchConstants.BackEndSession.Profile.Side} is not contained in anyCustomizationItem.Side.");
        #endif
                        continue;
                    }

                    if (anyCustomizationItem is GClass3317 gclass)
                    {
                        if (gclass.BodyPart == EBodyModelPart.Head)
                        {
                            _headTemplates.Add(new KeyValuePair<string, GClass3317>(mongoID, gclass));
        #if DEBUG
                            Console.WriteLine($"Added head customization template: {mongoID}");
        #endif
                        }
                    }
                    else if (anyCustomizationItem is GClass3321 gclass2)
                    {
                        _voiceTemplates.Add(new KeyValuePair<string, GClass3321>(mongoID, gclass2));
        #if DEBUG
                        Console.WriteLine($"Added voice customization template: {mongoID}");
        #endif
                    }
                }

        #if DEBUG
                Console.WriteLine($"Added {_headTemplates.Count} head customization templates.");
                Console.WriteLine($"Added {_voiceTemplates.Count} voice customization templates.");
        #endif

                _voices.Clear();

                if (_headSelector == null)
                {
                    Console.WriteLine("Head dropdown is null.");
                    return;
                }
                setupHeadDropdownInfo(_headSelector);

                if (_voiceSelector == null)
                {
                    Console.WriteLine("Voice dropdown is null.");
                    return;
                }
                setupVoiceDropdownInfo(_voiceSelector);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during customization drawers setup: {ex.Message}");
            }
        }

        private static void setupHeadDropdownInfo(DropDownBox _headSelector)
        {
            try
            {

                string text = PatchConstants.BackEndSession.Profile.Customization[EBodyModelPart.Head];

                int num = 0;
                while (num < _headTemplates.Count && _headTemplates[num].Key != text)
                {
                    num++;
                }

                _selectedHeadIndex = num;

                _headSelector.Show(initializeHeadDropdown);

                _headSelector.UpdateValue(_selectedHeadIndex, false);

                PatchConstants.BackEndSession.Profile.Customization[EBodyModelPart.Head] = _headTemplates[_selectedHeadIndex].Key;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during head dropdown info setup: {ex.Message}");
            }
        }

        private static void setupVoiceDropdownInfo(DropDownBox _voiceSelector)
        {
            try
            {
                string currentVoice = PatchConstants.BackEndSession.Profile.Info.Voice;

                int selectedIndex = _voiceTemplates.FindIndex(v => v.Value.Name == currentVoice);

                if (selectedIndex == -1)
                {
                    Console.WriteLine($"Current voice '{currentVoice}' not found in the voice templates.");
                    return;
                }

                _voiceSelector.Show(initializeVoiceDropdown);

                _voiceSelector.UpdateValue(selectedIndex, false);

                PatchConstants.BackEndSession.Profile.Info.Voice = _voiceTemplates[selectedIndex].Value.Name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during voice dropdown info setup: {ex.Message}");
            }
        }

        private static IEnumerable<string> initializeHeadDropdown()
        {
            return _headTemplates.Select(getLocalizedHead).ToArray();
        }

        private static IEnumerable<string> initializeVoiceDropdown()
        {
            return _voiceTemplates.Select(getLocalizedVoice).ToArray();
        }

        private static string getLocalizedHead(KeyValuePair<string, GClass3317> x)
        {
#if DEBUG
            Console.WriteLine($"Localizing head: {x.Key}");
#endif
            return x.Value.NameLocalizationKey.Localized();
        }

        private static string getLocalizedVoice(KeyValuePair<string, GClass3321> x)
        {
#if DEBUG
            Console.WriteLine($"Localizing voice: {x.Key}");
#endif
            return x.Value.NameLocalizationKey.Localized();
        }

        private static void selectHeadEvent(int selectedIndex)
        {
            try
            {
#if DEBUG
                Console.WriteLine($"Selecting head event for index: {selectedIndex}");
#endif
                if (selectedIndex == _selectedHeadIndex)
                {
#if DEBUG
                    Console.WriteLine("Selected head index is already set.");
#endif
                    return;
                }

                _selectedHeadIndex = selectedIndex;
                string key = _headTemplates[_selectedHeadIndex].Key;
                PatchConstants.BackEndSession.Profile.Customization[EBodyModelPart.Head] = key;
#if DEBUG
                Console.WriteLine($"Head customization updated to: {key}");
#endif
                showPlayerPreview().HandleExceptions();




                WTTChangeHead(key);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during select head event: {ex.Message}");
            }
        }

        private static async Task showPlayerPreview()
        {
            try
            {
                Transform leftSide = _overallScreen.transform.Find("LeftSide");
                if (leftSide == null)
                {
                    Console.WriteLine("Overall screen parent not found.");
                    return;
                }

                Transform characterPanel = leftSide.transform.Find("CharacterPanel");
                PlayerModelView playerModelViewScript = characterPanel.GetComponentInChildren<PlayerModelView>();

                InventoryPlayerModelWithStatsWindow inventoryPlayerModelWithStatsWindow = leftSide.GetComponent<InventoryPlayerModelWithStatsWindow>();
                if (inventoryPlayerModelWithStatsWindow == null)
                {
                    Console.WriteLine("InventoryPlayerModelWithStatsWindow component not found.");
                    return;
                }

                await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, inventoryPlayerModelWithStatsWindow.method_5);

                changeSelectedHead(false, playerModelViewScript);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during player preview: {ex.Message}");
            }
        }

        private static void changeSelectedHead(bool active, PlayerModelView playerModelView)
        {
            try
            {

                slotViews = playerModelView.PlayerBody.SlotViews;
                foreach (GameObject gameObject in _hiddenSlots.Where(getSlotType).Select(getSlotKey).Where(getModel))
                {
                    gameObject.SetActive(active);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during change selected head: {ex.Message}");
            }
        }

        private static bool getSlotType(EquipmentSlot slotType)
        {
            return slotViews.ContainsKey(slotType);
        }

        private static GameObject getSlotKey(EquipmentSlot slotType)
        {
            return slotViews.GetByKey(slotType).ParentedModel.Value;
        }

        private static bool getModel(GameObject model)
        {
            return model != null;
        }

        private static void selectVoiceEvent(int selectedIndex)
        {
            try
            {
                _selectedVoiceIndex = selectedIndex;
                selectVoice(selectedIndex).HandleExceptions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during select voice event: {ex.Message}");
            }
        }

        private static async Task selectVoice(int selectedIndex)
        {
            try
            {
                if (!_voices.TryGetValue(selectedIndex, out _))
                {
                    TagBank result = await Singleton<GClass868>.Instance.TakeVoice(_voiceTemplates[_selectedVoiceIndex].Value.Name);
                    _voices.Add(selectedIndex, result);
                    if (result == null)
                    {
                        Console.WriteLine($"Voice not available for index: {selectedIndex}");
                        return;
                    }
                }
                string key = _voiceTemplates[_selectedVoiceIndex].Value.Name;

                PatchConstants.BackEndSession.Profile.Info.Voice = key;


                int num = Random.Range(0, _voices[selectedIndex].Clips.Length);
                TaggedClip taggedClip = _voices[selectedIndex].Clips[num];
                await Singleton<GUISounds>.Instance.ForcePlaySound(taggedClip.Clip);


                WTTChangeVoice(key);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during select voice: {ex.Message}");
            }
        }
        
        // Routes to handle server changes
        private static void WTTChangeHead(string id)
        {
            if (id == null)
            {
                Console.WriteLine("Error: id is null.");
                return;
            }

            var response = WebRequestUtils.Post<string>("/WTT/WTTChangeHead", id);
            if (response != null)
            {
                Console.WriteLine("HeadVoiceSelector: Change Head Route has been requested");
            }

        }

        private static void WTTChangeVoice(string id)
        {
            if (id == null)
            {
                Console.WriteLine("Error: id is null.");
                return;
            }
#if DEBUG
            Console.WriteLine($"WTTChangeVoice: id = {id}");
#endif
            var response = WebRequestUtils.Post<string>("/WTT/WTTChangeVoice", id);
            if (response != null)
            {
                Console.WriteLine("'HeadVoiceSelector': Change Voice Route has been requested");
            }
        }


    }
}

#endif
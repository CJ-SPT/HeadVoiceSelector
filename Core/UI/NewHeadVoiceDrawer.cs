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

        private static readonly CompositeDisposableClass compositeDisposableClass = new();
        private static MongoID[] _availableCustomizations;
        private static readonly Dictionary<int, TagBank> _voices = [];
        private static int _selectedHeadIndex;
        private static int _selectedVoiceIndex;
        private static GClass786<EquipmentSlot, PlayerBody.GClass2076> slotViews;

        private static readonly Dictionary<MongoID, GClass3317> HeadTemplates = [];
        
        private static List<KeyValuePair<string, GClass3321>> _voiceTemplates;
        private static GameObject _overallScreen;

        private static GClass1930 Customization => PatchConstants.BackEndSession.Profile.Customization;
        
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

                var overallScreenGameObject = overallScreen.gameObject;
                _overallScreen = overallScreenGameObject;
                var leftSide = overallScreenGameObject.transform.Find("LeftSide");
                var clothingPanel = leftSide.transform.Find("ClothingPanel");

                if (clothingPanel == null || leftSide == null)
                {
                    Console.WriteLine("customizationDrawersPrefab or overallParent not found");
                    return;
                }

                var clonedCustomizationDrawers = Object.Instantiate(clothingPanel.gameObject, leftSide);
                clonedCustomizationDrawers.gameObject.name = "NewHeadVoiceCustomizationDrawers";

                var newPosition = clonedCustomizationDrawers.transform.localPosition;
                newPosition.y -= 50f;
                clonedCustomizationDrawers.transform.localPosition = newPosition;

                customizationDrawersCloned = true;

                if (clonedCustomizationDrawers == null)
                {
                    Console.WriteLine("clonedCustomizationDrawers is null");
                    return;
                }

                var headTransform = clonedCustomizationDrawers.transform.Find("Upper");
                var voiceTransform = clonedCustomizationDrawers.transform.Find("Lower");

                if (headTransform == null || voiceTransform == null)
                {
                    Console.WriteLine("headTransform or voiceTransform are null");
                    return;
                }

                headTransform.gameObject.name = "Head";
                voiceTransform.gameObject.name = "Voice";

                var headIconTransform = clonedCustomizationDrawers.transform.Find("Head/Icon");
                var voiceIconTransform = clonedCustomizationDrawers.transform.Find("Voice/Icon");

                if (headIconTransform != null && voiceIconTransform != null)
                {
                    var headIcon = headIconTransform.GetComponent<Image>();
                    var voiceIcon = voiceIconTransform.GetComponent<Image>();

                    var headIconPng = Path.Combine(HeadVoiceSelector.pluginPath, "WTT-HeadVoiceSelector", "Icons", "icon_face_selector.png");
                    var voiceIconPng = Path.Combine(HeadVoiceSelector.pluginPath, "WTT-HeadVoiceSelector", "Icons", "icon_voice_selector.png");

                    var headIconByte = File.ReadAllBytes(headIconPng);
                    var voiceIconByte = File.ReadAllBytes(voiceIconPng);

                    var headIconTexture = new Texture2D(2, 2);
                    var voiceIconTexture = new Texture2D(2, 2);

                    headIconTexture.LoadImage(headIconByte);
                    voiceIconTexture.LoadImage(voiceIconByte);

                    var headIconSprite = Sprite.Create(headIconTexture, new Rect(0, 0, headIconTexture.width, headIconTexture.height), Vector2.zero);
                    var voiceIconSprite = Sprite.Create(voiceIconTexture, new Rect(0, 0, voiceIconTexture.width, voiceIconTexture.height), Vector2.zero);

                    headIcon.sprite = headIconSprite;
                    voiceIcon.sprite = voiceIconSprite;
                }

                var headSelectorTransform = clonedCustomizationDrawers.transform.Find("Head/ClothingSelector");
                var voiceSelectorTransform = clonedCustomizationDrawers.transform.Find("Voice/ClothingSelector");

                if (headSelectorTransform == null || voiceSelectorTransform == null)
                {
                    Console.WriteLine("headSelectorTransform or voiceSelectorTransform is null");
                    return;
                }

                headSelectorTransform.gameObject.name = "HeadSelector";
                voiceSelectorTransform.gameObject.name = "VoiceSelector";

                var headDropDownBox = headSelectorTransform.GetComponent<DropDownBox>();
                var voiceDropDownBox = voiceSelectorTransform.GetComponent<DropDownBox>();

                if (headDropDownBox is null || voiceDropDownBox is null)
                {
                    Console.WriteLine("headDropdownBox or voiceDropdownBox is null");
                    return;
                }

                await GetAvailableCustomizations(Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession());
                InitCustomizationDropdowns(_availableCustomizations, headDropDownBox, voiceDropDownBox);
                SetupCustomizationDrawers(headDropDownBox, voiceDropDownBox);

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

                compositeDisposableClass.SubscribeEvent(_headSelector.OnValueChanged, SelectHeadEvent);

                compositeDisposableClass.SubscribeEvent(_voiceSelector.OnValueChanged, SelectVoiceEvent);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during initialization: {ex.Message}");
            }
        }

        private static async Task GetAvailableCustomizations(ISession session)
        {
            try
            {
                var result = await session.GetAvailableAccountCustomization();
                _availableCustomizations = result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void SetupCustomizationDrawers(DropDownBox _headSelector, DropDownBox _voiceSelector)
        {
            try
            {
                var instance = Singleton<GClass1597>.Instance;

                if (instance is null)
                {
                    Console.WriteLine("GClass1597 instance is null.");
                    return;
                }
                
                _voiceTemplates = [];

                if (_availableCustomizations == null)
                {
                    Console.WriteLine("_availableCustomizations is null.");
                    return;
                }

                foreach (var mongoID in _availableCustomizations)
                {
                    var anyCustomizationItem = instance.GetAnyCustomizationItem(mongoID);
                    
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
                            HeadTemplates.Add(mongoID, gclass);
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
                Console.WriteLine($"Added {HeadTemplates.Count} head customization templates.");
                Console.WriteLine($"Added {_voiceTemplates.Count} voice customization templates.");
        #endif

                _voices.Clear();

                if (_headSelector is null)
                {
                    Console.WriteLine("Head dropdown is null.");
                    return;
                }
                SetupHeadDropdownInfo(_headSelector);

                if (_voiceSelector is null)
                {
                    Console.WriteLine("Voice dropdown is null.");
                    return;
                }
                SetupVoiceDropdownInfo(_voiceSelector);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during customization drawers setup: {ex.Message}");
            }
        }

        private static void SetupHeadDropdownInfo(DropDownBox headSelector)
        {
            try
            {
                var id = Customization[EBodyModelPart.Head];

                _selectedHeadIndex = HeadTemplates.GetIndexOfKey(id);
                
                headSelector.Show(HeadTemplates
                    .Select(t => t.Key.LocalizedShortName())
                    .Where(s => s.Length > 0));

                headSelector.UpdateValue(_selectedHeadIndex, false);

               Customization[EBodyModelPart.Head] = HeadTemplates.GetKvpFromIndex(_selectedHeadIndex).Key;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during head dropdown info setup: {ex.Message}");
            }
        }

        private static void SetupVoiceDropdownInfo(DropDownBox _voiceSelector)
        {
            try
            {
                var currentVoice = PatchConstants.BackEndSession.Profile.Info.Voice;
                var selectedIndex = _voiceTemplates.FindIndex(v => v.Value.Name == currentVoice);

                if (selectedIndex == -1)
                {
                    Console.WriteLine($"Current voice '{currentVoice}' not found in the voice templates.");
                    return;
                }

                _voiceSelector.Show(InitializeVoiceDropdown);

                _voiceSelector.UpdateValue(selectedIndex, false);

                PatchConstants.BackEndSession.Profile.Info.Voice = _voiceTemplates[selectedIndex].Value.Name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during voice dropdown info setup: {ex.Message}");
            }
        }
        
        private static IEnumerable<string> InitializeVoiceDropdown()
        {
            return _voiceTemplates.Select(x => x.Value.NameLocalizationKey.Localized()).ToArray();
        }
        
        private static void SelectHeadEvent(int selectedIndex)
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
                var key = HeadTemplates.GetKvpFromIndex(_selectedHeadIndex).Key;
                Customization[EBodyModelPart.Head] = key;
#if DEBUG
                Console.WriteLine($"Head customization updated to: {key}");
#endif
                ShowPlayerPreview().HandleExceptions();




                WTTChangeHead(key);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during select head event: {ex.Message}");
            }
        }

        private static async Task ShowPlayerPreview()
        {
            try
            {
                var leftSide = _overallScreen.transform.Find("LeftSide");
                if (leftSide is null)
                {
                    Console.WriteLine("Overall screen parent not found.");
                    return;
                }

                var characterPanel = leftSide.transform.Find("CharacterPanel");
                var playerModelViewScript = characterPanel.GetComponentInChildren<PlayerModelView>();

                var inventoryPlayerModelWithStatsWindow = leftSide.GetComponent<InventoryPlayerModelWithStatsWindow>();
                if (inventoryPlayerModelWithStatsWindow is null)
                {
                    Console.WriteLine("InventoryPlayerModelWithStatsWindow component not found.");
                    return;
                }

                await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, inventoryPlayerModelWithStatsWindow.method_5);

                ChangeSelectedHead(false, playerModelViewScript);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during player preview: {ex.Message}");
            }
        }

        private static void ChangeSelectedHead(bool active, PlayerModelView playerModelView)
        {
            try
            {

                slotViews = playerModelView.PlayerBody.SlotViews;
                foreach (var gameObject in _hiddenSlots.Where(GetSlotType).Select(GetSlotKey).Where(GetModel))
                {
                    gameObject.SetActive(active);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during change selected head: {ex.Message}");
            }
        }

        private static bool GetSlotType(EquipmentSlot slotType)
        {
            return slotViews.ContainsKey(slotType);
        }

        private static GameObject GetSlotKey(EquipmentSlot slotType)
        {
            return slotViews.GetByKey(slotType).ParentedModel.Value;
        }

        private static bool GetModel(GameObject model)
        {
            return model is not null;
        }

        private static void SelectVoiceEvent(int selectedIndex)
        {
            try
            {
                _selectedVoiceIndex = selectedIndex;
                SelectVoice(selectedIndex).HandleExceptions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during select voice event: {ex.Message}");
            }
        }

        private static async Task SelectVoice(int selectedIndex)
        {
            try
            {
                if (!_voices.ContainsKey(selectedIndex))
                {
                    var result = await Singleton<GClass868>.Instance.TakeVoice(_voiceTemplates[_selectedVoiceIndex].Value.Name);
                    _voices.Add(selectedIndex, result);
                    
                    if (result is null)
                    {
                        Console.WriteLine($"Voice not available for index: {selectedIndex}");
                        return;
                    }
                }
                
                var key = _voiceTemplates[_selectedVoiceIndex].Value.Name;

                PatchConstants.BackEndSession.Profile.Info.Voice = key;


                var num = Random.Range(0, _voices[selectedIndex].Clips.Length);
                var taggedClip = _voices[selectedIndex].Clips[num];
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
            if (id is null)
            {
                Console.WriteLine("Error: id is null.");
                return;
            }

            var response = WebRequestUtils.Post<string>("/WTT/WTTChangeHead", id);
            if (response is not null)
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
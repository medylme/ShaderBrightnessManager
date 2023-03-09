using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;

namespace dylTools.ShaderBrightnessManager
{
    public class ShaderBrightnessManager : EditorWindow
    {
        private string CURRENT_VERSION = "0.1.1";

        private VRCAvatarDescriptor _avatar;
        private VisualElement _topArea;
        private ScrollView _selectedTabArea;

        [MenuItem("dylTools/Shader Brightness Manager")]
        public static void ShowWindow()
        {
            GetWindow<ShaderBrightnessManager>("Shader Brightness Manager");
        }

        private void OnEnable()
        {
            VisualElement root = rootVisualElement;
            root.style.minWidth = 320;
            var styleSheet = Resources.Load<StyleSheet>("ShaderBrightnessManagerStyle");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("Failed to load style sheet: ShaderBrightnessManagerStyle");
            }

            VisualElement topArea = new VisualElement().WithClass("top-area").ChildOf(root);
            VisualElement mainBody = new VisualElement()
                .WithFlexDirection(FlexDirection.RowReverse)
                .WithFlexGrow(1)
                .ChildOf(root);
            VisualElement tabsArea = new VisualElement().WithClass("tabs-area").ChildOf(mainBody);
            _selectedTabArea = new ScrollView().WithClass("selected-tab").ChildOf(mainBody);

            LoadTopArea(topArea);

            UpdateTabs();
        }

        private void LoadTopArea(VisualElement topArea)
        {
            ObjectField avatar = FluentUIElements
                .NewObjectField("Avatar", typeof(VRCAvatarDescriptor), _avatar)
                .WithClass("avatar-field")
                .WithFlexGrow(1)
                .ChildOf(topArea);

            Button refreshButton = new Button(UpdateTabs)
                .WithClass("refresh-button")
                .ChildOf(topArea);
            refreshButton.text = "Refresh";

            avatar.RegisterValueChangedCallback(e =>
            {
                _avatar = (VRCAvatarDescriptor)e.newValue;

                UpdateTabs();
            });
        }

        private void UpdateTabs()
        {
            _selectedTabArea.Clear();

            VisualElement materialsList = new VisualElement()
                .WithClass("materials-list")
                .ChildOf(_selectedTabArea);

            Foldout aboutHeader = new Foldout().WithClass("about-header").ChildOf(materialsList);
            aboutHeader.value = false;
            aboutHeader.text = "About";
            aboutHeader.style.fontSize = 13;
            aboutHeader.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.33f);
            aboutHeader.style.paddingTop = 4;
            aboutHeader.style.paddingBottom = 4;
            aboutHeader.style.marginLeft = 10;
            aboutHeader.style.marginRight = 10;
            aboutHeader.style.marginBottom = 4;
            aboutHeader.style.marginTop = 4;
            aboutHeader.style.borderTopLeftRadius = 10;
            aboutHeader.style.borderTopRightRadius = 10;
            aboutHeader.style.borderBottomRightRadius = 10;
            aboutHeader.style.borderBottomLeftRadius = 10;

            renderAboutSection(aboutHeader);

            if (_avatar == null)
            {
                AddSelectAvatarLabel();
                return;
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers =
                _avatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderers.Length == 0)
            {
                AddNoMaterialsLabel(materialsList);
                return;
            }

            List<Material> poiyomi81Shaders = new List<Material>();
            List<Material> lilToonShaders = new List<Material>();
            List<Material> unsupportedShaders = new List<Material>();

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                Material[] materials = skinnedMeshRenderer.sharedMaterials;
                if (materials.Length == 0)
                {
                    continue;
                }
                foreach (Material material in materials)
                {
                    Shader shader = material.shader;
                    string shaderName = shader ? shader.name : "None";

                    if (shaderName.Contains("Poiyomi 8.1") && !shaderName.Contains("Locked"))
                    {
                        poiyomi81Shaders.Add(material);
                    }
                    else if (shaderName.Contains("lilToon"))
                    {
                        lilToonShaders.Add(material);
                    }
                    else
                    {
                        unsupportedShaders.Add(material);
                    }
                }
            }

            if (unsupportedShaders.Count > 0)
            {
                Foldout unsupportedShadersHeader = new Foldout()
                    .WithClass("unsupported-shaders-header")
                    .ChildOf(materialsList);
                unsupportedShadersHeader.value = false;
                unsupportedShadersHeader.text = "Unsupported/Locked Materials";
                unsupportedShadersHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                unsupportedShadersHeader.style.fontSize = 13;
                unsupportedShadersHeader.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.33f);
                unsupportedShadersHeader.style.paddingTop = 4;
                unsupportedShadersHeader.style.paddingBottom = 4;
                unsupportedShadersHeader.style.marginLeft = 10;
                unsupportedShadersHeader.style.marginRight = 10;
                unsupportedShadersHeader.style.marginBottom = 4;
                unsupportedShadersHeader.style.marginTop = 4;
                unsupportedShadersHeader.style.borderTopLeftRadius = 10;
                unsupportedShadersHeader.style.borderTopRightRadius = 10;
                unsupportedShadersHeader.style.borderBottomRightRadius = 10;
                unsupportedShadersHeader.style.borderBottomLeftRadius = 10;

                unsupportedShaders = unsupportedShaders.Distinct().ToList();
                AddShadersToUI(
                    unsupportedShadersHeader,
                    unsupportedShaders,
                    "Unsupported/Locked Shaders",
                    "",
                    ""
                );
            }

            Foldout shaderListHeader = new Foldout()
                .WithClass("shader-list-header")
                .ChildOf(materialsList);
            shaderListHeader.value = true;
            shaderListHeader.text = "Controls";
            shaderListHeader.style.fontSize = 13;
            shaderListHeader.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.33f);
            shaderListHeader.style.paddingTop = 4;
            shaderListHeader.style.paddingBottom = 4;
            shaderListHeader.style.marginLeft = 10;
            shaderListHeader.style.marginRight = 10;
            shaderListHeader.style.marginBottom = 4;
            shaderListHeader.style.marginTop = 4;
            shaderListHeader.style.borderTopLeftRadius = 10;
            shaderListHeader.style.borderTopRightRadius = 10;
            shaderListHeader.style.borderBottomRightRadius = 10;
            shaderListHeader.style.borderBottomLeftRadius = 10;

            if (poiyomi81Shaders.Count > 0)
            {
                poiyomi81Shaders = poiyomi81Shaders.Distinct().ToList();
                AddShadersToUI(
                    shaderListHeader,
                    poiyomi81Shaders,
                    "Poiyomi 8.1 Toon",
                    "_LightingMinLightBrightness",
                    "_LightingCap"
                );
            }

            if (lilToonShaders.Count > 0)
            {
                lilToonShaders = lilToonShaders.Distinct().ToList();
                AddShadersToUI(
                    shaderListHeader,
                    lilToonShaders,
                    "lilToon",
                    "_LightMinLimit",
                    "_LightMaxLimit"
                );
            }
        }

        private void AddSelectAvatarLabel()
        {
            Label selectAvatarLabel = new Label("Select an avatar to begin.").WithClass(
                "select-avatar-label"
            );
            selectAvatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _selectedTabArea.Add(selectAvatarLabel);
        }

        private void AddNoMaterialsLabel(VisualElement parentElement)
        {
            Label noMaterialsLabel = new Label("No materials found").WithClass(
                "no-materials-label"
            );
            parentElement.Add(noMaterialsLabel);
        }

        private void AddShadersToUI(
            VisualElement parentElement,
            List<Material> materials,
            string headerText,
            string minBrightnessPropertyName,
            string maxBrightnessPropertyName
        )
        {
            VisualElement shaderList = new VisualElement()
                .WithClass("shader-list")
                .ChildOf(parentElement);
            shaderList.style.marginBottom = 16;

            if (headerText != "Unsupported/Locked Shaders")
            {
                Label shaderListHeader = new Label(headerText)
                    .WithClass("shader-list-header")
                    .ChildOf(shaderList);
                shaderListHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                shaderListHeader.style.unityTextAlign = TextAnchor.MiddleCenter;
                shaderListHeader.style.fontSize = 14;
                shaderListHeader.style.marginTop = 4;
            }

            foreach (Material material in materials)
            {
                VisualElement shaderElement = new VisualElement()
                    .WithClass("shader-element")
                    .ChildOf(shaderList);

                Label materialName = new Label($"- {material.name}")
                    .WithClass("shader-name")
                    .ChildOf(shaderElement);
                materialName.style.fontSize = 13;
                materialName.style.marginTop = 12;
                materialName.style.marginBottom = 4;

                if (headerText != "Unsupported/Locked Shaders")
                {
                    float minBrightness = 0f;
                    float maxBrightness = 0f;

                    try
                    {
                        minBrightness = material.GetFloat(minBrightnessPropertyName);
                        maxBrightness = material.GetFloat(maxBrightnessPropertyName);
                    }
                    catch (UnityException)
                    {
                        Label errorLabel = new Label("Error!")
                            .WithClass("slider-value")
                            .ChildOf(shaderElement);
                        continue;
                    }

                    VisualElement minBrightnessSliderContainer = new VisualElement()
                        .WithClass("slider-container")
                        .ChildOf(shaderElement);
                    minBrightnessSliderContainer.style.display = DisplayStyle.Flex;
                    minBrightnessSliderContainer.style.flexDirection = FlexDirection.Row;
                    minBrightnessSliderContainer.style.justifyContent = Justify.SpaceBetween;

                    VisualElement maxBrightnessSliderContainer = new VisualElement()
                        .WithClass("slider-container")
                        .ChildOf(shaderElement);
                    maxBrightnessSliderContainer.style.display = DisplayStyle.Flex;
                    maxBrightnessSliderContainer.style.flexDirection = FlexDirection.Row;
                    maxBrightnessSliderContainer.style.justifyContent = Justify.SpaceBetween;

                    Slider minBrightnessSlider = new Slider($"Min Brightness", 0f, 1f, 0, 1f)
                        .WithClass("shader-brightness-slider")
                        .ChildOf(minBrightnessSliderContainer);
                    minBrightnessSlider.style.fontSize = 12;
                    minBrightnessSlider.style.flexGrow = 1;
                    minBrightnessSlider.value = minBrightness;

                    Label minBrightnessValue = new Label(minBrightness.ToString("0.00"))
                        .WithClass("slider-value")
                        .ChildOf(minBrightnessSliderContainer);
                    minBrightnessValue.style.marginLeft = 4;
                    minBrightnessValue.style.marginRight = 16;

                    Slider maxBrightnessSlider = new Slider($"Max Brightness", 0f, 1f, 0, 1f)
                        .WithClass("shader-brightness-slider")
                        .ChildOf(maxBrightnessSliderContainer);
                    maxBrightnessSlider.style.fontSize = 12;
                    maxBrightnessSlider.style.flexGrow = 1;
                    maxBrightnessSlider.value = maxBrightness;

                    Label maxBrightnessValue = new Label(maxBrightness.ToString("0.00"))
                        .WithClass("slider-value")
                        .ChildOf(maxBrightnessSliderContainer);
                    maxBrightnessValue.style.marginLeft = 4;
                    maxBrightnessValue.style.marginRight = 16;

                    minBrightnessSlider.RegisterValueChangedCallback(evt =>
                    {
                        float newMinBrightness = evt.newValue;
                        minBrightnessValue.text = evt.newValue.ToString("0.00");
                        material.SetFloat(
                            minBrightnessPropertyName,
                            (float)Math.Round(evt.newValue * 100f) / 100f
                        );
                    });

                    maxBrightnessSlider.RegisterValueChangedCallback(evt =>
                    {
                        float newMaxBrightness = evt.newValue;
                        maxBrightnessValue.text = evt.newValue.ToString("0.00");
                        material.SetFloat(
                            maxBrightnessPropertyName,
                            (float)Math.Round(evt.newValue * 100f) / 100f
                        );
                    });
                }
            }
        }

        private void renderAboutSection(VisualElement parentElement)
        {
            // Create a list with the currently supported shaders
            Label supportedShadersHeaderLabel = new Label("Supported Shaders").WithClass(
                "supported-shaders-header-label"
            );
            supportedShadersHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            supportedShadersHeaderLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            supportedShadersHeaderLabel.style.fontSize = 14;
            parentElement.Add(supportedShadersHeaderLabel);

            Label supportedShadersLabel = new Label("- lilToon\n- Poiyomi Toon 8.1").WithClass(
                "supported-shaders-label"
            );

            supportedShadersLabel.style.fontSize = 12;
            parentElement.Add(supportedShadersLabel);

            Label versionLabel = new Label($"v{CURRENT_VERSION} | Made by dyl#1234").WithClass(
                "version-label"
            );
            versionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            versionLabel.style.fontSize = 12;
            versionLabel.style.marginTop = 12;
            versionLabel.style.marginBottom = 8;
            parentElement.Add(versionLabel);
        }
    }
}

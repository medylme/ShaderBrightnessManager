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

namespace medyl.ShaderBrightnessManager
{
    public class ShaderBrightnessManager : EditorWindow
    {
        private VRCAvatarDescriptor _avatar;
        private VisualElement _topArea;
        private ScrollView _mainContainer;

        [MenuItem("medyl/Shader Brightness Manager")]
        public static void ShowWindow()
        {
            GetWindow<ShaderBrightnessManager>("Shader Brightness Manager");
        }

        private void OnEnable()
        {
            VisualElement root = rootVisualElement;

            // Load stylesheet
            var styleSheet = Resources.Load<StyleSheet>("ShaderBrightnessManagerStyle");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("Failed to load style sheet: ShaderBrightnessManagerStyle");
            }

            // Top Area
            VisualElement topArea = new VisualElement().WithClass("top-area").ChildOf(root);
            LoadTopArea(topArea);

            // Main Area
            VisualElement mainBody = new VisualElement()
                .WithClass("main-body")
                .WithFlexGrow(1)
                .ChildOf(root);

            _mainContainer = new ScrollView().WithClass("main-container").ChildOf(mainBody);
            UpdateMainArea();
        }

        private void LoadTopArea(VisualElement topArea)
        {
            // Avatar Field
            ObjectField avatar = FluentUIElements
                .NewObjectField("Avatar", typeof(VRCAvatarDescriptor), _avatar)
                .WithClass("avatar-field")
                .WithFlexGrow(1)
                .ChildOf(topArea);

            // Refresh Button
            Button refreshButton = new Button(UpdateMainArea)
                .WithClass("refresh-button")
                .ChildOf(topArea);
            refreshButton.text = "Refresh";

            avatar.RegisterValueChangedCallback(e =>
            {
                _avatar = (VRCAvatarDescriptor)e.newValue;

                UpdateMainArea();
            });
        }

        private void UpdateMainArea()
        {
            _mainContainer.Clear();

            VisualElement sectionList = new VisualElement().ChildOf(_mainContainer);

            // - About Section -
            Foldout aboutSection = new Foldout().WithClass("section").ChildOf(sectionList);
            aboutSection.value = true;
            aboutSection.text = "About";
            renderAboutSection(aboutSection);

            if (_avatar == null)
            {
                Label selectAvatarLabel = new Label("Select an avatar to begin.").WithClass(
                    "notice-label"
                );
                selectAvatarLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _mainContainer.Add(selectAvatarLabel);
                return;
            }

            // - Material Section -
            SkinnedMeshRenderer[] skinnedMeshRenderers =
                _avatar.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (skinnedMeshRenderers.Length == 0)
            {
                Label noMaterialsLabel = new Label("No materials found").WithClass("notice-label");
                noMaterialsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                sectionList.Add(noMaterialsLabel);
                return;
            }

            Foldout materialSection = new Foldout().WithClass("section").ChildOf(sectionList);
            materialSection.value = true;
            materialSection.text = "Materials";
            renderMaterialSection(skinnedMeshRenderers, materialSection);
        }

        private void renderMaterialSection(
            SkinnedMeshRenderer[] skinnedMeshRenderers,
            VisualElement parentElement
        )
        {
            // Get all materials on the avatar and sort them into lists
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
                    if 
                    (
                        material == null ||
                        material.shader == null ||
                        material.name == null
                    )
                    {
                        continue;
                    }

                    try
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
                    catch
                    {
                        Debug.LogError("Failed to load material: " + material.name);
                        continue;
                    }
                }
            }

            // Poiyomi 8.1 Toon
            if (poiyomi81Shaders.Count > 0)
            {
                poiyomi81Shaders = poiyomi81Shaders.Distinct().ToList();
                AddShaderToUI(
                    "Poiyomi 8.1 Toon",
                    parentElement,
                    poiyomi81Shaders,
                    "_LightingMinLightBrightness",
                    "_LightingCap"
                );
            }

            // lilToon
            if (lilToonShaders.Count > 0)
            {
                lilToonShaders = lilToonShaders.Distinct().ToList();
                AddShaderToUI(
                    "lilToon",
                    parentElement,
                    lilToonShaders,
                    "_LightMinLimit",
                    "_LightMaxLimit"
                );
            }

            // Unsupported Section
            if (unsupportedShaders.Count > 0)
            {
                Foldout unsupportedShadersSection = new Foldout()
                    .WithClass("subsection")
                    .ChildOf(parentElement);
                unsupportedShadersSection.value = false;
                unsupportedShadersSection.text = "Unsupported/Locked Materials";

                // Remove duplicates
                unsupportedShaders = unsupportedShaders.Distinct().ToList();

                foreach (Material material in unsupportedShaders)
                {
                    Shader shader = material.shader;
                    string shaderName = shader ? shader.name : "None";

                    Label unsupportedMaterialLabel = new Label(
                        $"- {material.name}\n({shaderName})"
                    ).WithClass("unsupported-material-label");
                    unsupportedShadersSection.Add(unsupportedMaterialLabel);
                }
            }
        }

        private void AddShaderToUI(
            string shaderName,
            VisualElement parentElement,
            List<Material> materials,
            string minBrightnessPropertyName,
            string maxBrightnessPropertyName
        )
        {
            VisualElement shaderList = new VisualElement()
                .WithClass("shader-list")
                .ChildOf(parentElement);
            shaderList.style.marginBottom = 16;

            foreach (Material material in materials)
            {
                VisualElement shaderElement = new VisualElement()
                    .WithClass("shader-element")
                    .ChildOf(shaderList);

                Label materialName = new Label($"- {material.name} ({shaderName})")
                    .WithClass("shader-name")
                    .ChildOf(shaderElement);
                materialName.style.fontSize = 13;
                materialName.style.marginTop = 12;
                materialName.style.marginBottom = 4;

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
                    .WithClass("shaderslider-container")
                    .ChildOf(shaderElement);

                Slider minBrightnessSlider = new Slider($"Min Brightness", 0f, 1f, 0, 1f)
                    .WithClass("shaderslider-brightness")
                    .ChildOf(minBrightnessSliderContainer);
                minBrightnessSlider.value = minBrightness;

                Label minBrightnessValue = new Label(minBrightness.ToString("0.00"))
                    .WithClass("shaderslider-value")
                    .ChildOf(minBrightnessSliderContainer);

                minBrightnessSlider.RegisterValueChangedCallback(evt =>
                {
                    float newMinBrightness = evt.newValue;
                    minBrightnessValue.text = evt.newValue.ToString("0.00");
                    material.SetFloat(
                        minBrightnessPropertyName,
                        (float)Math.Round(evt.newValue * 100f) / 100f
                    );
                });

                VisualElement maxBrightnessSliderContainer = new VisualElement()
                    .WithClass("shaderslider-container")
                    .ChildOf(shaderElement);

                Slider maxBrightnessSlider = new Slider($"Max Brightness", 0f, 1f, 0, 1f)
                    .WithClass("shaderslider-brightness")
                    .ChildOf(maxBrightnessSliderContainer);
                maxBrightnessSlider.value = maxBrightness;

                Label maxBrightnessValue = new Label(maxBrightness.ToString("0.00"))
                    .WithClass("shaderslider-value")
                    .ChildOf(maxBrightnessSliderContainer);

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

        private void renderAboutSection(VisualElement parentElement)
        {
            Label supportedShadersHeaderLabel = new Label("Supported Shaders").WithClass("header");
            parentElement.Add(supportedShadersHeaderLabel);

            Label supportedShadersLabel = new Label("- lilToon\n- Poiyomi Toon 8.1").WithClass(
                "supported-shaders-label"
            );
            parentElement.Add(supportedShadersLabel);

            Label versionLabel = new Label("version Dev").WithClass("header");
            versionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            versionLabel.style.marginTop = 12;
            versionLabel.style.marginBottom = 8;
            parentElement.Add(versionLabel);
        }
    }
}

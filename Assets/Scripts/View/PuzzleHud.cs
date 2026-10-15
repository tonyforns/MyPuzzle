using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MyPuzzle.View
{
    public sealed class PuzzleHud : MonoBehaviour
    {
        [SerializeField] PuzzleSession session;
        [SerializeField] PuzzleSettings easy;
        [SerializeField] PuzzleSettings medium;
        [SerializeField] PuzzleSettings hard;
        [SerializeField] PuzzleSettings expert;

        RawImage preview;
        Text status;
        Text referenceLabel;

        void Awake()
        {
            if (session == null)
                session = FindAnyObjectByType<PuzzleSession>();

            Build();
            if (session != null)
            {
                session.RoundBegan += Refresh;
                session.RoundCompleted += ShowComplete;
                session.ReferenceChanged += PaintReference;
            }
        }

        void OnDestroy()
        {
            if (session == null)
                return;

            session.RoundBegan -= Refresh;
            session.RoundCompleted -= ShowComplete;
            session.ReferenceChanged -= PaintReference;
        }

        void Build()
        {
            var canvasObject = new GameObject("HudCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var eventObject = new GameObject("EventSystem");
                eventObject.AddComponent<EventSystem>();
                eventObject.AddComponent<InputSystemUIInputModule>();
            }

            var bar = new GameObject("Bar");
            bar.transform.SetParent(canvasObject.transform, false);
            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(0f, 96f);
            barRect.anchoredPosition = Vector2.zero;
            var barImage = bar.AddComponent<Image>();
            barImage.sprite = White();
            barImage.color = new Color(0.12f, 0.1f, 0.09f, 0.88f);

            var row = new GameObject("Row");
            row.transform.SetParent(bar.transform, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = new Vector2(16f, 12f);
            rowRect.offsetMax = new Vector2(-16f, -12f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            preview = CreatePreview(row.transform);
            CreateButton(row.transform, "Fácil", () => Play(easy));
            CreateButton(row.transform, "Medio", () => Play(medium));
            CreateButton(row.transform, "Difícil", () => Play(hard));
            CreateButton(row.transform, "Experto", () => Play(expert));
            CreateButton(row.transform, "Mezclar", Shuffle);
            CreateButton(row.transform, "Girar", () => session?.RotateHeld());
            CreateButton(row.transform, "Separar", () => session?.DetachHeld());
            referenceLabel = CreateButton(row.transform, "Referencia", ToggleReference).GetComponentInChildren<Text>();
            CreateButton(row.transform, "Guardar", Save);
            CreateButton(row.transform, "Cargar", Load);
            CreateButton(row.transform, "Otra imagen", ChooseImage);
            status = CreateStatus(row.transform);
        }

        void Play(PuzzleSettings preset)
        {
            if (session == null || preset == null || session.ActiveImage == null)
                return;

            session.Begin(preset, session.ActiveImage, session.ActiveSeed, ownImage: false);
        }

        void Shuffle()
        {
            if (session != null)
                session.Shuffle();
        }

        void ChooseImage()
        {
            if (session == null || session.ActiveSettings == null)
                return;
            if (!ImageFileDialog.TryPick(out var path))
                return;

            var imported = ImageImportService.Open(path);
            if (!imported.Succeeded)
            {
                status.text = imported.Message;
                return;
            }

            session.Begin(session.ActiveSettings, imported.Texture, session.ActiveSeed, ownImage: true, imagePath: path);
        }

        void Save()
        {
            if (session == null)
                return;

            string text;
            try
            {
                text = session.Export();
            }
            catch (Exception)
            {
                text = null;
            }

            if (string.IsNullOrEmpty(text))
            {
                if (status != null)
                    status.text = "No hay partida.";
                return;
            }

            if (!PuzzleFileDialog.TrySave(out var path))
                return;

            try
            {
                File.WriteAllText(path, text);
                if (status != null)
                    status.text = "Guardado";
            }
            catch (Exception)
            {
                if (status != null)
                    status.text = "No pude guardar.";
            }
        }

        void Load()
        {
            if (session == null)
                return;
            if (!PuzzleFileDialog.TryOpen(out var path))
                return;

            MyPuzzle.Core.SavedSession saved;
            try
            {
                saved = MyPuzzle.Core.SessionArchive.Read(File.ReadAllText(path));
            }
            catch (Exception)
            {
                if (status != null)
                    status.text = "No pude leer la partida.";
                return;
            }

            var preset = FindPreset(saved.Config.PresetName);
            if (preset == null)
            {
                if (status != null)
                    status.text = "No encuentro el preset.";
                return;
            }

            if (string.IsNullOrEmpty(saved.ImagePath))
            {
                if (session.FallbackImage == null)
                {
                    if (status != null)
                        status.text = "No hay imagen de prueba.";
                    return;
                }

                session.Restore(saved, preset, session.FallbackImage, ownImage: false);
                return;
            }

            var imported = ImageImportService.Open(saved.ImagePath);
            if (!imported.Succeeded)
            {
                if (status != null)
                    status.text = imported.Message;
                return;
            }

            session.Restore(saved, preset, imported.Texture, ownImage: true);
        }

        PuzzleSettings FindPreset(string presetName)
        {
            if (Matches(easy, presetName))
                return easy;
            if (Matches(medium, presetName))
                return medium;
            if (Matches(hard, presetName))
                return hard;
            if (Matches(expert, presetName))
                return expert;

            return null;
        }

        static bool Matches(PuzzleSettings preset, string presetName)
        {
            return preset != null && preset.ToConfig().PresetName == presetName;
        }

        void Refresh()
        {
            if (session == null)
                return;

            if (preview != null)
                preview.texture = session.ActiveImage;

            PaintReference();

            if (status == null || session.ActiveSettings == null)
                return;

            status.text = session.ActiveSettings.ToConfig().PresetName;
        }

        void ToggleReference()
        {
            if (session != null)
                session.ToggleReference();
        }

        void PaintReference()
        {
            if (referenceLabel == null || session == null)
                return;

            referenceLabel.text = session.ReferenceVisible ? "Referencia" : "Sin guía";
        }

        void ShowComplete()
        {
            if (status != null)
                status.text = "Completo";
        }

        static RawImage CreatePreview(Transform parent)
        {
            var previewObject = new GameObject("Preview");
            previewObject.transform.SetParent(parent, false);
            var rect = previewObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(72f, 72f);
            var image = previewObject.AddComponent<RawImage>();
            image.color = Color.white;
            return image;
        }

        static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction click)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(126f, 72f);
            var image = buttonObject.AddComponent<Image>();
            image.sprite = White();
            image.color = new Color(0.24f, 0.2f, 0.17f, 1f);
            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.24f, 0.2f, 0.17f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.32f, 0.24f, 1f);
            colors.pressedColor = new Color(0.52f, 0.4f, 0.28f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(click);

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<Text>();
            text.font = BuiltinFont();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.93f, 0.88f, 1f);
            text.fontSize = 20;
            return button;
        }

        static Text CreateStatus(Transform parent)
        {
            var statusObject = new GameObject("Status");
            statusObject.transform.SetParent(parent, false);
            var rect = statusObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 72f);
            var text = statusObject.AddComponent<Text>();
            text.font = BuiltinFont();
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.96f, 0.93f, 0.88f, 1f);
            text.fontSize = 28;
            text.text = string.Empty;
            return text;
        }

        static Font BuiltinFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        static Sprite White()
        {
            if (white != null)
                return white;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            white = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return white;
        }

        static Sprite white;
    }
}

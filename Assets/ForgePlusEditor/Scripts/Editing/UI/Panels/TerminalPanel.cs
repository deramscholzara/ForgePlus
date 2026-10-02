using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;
using static AlephOne.computer_interface;
using static AlephOne.csmacros;
using static AlephOne.screen_drawing;
using Rect = AlephOne.Rect;

namespace ForgePlus.UI
{
    // The page being read, drawn where Aleph One draws it (in terminal coordinates, scaled to the panel's width),
    // though only its content, without the terminal's borders. A checkpoint's map is the level itself, seen
    // through the page and framed on the checkpoint's goal.
    [AutoStaticsCleanup]
    public partial class TerminalPanel : UIPanel
    {
        // The part of the terminal's screen between its header and footer
        private const int ScreenTop = 18;
        private const int ScreenWidth = 640;
        private const int ScreenHeight = 284;

        // Courier Prime's characters are 1228/2048 of its size wide, and it rises 1421/2048 of its size above the
        // baseline, so at this size its characters are the terminal font's 7 pixels wide
        private const float FontSize = terminal_font_character_width * 2048f / 1228f;
        private const float Ascent = FontSize * 1421f / 2048f;

        private const int StaticWidth = 160;
        private const int StaticHeight = 80;

        private static Texture2D staticTexture;

        private TerminalsViewModel terminals;
        private VisualElement screen;
        private IVisualElementScheduledItem staticAnimation;
        private float scale;
        private bool hasDrawn;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Terminal";
            }
        }

        protected override void OnLoaded()
        {
            terminals = ForgePlusUI.Instance.Terminals;
            screen = Root.Q("screen");

            var back = Root.Find<Button>("back");
            back.BindEnabled(terminals, nameof(TerminalsViewModel.CanGoBack));
            back.clicked += terminals.Back;

            var next = Root.Find<Button>("next");
            next.BindEnabled(terminals, nameof(TerminalsViewModel.CanGoNext));
            next.clicked += terminals.Next;

            Root.Q<Label>("page").Bind("text", terminals, nameof(TerminalsViewModel.PageCaption));

            screen.RegisterCallback<GeometryChangedEvent>(OnScreenGeometryChanged);
            terminals.OnPageChanged += OnPageChanged;
        }

        protected override void OnUnloading()
        {
            terminals.OnPageChanged -= OnPageChanged;
            staticAnimation?.Pause();
        }

        // The screen keeps the terminal's proportions at whatever width the panel has
        private void OnScreenGeometryChanged(GeometryChangedEvent geometryChangedEvent)
        {
            var newScale = geometryChangedEvent.newRect.width / ScreenWidth;
            if (Mathf.Approximately(newScale, scale))
            {
                return;
            }

            scale = newScale;
            screen.style.height = ScreenHeight * scale;

            // A checkpoint is framed when its page is first drawn, not whenever the panel is resized
            Draw(frameCheckpoint: !hasDrawn);
        }

        private void OnPageChanged()
        {
            Draw(frameCheckpoint: true);
        }

        private void Draw(bool frameCheckpoint)
        {
            screen.Clear();
            staticAnimation?.Pause();
            staticAnimation = null;

            var page = terminals.Page;
            if (scale <= 0f || page == null)
            {
                return;
            }

            hasDrawn = true;

            var terminal = terminals.Terminal;
            var group = page.Group;
            var fullScreen = new Rect { top = ScreenTop, left = 0, bottom = ScreenTop + ScreenHeight, right = ScreenWidth };

            // As _render_computer_interface draws each type of group
            switch (group.type)
            {
                case _logon_group:
                case _logoff_group:
                    AddBackground(fullScreen);
                    AddLogon(terminal, group);
                    break;
                case _information_group:
                    AddBackground(fullScreen);
                    AddLines(terminals.GetPageLines(), TerminalLayout.GetTextBounds(group));
                    break;
                case _pict_group:
                    AddBackground(fullScreen);
                    AddPicture(group.permutation, group.flags);
                    AddLines(terminals.GetPageLines(), TerminalLayout.GetTextBounds(group));
                    break;
                case _checkpoint_group:
                    AddCheckpoint(group, fullScreen, frameCheckpoint);
                    AddLines(terminals.GetPageLines(), TerminalLayout.GetTextBounds(group));
                    break;
                case _static_group:
                    AddBackground(fullScreen);
                    AddStatic(fullScreen);
                    break;
                default:
                    AddBackground(fullScreen);
                    AddNote(TerminalLayout.GetDirective(group), Describe(group), fullScreen);
                    break;
            }
        }

        // draw_logon_text: the logo centered, and the group's line centered below it
        private void AddLogon(terminal_text_t terminal, terminal_groupings group)
        {
            var frame = get_term_rectangle(_terminal_logon_graphic_rect);

            if ((group.flags & _group_is_marathon_1) != 0)
            {
                // Marathon 1's logon is a shape from the interface collection, not a picture
                var logoBounds = frame.Clone();
                logoBounds.bottom = (short) (frame.top + RECTANGLE_HEIGHT(frame) / 2);
                AddNote(null, Strings.Get(Strings.Terminals, "Terminals.Note.Marathon1Logon"), logoBounds);
                frame = logoBounds;
            }
            else
            {
                var picture = ScenarioPictures.Get(group.permutation);
                if (picture != null)
                {
                    frame = TerminalLayout.GetPictureBounds(picture, _center_object, out var isUnscaled);
                    AddImage(picture.Texture, frame, isUnscaled);
                }
                else
                {
                    AddMissing(Strings.Get(Strings.Terminals, "Terminals.Missing.Picture", group.permutation),TerminalLayout.GetObjectBounds(_center_object));
                }
            }

            var textBounds = TerminalLayout.GetLogonTextBounds(terminal, group, frame);
            AddLines(TerminalLayout.GetLines(terminal, group, textBounds, 0, terminal.lines_per_page), textBounds);
        }

        // display_picture
        private void AddPicture(short pictureId, short flags)
        {
            var picture = ScenarioPictures.Get(pictureId);
            if (picture != null)
            {
                var bounds = TerminalLayout.GetPictureBounds(picture, flags, out var isUnscaled);
                AddImage(picture.Texture, bounds, isUnscaled);
            }
            else
            {
                AddMissing(Strings.Get(Strings.Terminals, "Terminals.Missing.Picture", pictureId),TerminalLayout.GetObjectBounds(flags));
            }
        }

        // The page is drawn around the map's bounds, where the level shows through, framed on the goal
        private void AddCheckpoint(terminal_groupings group, Rect fullScreen, bool frameCamera)
        {
            var mapBounds = TerminalLayout.GetObjectBounds(group.flags);

            AddBackground(new Rect { top = fullScreen.top, left = fullScreen.left, bottom = mapBounds.top, right = fullScreen.right });
            AddBackground(new Rect { top = mapBounds.bottom, left = fullScreen.left, bottom = fullScreen.bottom, right = fullScreen.right });
            AddBackground(new Rect { top = mapBounds.top, left = fullScreen.left, bottom = mapBounds.bottom, right = mapBounds.left });
            AddBackground(new Rect { top = mapBounds.top, left = mapBounds.right, bottom = mapBounds.bottom, right = fullScreen.right });

            var goalAreas = GetGoalAreas(group.permutation);
            if (goalAreas.Count == 0)
            {
                AddMissing(Strings.Get(Strings.Terminals, "Terminals.Missing.Checkpoint", group.permutation),mapBounds);
                return;
            }

            var window = new VisualElement { pickingMode = PickingMode.Ignore };
            window.AddToClassList("fp-terminal__map");
            Place(window, mapBounds);
            screen.Add(window);

            if (frameCamera)
            {
                window.RegisterCallback<GeometryChangedEvent, List<ISelectable>>(FrameOnGoal, goalAreas);
            }
        }

        // The polygons the checkpoint's goals are in (as find_checkpoint_location finds the goals)
        private static List<ISelectable> GetGoalAreas(short checkpointIndex)
        {
            var goalAreas = new List<ISelectable>();
            var level = LevelEntity_Level.Instance;
            if (!level)
            {
                return goalAreas;
            }

            foreach (var mapObject in level.MapObjects.Values)
            {
                var savedObject = mapObject.NativeObject;
                if (savedObject.type == map._saved_goal && savedObject.index == checkpointIndex)
                {
                    goalAreas.Add(level.Polygons.TryGetValue(savedObject.polygon_index, out var polygon) ? (ISelectable) polygon : mapObject);
                }
            }

            return goalAreas;
        }

        private void FrameOnGoal(GeometryChangedEvent geometryChangedEvent, List<ISelectable> goalAreas)
        {
            var window = (VisualElement) geometryChangedEvent.target;
            window.UnregisterCallback<GeometryChangedEvent, List<ISelectable>>(FrameOnGoal);

            var panelBounds = window.panel.visualTree.worldBound;
            var windowBounds = window.worldBound;
            var viewportArea = new UnityEngine.Rect(
                windowBounds.xMin / panelBounds.width,
                1f - windowBounds.yMax / panelBounds.height,
                windowBounds.width / panelBounds.width,
                windowBounds.height / panelBounds.height);

            ForgePlusUI.Instance.EditorCamera.Frame(goalAreas, viewportArea);
        }

        // fill_terminal_with_static, redrawn as often as it would be dirtied
        private void AddStatic(Rect bounds)
        {
            if (!staticTexture)
            {
                staticTexture = new Texture2D(StaticWidth, StaticHeight, TextureFormat.RGBA32, mipChain: false)
                {
                    name = "Terminal Static",
                    filterMode = FilterMode.Point,
                };
            }

            RandomizeStatic();

            AddImage(staticTexture, bounds, isUnscaled: false);

            staticAnimation = screen.schedule.Execute(RandomizeStatic).Every(50);
        }

        private static void RandomizeStatic()
        {
            var pixels = staticTexture.GetPixelData<Color32>(0);
            for (var i = 0; i < pixels.Length; i++)
            {
                var value = (byte) Random.Range(0, 256);
                pixels[i] = new Color32(value, value, value, 255);
            }

            staticTexture.Apply(updateMipmaps: false);
        }

        private void AddBackground(Rect bounds)
        {
            if (RECTANGLE_WIDTH(bounds) <= 0 || RECTANGLE_HEIGHT(bounds) <= 0)
            {
                return;
            }

            var background = new VisualElement();
            background.AddToClassList("fp-terminal__background");
            Place(background, bounds);
            screen.Add(background);
        }

        private void AddImage(Texture2D texture, Rect bounds, bool isUnscaled)
        {
            var image = new Image { image = texture, scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            image.AddToClassList("fp-terminal__picture");
            Place(image, bounds);

            if (isUnscaled)
            {
                image.style.width = texture.width * scale;
                image.style.height = texture.height * scale;
            }

            screen.Add(image);
        }

        // Where Aleph One can't find what it would draw, it fills the space with black and says so
        private void AddMissing(string message, Rect bounds)
        {
            var missing = new Label(message) { pickingMode = PickingMode.Ignore };
            missing.AddToClassList("fp-terminal__missing");
            Place(missing, bounds);
            missing.style.fontSize = FontSize * scale;
            screen.Add(missing);
        }

        // What a group that has nothing to draw does instead: its directive, as terminal source would have it, and
        // what Aleph One does with it
        private void AddNote(string directive, string description, Rect bounds)
        {
            var note = new VisualElement { pickingMode = PickingMode.Ignore };
            note.AddToClassList("fp-terminal__note");
            Place(note, bounds);

            if (directive != null)
            {
                var directiveLabel = new Label(directive) { enableRichText = false, pickingMode = PickingMode.Ignore };
                directiveLabel.AddToClassList("fp-terminal__note-directive");
                note.Add(directiveLabel);
            }

            var descriptionLabel = new Label(description) { enableRichText = false, pickingMode = PickingMode.Ignore };
            descriptionLabel.AddToClassList("fp-terminal__note-description");
            note.Add(descriptionLabel);

            screen.Add(note);
        }

        // Each run at its column, on its line's baseline, in its face and color
        private void AddLines(List<TerminalLayout.Line> lines, Rect bounds)
        {
            foreach (var line in lines)
            {
                var baseline = bounds.top + _get_font_line_height() * (line.Number + FUDGE_FACTOR);

                foreach (var run in line.Runs)
                {
                    var label = new Label(run.Text) { enableRichText = false, pickingMode = PickingMode.Ignore };
                    label.AddToClassList("fp-terminal__text");
                    label.style.left = (bounds.left + run.Column * terminal_font_character_width) * scale;
                    label.style.top = (baseline - ScreenTop - Ascent) * scale;
                    label.style.fontSize = FontSize * scale;
                    TerminalText.ApplyStyle(label, run.Face, run.Color, scale);

                    screen.Add(label);
                }
            }
        }

        private void Place(VisualElement element, Rect bounds)
        {
            element.style.position = Position.Absolute;
            element.style.left = bounds.left * scale;
            element.style.top = (bounds.top - ScreenTop) * scale;
            element.style.width = RECTANGLE_WIDTH(bounds) * scale;
            element.style.height = RECTANGLE_HEIGHT(bounds) * scale;
        }

        // What Aleph One does when the terminal reaches the group
        private static string Describe(terminal_groupings group)
        {
            switch (group.type)
            {
                case _unfinished_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Unfinished.Description");
                case _success_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Success.Description");
                case _failure_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Failure.Description");
                case _end_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.End.Description");
                case _interlevel_teleport_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.InterlevelTeleport.Description", group.permutation);
                case _intralevel_teleport_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.IntralevelTeleport.Description", group.permutation);
                case _sound_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Sound.Description", group.permutation);
                case _tag_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Tag.Description", group.permutation);
                case _movie_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Movie.Description", group.permutation);
                case _track_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Track.Description", group.permutation);
                case _camera_group:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Camera.Description", group.permutation);
                default:
                    return Strings.Get(Strings.Terminals, "Terminals.Group.Default.Description", group.type);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BackupPower;

/// <summary>
/// A general-purpose gizmo that exposes a <see cref="FloatRange" /> through two independently
/// draggable target markers on a single fillable bar - the lower bound (min) and the upper
/// bound (max). Mirrors <c>Verse.Gizmo_Slider</c> / <c>Gizmo_SetFuelLevel</c> in look and feel,
/// but with two targets instead of one. Not hard-wired to any domain: subclasses provide the
/// bounds, the current value to fill to, labels and drag state.
/// </summary>
/// <remarks>
///     <para>
///     Drag state (<see cref="DraggingMin" /> / <see cref="DraggingMax" />) is abstract because
///     it must survive a gizmo being rebuilt every frame (the common case for gizmos produced
///     from <c>CompGetGizmosExtra</c>). Subclasses should back these with <c>static</c> fields,
///     exactly as <c>Gizmo_SetFuelLevel</c> does for its single bar.
///     </para>
///     <para>
///     <b>Multiple selection:</b> inherits the <c>Gizmo</c> defaults, i.e. gizmos do
///     <em>not</em> group or merge. Each selected object draws and controls its own gizmo, so
///     dragging one does not affect the others. Override <see cref="Gizmo.GroupsWith" />,
///     <see cref="Gizmo.MergeWith(Gizmo)" /> and <see cref="Gizmo.ProcessGroupInput" /> to
///     implement collective behaviour.
///     </para>
/// </remarks>
// ReSharper disable once InconsistentNaming
[StaticConstructorOnStartup]
public abstract class Gizmo_RangeSlider : Gizmo
{
    private const float Spacing = 8f;

    private static readonly Texture2D BarTex =
        SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

    private static readonly Texture2D BarHighlightTex =
        SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

    private static readonly Texture2D EmptyBarTex =
        SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

    private static readonly Texture2D DragBarTex =
        SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

    private Texture2D _bandTex = null!;
    private Texture2D _barHighlightTex = null!;

    private Texture2D _barTex = null!;
    private bool _drawBand;
    private bool _initialized;
    private Texture2D _maxDragTex = null!;
    private Texture2D _minDragTex = null!;
    private float _targetMaxPct;

    private float _targetMinPct;

    public override float Order => -100f;

    public List<(string label, Action action)> RightClickOptions { get; } = [];

    /// <summary>
    /// Combines any base-provided options with those registered via
    /// <see cref="RightClickOptions" /> / <see cref="AddRightClickOption(string,Action)" />.
    /// Still overridable for full control if a subclass needs dynamic generation instead.
    /// </summary>
    public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
    {
        get
        {
            foreach (FloatMenuOption option in base.RightClickFloatMenuOptions)
                yield return option;

            // FloatMenuOption ctor -> Label -> CalcHeight happens HERE, on the main thread. Safe.
            foreach ((string label, Action action) in RightClickOptions)
            {
                yield return new FloatMenuOption(label, action);
            }
        }
    }

    protected virtual float Width => 160f;

    protected virtual float DrawHeight => 75f;

    /// <summary>
    /// The editable bounds, expressed in <see cref="DragRange" /> space.
    /// </summary>
    protected abstract FloatRange Target { get; set; }

    /// <summary>
    /// The current value to fill the bar to, in <see cref="DragRange" /> space.
    /// </summary>
    protected abstract float ValuePercent { get; }

    protected abstract string Title { get; }

    protected abstract bool DraggingMin { get; set; }

    protected abstract bool DraggingMax { get; set; }

    protected virtual bool IsDraggable => true;

    protected virtual FloatRange DragRange => FloatRange.ZeroToOne;

    protected virtual int Increments => 20;

    /// <summary>
    /// When true, dragging a bound past the other pushes the other along, keeping min &lt;= max.
    /// </summary>
    protected virtual bool EnforceOrdered => true;

    protected virtual string? HighlightTag => null;

    protected virtual string BarLabel =>
        $"{Target.min.ToStringPercent("0")} - {Target.max.ToStringPercent("0")}";

    // Colours: returning `new Color()` (the default struct) is a sentinel meaning
    // "use the shared static texture". Any other value allocates a texture once per gizmo.
    // This mirrors Gizmo_Slider and avoids per-frame allocations for the common, uncustomised case.
    protected virtual Color BarColor => new Color();
    protected virtual Color BarHighlightColor => new Color();
    protected virtual Color MinMarkerColor => new Color();
    protected virtual Color MaxMarkerColor => new Color();

    /// <summary>
    /// Colour of the band drawn between the two markers. Default (<c>new Color()</c>) draws no band.
    /// </summary>
    protected virtual Color BandColor => new Color();

    private bool HasRightClickOptions =>
        RightClickOptions.Count > 0;

    public sealed override float GetWidth(float maxWidth) => Width;

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        if (!_initialized)
            Initialize();

        // Keep the drag markers in sync with the source when not actively dragging, so external
        // changes to Target are reflected on the bar.
        if (!DraggingMin)
            _targetMinPct = Mathf.Clamp(Target.min, DragRange.min, DragRange.max);

        if (!DraggingMax)
            _targetMaxPct = Mathf.Clamp(Target.max, DragRange.min, DragRange.max);

        Rect outer = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), DrawHeight);
        Rect inner = outer.ContractedBy(Spacing);
        Widgets.DrawWindowBackground(outer);

        bool mouseOverElement = false;
        Text.Font = GameFont.Small;
        Rect headerRect = inner with { height = Text.LineHeight };
        DrawHeader(headerRect, ref mouseOverElement);

        Rect barRect = inner;
        barRect.yMin = headerRect.yMax + Spacing;
        DrawBar(barRect, Mouse.IsOver(barRect));

        if (Mouse.IsOver(outer) && !mouseOverElement)
        {
            Widgets.DrawHighlight(outer);
            TooltipHandler.TipRegion(outer, GetTooltip,
                Gen.HashCombineInt(GetHashCode(), 8573612));
        }

        if (!HighlightTag.NullOrEmpty())
            UIHighlighter.HighlightOpportunity(outer, HighlightTag);

        // Detect right-click so the grid drawer opens our RightClickFloatMenuOptions.
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1
                                                      && Mouse.IsOver(outer) && HasRightClickOptions)
            return new GizmoResult(GizmoState.OpenedFloatMenu, Event.current);

        return Mouse.IsOver(outer)
            ? new GizmoResult(GizmoState.Mouseover)
            : new GizmoResult(GizmoState.Clear);
    }

    /// <summary>
    /// Register a right-click menu entry that runs <paramref name="action" /> when clicked.
    /// For richer options (priority, icon, tooltip, disabled state) construct a
    /// <see cref="FloatMenuOption" /> and add it to <see cref="RightClickOptions" /> directly.
    /// </summary>
    protected void AddRightClickOption(string label, Action action)
    {
        // Defer options so that creation occurs on main thread. When RightClickFloatMenuOptions() is called
        // by the engine, it is on the correct thread. If we try to construct here, it will go very wrong,
        // IMGUI will crash, and we will experience all kinds of graphics issues, if not crash outright.
        RightClickOptions.Add((label, action)); // no GUI work at call time
    }

    protected abstract string GetTooltip();

    protected virtual void DrawHeader(Rect headerRect, ref bool mouseOverElement)
    {
        string label = Title.Truncate(headerRect.width);
        Widgets.Label(headerRect, label);
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _barTex = ResolveTex(BarColor, BarTex);
        _barHighlightTex = ResolveTex(BarHighlightColor, BarHighlightTex);
        _minDragTex = ResolveTex(MinMarkerColor, DragBarTex);
        _maxDragTex = ResolveTex(MaxMarkerColor, DragBarTex);
        _bandTex = ResolveTex(BandColor, BarHighlightTex);
        _drawBand = BandColor != new Color();
    }

    private static Texture2D ResolveTex(Color color, Texture2D fallback) =>
        color == new Color() ? fallback : SolidColorMaterials.NewSolidColorTexture(color)!;

    private void DrawBar(Rect barRect, bool mouseOver)
    {
        float fillNorm = Mathf.Clamp01(Normalize(ValuePercent));
        Texture2D fillTex = mouseOver ? _barHighlightTex : _barTex;

        Widgets.FillableBar(barRect, fillNorm, fillTex, EmptyBarTex, true);

        float minNorm = Normalize(_targetMinPct);
        float maxNorm = Normalize(_targetMaxPct);

        if (_drawBand)
            DrawBand(barRect, minNorm, maxNorm);

        DrawTargetMarker(barRect, minNorm, _minDragTex);
        DrawTargetMarker(barRect, maxNorm, _maxDragTex);

        HandleDrag(barRect);

        // Label on top of the bar, matching Gizmo_Slider.
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(barRect, BarLabel);
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    private float Normalize(float value) => Mathf.InverseLerp(DragRange.min, DragRange.max, value);

    private static void DrawTargetMarker(Rect barRect, float normalizedPct, Texture2D tex)
    {
        float x = Mathf.Round((barRect.width - 8f) * normalizedPct);
        Rect line = new Rect(barRect.x + 3f + x, barRect.y, 2f, barRect.height);
        GUI.DrawTexture(line, tex);
        Rect nub = new Rect(barRect.x + 2f + x, barRect.y - 3f, 4f, 5f);
        GUI.DrawTexture(nub, tex);
        GUI.DrawTexture(new Rect(nub.x, barRect.yMax - 2f, nub.width, nub.height), tex);
    }

    private void DrawBand(Rect barRect, float minNorm, float maxNorm)
    {
        float xMin = barRect.x + 3f + (barRect.width - 8f) * minNorm;
        float xMax = barRect.x + 3f + (barRect.width - 8f) * maxNorm;
        Rect band = new Rect(xMin, barRect.y, xMax - xMin, barRect.height);
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.35f);
        GUI.DrawTexture(band, _bandTex);
        GUI.color = Color.white;
    }

    private void HandleDrag(Rect barRect)
    {
        if (!IsDraggable)
            return;

        Event ev = Event.current;
        bool overBar = Mouse.IsOver(barRect);
        float mousePct = SnapToIncrements(MouseToPct(barRect, ev.mousePosition.x));

        // Begin a drag on left-press over the bar: pick the nearer marker.
        if (ev is { type: EventType.MouseDown, button: 0 } && overBar && !DraggingMin && !DraggingMax)
        {
            // When the two markers are (near-)coincident, pick by which side of the shared
            // position the cursor is on: left of it grabs min, right of it grabs max. This
            // lets the player pull them apart in either direction even when min == max.
            bool coincident = Mathf.Abs(_targetMinPct - _targetMaxPct) < 0.001f;
            bool pickMin = coincident
                ? mousePct <= _targetMinPct
                : Mathf.Abs(mousePct - _targetMinPct) <= Mathf.Abs(mousePct - _targetMaxPct);
            if (pickMin)
                DraggingMin = true;
            else
                DraggingMax = true;

            ApplyDraggedTarget(mousePct, pickMin);
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            ev.Use();
        }

        // Continue dragging.
        if ((DraggingMin || DraggingMax) && UnityGUIBugsFixer.MouseDrag())
        {
            bool min = DraggingMin;
            float prev = min ? _targetMinPct : _targetMaxPct;
            if (Mathf.Abs(mousePct - prev) > Mathf.Epsilon)
            {
                ApplyDraggedTarget(mousePct, min);
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
            }

            if (ev.type == EventType.MouseDrag)
                ev.Use();
        }

        // End dragging.
        if ((DraggingMin || DraggingMax) && ev is { type: EventType.MouseUp, button: 0 })
        {
            DraggingMin = false;
            DraggingMax = false;
            ev.Use();
        }
    }

    private float MouseToPct(Rect barRect, float mouseX)
    {
        float span = DragRange.max - DragRange.min;
        float pct = DragRange.min + (mouseX - barRect.x) / barRect.width * span;
        return Mathf.Clamp(pct, DragRange.min, DragRange.max);
    }

    private float SnapToIncrements(float pct)
    {
        if (Increments <= 0)
            return pct;

        float span = DragRange.max - DragRange.min;
        if (span <= 0f)
            return pct;

        float step = span / Increments;
        return Mathf.Round((pct - DragRange.min) / step) * step + DragRange.min;
    }

    private void ApplyDraggedTarget(float pct, bool min)
    {
        pct = Mathf.Clamp(pct, DragRange.min, DragRange.max);
        if (min)
        {
            _targetMinPct = pct;
            if (EnforceOrdered && _targetMinPct > _targetMaxPct)
                _targetMaxPct = _targetMinPct;
        }
        else
        {
            _targetMaxPct = pct;
            if (EnforceOrdered && _targetMaxPct < _targetMinPct)
                _targetMinPct = _targetMaxPct;
        }

        Target = new FloatRange(_targetMinPct, _targetMaxPct);
    }
}
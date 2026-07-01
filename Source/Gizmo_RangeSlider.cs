// Gizmo_RangeSlider.cs
// Copyright Karel Kroeze, 2020-2025

using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BackupPower;

/// <summary>
///     A general-purpose gizmo that exposes a <see cref="FloatRange" /> through two stacked,
///     independently draggable bars - one for the lower bound and one for the upper bound.
///     Mirrors <c>Verse.Gizmo_Slider</c> in spirit and rendering, but for a range instead of a
///     single value. Not hard-wired to any domain: subclasses provide the bounds, the current
///     values to display, labels and drag state.
/// </summary>
/// <remarks>
///     <para>
///         Drag state (<see cref="DraggingMin" /> / <see cref="DraggingMax" />) is abstract because
///         it must survive a gizmo being rebuilt every frame (the common case for gizmos produced
///         from <c>CompGetGizmosExtra</c>). Subclasses should back these with <c>static</c> fields,
///         exactly as <c>Gizmo_SetFuelLevel</c> does for its single bar.
///     </para>
///     <para>
///         <b>Multiple selection:</b> inherits the <c>Gizmo</c> defaults, i.e. gizmos do
///         <em>not</em> group or merge. Each selected object draws and controls its own gizmo, so
///         dragging one does not affect the others. Override <see cref="Gizmo.GroupsWith" />,
///         <see cref="Gizmo.MergeWith(Gizmo)" /> and <see cref="Gizmo.ProcessGroupInput" /> to
///         implement collective behaviour.
///     </para>
/// </remarks>
public abstract class Gizmo_RangeSlider : Gizmo
{
	private const float Spacing = 8f;
	private const float BarSpacing = 4f;

	private static readonly Texture2D BarTex =
		SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));

	private static readonly Texture2D BarHighlightTex =
		SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));

	private static readonly Texture2D EmptyBarTex =
		SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

	private static readonly Texture2D DragBarTex =
		SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

	private Texture2D _minBarTex = null!;
	private Texture2D _minBarHighlightTex = null!;
	private Texture2D _minBarDragTex = null!;
	private Texture2D _maxBarTex = null!;
	private Texture2D _maxBarHighlightTex = null!;
	private Texture2D _maxBarDragTex = null!;

	private float _targetMinPct;
	private float _targetMaxPct;
	private bool _initialized;

	protected virtual float Width => 160f;

	/// <summary>
	///     Total drawn height. The vanilla gizmo grid reserves 75f + 14f (spacing) per row, so the
	///     default of 89f fits two usable bars without spilling into the next row's content.
	/// </summary>
	protected virtual float DrawHeight => 89f;

	public sealed override float GetWidth(float maxWidth) => Width;

	public override float Order => -100f;

	/// <summary>The editable bounds, expressed in <see cref="DragRange" /> space.</summary>
	protected abstract FloatRange Target { get; set; }

	/// <summary>
	///     The values currently displayed as the filled portion of each bar, in
	///     <see cref="DragRange" /> space. <c>min</c> drives the lower bar, <c>max</c> the upper.
	/// </summary>
	protected abstract FloatRange ValueRange { get; }

	protected abstract string Title { get; }

	protected abstract bool DraggingMin { get; set; }

	protected abstract bool DraggingMax { get; set; }

	protected abstract string GetTooltip();

	protected virtual bool IsDraggable => true;

	protected virtual FloatRange DragRange => FloatRange.ZeroToOne;

	protected virtual int Increments => 20;

	/// <summary>When true, dragging the lower bound above the upper (or vice versa) pushes the other bound along.</summary>
	protected virtual bool EnforceOrdered => true;

	protected virtual string HighlightTag => null;

	protected virtual IEnumerable<float>? BarThresholds => null;

	protected virtual string MinLabel => Target.min.ToStringPercent("0");

	protected virtual string MaxLabel => Target.max.ToStringPercent("0");

	// Colours: returning `new Color()` (the default struct) is a sentinel meaning
	// "use the shared static texture". Any other value allocates a texture once per gizmo.
	// This mirrors Gizmo_Slider and avoids per-frame allocations for the common, uncustomised case.
	protected virtual Color MinBarColor => new Color();
	protected virtual Color MaxBarColor => new Color();
	protected virtual Color MinBarHighlightColor => new Color();
	protected virtual Color MaxBarHighlightColor => new Color();
	protected virtual Color MinBarDragColor => new Color();
	protected virtual Color MaxBarDragColor => new Color();

	private readonly List<FloatMenuOption> _rightClickOptions = new();

	/// <summary>
	///     Imperatively registered right-click menu options, merged into
	///     <see cref="Gizmo.RightClickFloatMenuOptions" />. Callers can either add ready-made
	///     <see cref="FloatMenuOption" />s directly, or use
	///     <see cref="AddRightClickOption(string,Action)" /> for the common case.
	/// </summary>
	public List<FloatMenuOption> RightClickOptions => _rightClickOptions;

	/// <summary>
	///     Register a right-click menu entry that runs <paramref name="action" /> when clicked.
	///     For richer options (priority, icon, tooltip, disabled state) construct a
	///     <see cref="FloatMenuOption" /> and add it to <see cref="RightClickOptions" /> directly.
	/// </summary>
	public void AddRightClickOption(string label, Action action)
	{
		_rightClickOptions.Add(new FloatMenuOption(label, action));
	}

	/// <summary>
	///     Combines any base-provided options with those registered via
	///     <see cref="RightClickOptions" /> / <see cref="AddRightClickOption(string,Action)" />.
	///     Still overridable for full control if a subclass needs dynamic generation instead.
	/// </summary>
	public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
	{
		get
		{
			foreach (FloatMenuOption option in base.RightClickFloatMenuOptions)
			{
				yield return option;
			}

			for (int i = 0; i < _rightClickOptions.Count; i++)
			{
				yield return _rightClickOptions[i];
			}
		}
	}

	private void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		_minBarTex = ResolveTex(MinBarColor, BarTex);
		_minBarHighlightTex = ResolveTex(MinBarHighlightColor, BarHighlightTex);
		_minBarDragTex = ResolveTex(MinBarDragColor, DragBarTex);
		_maxBarTex = ResolveTex(MaxBarColor, BarTex);
		_maxBarHighlightTex = ResolveTex(MaxBarHighlightColor, BarHighlightTex);
		_maxBarDragTex = ResolveTex(MaxBarDragColor, DragBarTex);
	}

	private static Texture2D ResolveTex(Color color, Texture2D fallback)
	{
		return color == new Color() ? fallback : SolidColorMaterials.NewSolidColorTexture(color)!;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		if (!_initialized)
		{
			Initialize();
		}

		// Keep the drag markers in sync with the source when not actively dragging, so external
		// changes to Target are reflected on the bars.
		if (!DraggingMin)
		{
			_targetMinPct = Mathf.Clamp(Target.min, DragRange.min, DragRange.max);
		}

		if (!DraggingMax)
		{
			_targetMaxPct = Mathf.Clamp(Target.max, DragRange.min, DragRange.max);
		}

		Rect outer = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), DrawHeight);
		Rect inner = outer.ContractedBy(Spacing);
		Widgets.DrawWindowBackground(outer);

		bool mouseOverElement = false;
		Text.Font = GameFont.Small;
		Rect headerRect = inner with { height = Text.LineHeight };
		DrawHeader(headerRect, ref mouseOverElement);

		float barsTop = headerRect.yMax + Spacing;
		float barsHeight = inner.yMax - barsTop;
		float barHeight = (barsHeight - BarSpacing) / 2f;
		Rect minRect = new Rect(inner.x, barsTop, inner.width, barHeight);
		Rect maxRect = new Rect(inner.x, minRect.yMax + BarSpacing, inner.width, barHeight);

		DrawBar(minRect, isMin: true);
		DrawBar(maxRect, isMin: false);

		if (Mouse.IsOver(outer) && !mouseOverElement)
		{
			Widgets.DrawHighlight(outer);
			TooltipHandler.TipRegion(outer, new Func<string>(GetTooltip),
				Gen.HashCombineInt(GetHashCode(), 8573612));
		}

		if (!HighlightTag.NullOrEmpty())
		{
			UIHighlighter.HighlightOpportunity(outer, HighlightTag);
		}

		return new GizmoResult(GizmoState.Clear);
	}

	private void DrawBar(Rect barRect, bool isMin)
	{
		Texture2D barTex = isMin ? _minBarTex : _maxBarTex;
		Texture2D highlightTex = isMin ? _minBarHighlightTex : _maxBarHighlightTex;
		Texture2D dragTex = isMin ? _minBarDragTex : _maxBarDragTex;
		float valuePct = isMin ? ValueRange.min : ValueRange.max;
		bool dragging = isMin ? DraggingMin : DraggingMax;
		float targetPct = isMin ? _targetMinPct : _targetMaxPct;

		if (!IsDraggable)
		{
			Widgets.FillableBar(barRect, Mathf.Min(valuePct, 1f), barTex, EmptyBarTex, true);
		}
		else
		{
			Widgets.DraggableBar(barRect, barTex, highlightTex, EmptyBarTex, dragTex,
				ref dragging, Mathf.Min(valuePct, 1f), ref targetPct,
				BarThresholds, Increments, DragRange.min, DragRange.max);

			if (isMin)
			{
				DraggingMin = dragging;
			}
			else
			{
				DraggingMax = dragging;
			}

			targetPct = Mathf.Clamp(targetPct, DragRange.min, DragRange.max);

			// Keep the bounds ordered, then write back so the source of truth stays in sync.
			if (isMin)
			{
				_targetMinPct = targetPct;
				if (EnforceOrdered && _targetMinPct > _targetMaxPct)
				{
					_targetMaxPct = _targetMinPct;
				}
			}
			else
			{
				_targetMaxPct = targetPct;
				if (EnforceOrdered && _targetMaxPct < _targetMinPct)
				{
					_targetMinPct = _targetMaxPct;
				}
			}

			Target = new FloatRange(_targetMinPct, _targetMaxPct);
		}

		// Label on top of the bar, matching Gizmo_Slider.
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(barRect, isMin ? MinLabel : MaxLabel);
		Text.Anchor = TextAnchor.UpperLeft;
		Text.Font = GameFont.Small;
	}

	protected virtual void DrawHeader(Rect headerRect, ref bool mouseOverElement)
	{
		string label = Title.Truncate(headerRect.width);
		Widgets.Label(headerRect, label);
	}
}

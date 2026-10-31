using System.Numerics;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.DamageOverlays.Overlays;

public sealed class DamageOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CircleMaskShader = "GradientCircleMask";
    // Inferus - shader that replaces the black/white crit overlays
    private static readonly ProtoId<ShaderPrototype> InferusCritDistortionShader = "InferusCritDistortion";
    // Inferus - shader that replaces the red pulsing pain overlay
    private static readonly ProtoId<ShaderPrototype> InferusPainStaticShader = "InferusPainStatic";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    // Inferus - the crit distortion shader samples what has already been rendered
    public override bool RequestScreenTexture => true;

    // Inferus - the separate white crit circle-mask shader was removed, the crit distortion shader replaces it
    private readonly ShaderInstance _oxygenShader;
    // Inferus - the red circle-mask pain shader was removed, the pain static shader replaces it
    private readonly ShaderInstance _painStaticShader;
    // Inferus - crit distortion shader instance
    private readonly ShaderInstance _critDistortionShader;

    public MobState State = MobState.Alive;

    /// <summary>
    /// Handles the red pulsing overlay
    /// </summary>
    public float PainLevel = 0f;

    private float _oldPainLevel = 0f;

    /// <summary>
    /// Handles the darkening overlay.
    /// </summary>
    public float OxygenLevel = 0f;

    private float _oldOxygenLevel = 0f;

    /// <summary>
    /// Handles the white overlay when crit.
    /// </summary>
    public float CritLevel = 0f;

    private float _oldCritLevel = 0f;

    public float DeadLevel = 1f;

    public DamageOverlay()
    {
        // TODO: Replace
        IoCManager.InjectDependencies(this);
        _oxygenShader = _prototypeManager.Index(CircleMaskShader).InstanceUnique();
        // Inferus - removed the crit circle-mask shader instance, replaced by the crit distortion shader
        // Inferus - removed the brute circle-mask shader instance, replaced by the pain static shader
        // Inferus - load the pain static shader
        _painStaticShader = _prototypeManager.Index(InferusPainStaticShader).InstanceUnique();
        // Inferus - load the crit distortion shader
        _critDistortionShader = _prototypeManager.Index(InferusCritDistortionShader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return;

        if (args.Viewport.Eye != eyeComp.Eye)
            return;

        // Inferus - scale the vision circles with the eye zoom so zooming out
        // can't be used to see more than the vignettes allow
        var zoom = eyeComp.Zoom.X;

        /*
         * Here's the rundown:
         * 1. There's lerping for each level so the transitions are smooth.
         * 2. There's 3 overlays, 1 for brute damage, 1 for oxygen damage (that also doubles as a crit overlay),
         * and a white one during crit that closes in as you progress towards death. When you die it slowly disappears.
         * The crit overlay also occasionally reduces its alpha as a "blink"
         * Inferus - the white crit overlay was replaced by a full-screen distortion shader
         * that tears/desaturates/darkens the world the deeper into crit you are, and the
         * red pain overlay was replaced by a staticky red vignette shader.
         */

        var viewport = args.WorldAABB;
        var handle = args.WorldHandle;
        var distance = args.ViewportBounds.Width;

        var time = (float) _timing.RealTime.TotalSeconds;
        var lastFrameTime = (float) _timing.FrameTime.TotalSeconds;

        // If they just died then lerp out the white overlay.
        if (State != MobState.Dead)
        {
            DeadLevel = 1f;
        }
        else if (!MathHelper.CloseTo(0f, DeadLevel, 0.001f))
        {
            var diff = -DeadLevel;
            DeadLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            DeadLevel = 0f;
        }

        if (!MathHelper.CloseTo(_oldPainLevel, PainLevel, 0.001f))
        {
            var diff = PainLevel - _oldPainLevel;
            _oldPainLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            _oldPainLevel = PainLevel;
        }

        if (!MathHelper.CloseTo(_oldOxygenLevel, OxygenLevel, 0.001f))
        {
            var diff = OxygenLevel - _oldOxygenLevel;
            _oldOxygenLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            _oldOxygenLevel = OxygenLevel;
        }

        if (!MathHelper.CloseTo(_oldCritLevel, CritLevel, 0.001f))
        {
            var diff = CritLevel - _oldCritLevel;
            _oldCritLevel += GetDiff(diff, lastFrameTime);
        }
        else
        {
            _oldCritLevel = CritLevel;
        }

        /*
         * darknessAlphaOuter is the maximum alpha for anything outside of the larger circle
         * darknessAlphaInner (on the shader) is the alpha for anything inside the smallest circle
         *
         * outerCircleRadius is what we end at for max level for the outer circle
         * outerCircleMaxRadius is what we start at for 0 level for the outer circle
         *
         * innerCircleRadius is what we end at for max level for the inner circle
         * innerCircleMaxRadius is what we start at for 0 level for the inner circle
         */

        // Makes debugging easier don't @ me
        float level = 0f;
        level = _oldPainLevel;

        // TODO: Lerping
        if (level > 0f && _oldCritLevel <= 0f)
        {
            // Inferus
            if (ScreenTexture == null)
                return;

            _painStaticShader.SetParameter("painLevel", level);
            _painStaticShader.SetParameter("zoom", zoom);
            _painStaticShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_painStaticShader);
            handle.DrawRect(viewport, Color.White);
        }
        else
        {
            _oldPainLevel = PainLevel;
        }

        // Inferus
        level = State != MobState.Critical ? _oldOxygenLevel : 0f;

        if (level > 0f)
        {
            float outerMaxLevel = 0.6f * distance;
            float outerMinLevel = 0.06f * distance;
            float innerMaxLevel = 0.02f * distance;
            float innerMinLevel = 0.02f * distance;

            var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

            float outerDarkness;
            float critTime;

            // If in crit then just fix it; also pulse it very occasionally so they can see more.
            if (_oldCritLevel > 0f)
            {
                var adjustedTime = time * 2f;
                critTime = MathF.Max(0, MathF.Sin(adjustedTime) + 2 * MathF.Sin(2 * adjustedTime / 4f) + MathF.Sin(adjustedTime / 4f) - 3f);

                if (critTime > 0f)
                {
                    outerDarkness = 1f - critTime / 1.5f;
                }
                else
                {
                    outerDarkness = 1f;
                }
            }
            else
            {
                outerDarkness = MathF.Min(0.98f, 0.3f * MathF.Log(level) + 1f);
            }

            _oxygenShader.SetParameter("time", 0.0f);
            _oxygenShader.SetParameter("color", new Vector3(0f, 0f, 0f));
            _oxygenShader.SetParameter("darknessAlphaOuter", outerDarkness);
            _oxygenShader.SetParameter("innerCircleRadius", innerRadius);
            _oxygenShader.SetParameter("innerCircleMaxRadius", innerRadius);
            _oxygenShader.SetParameter("outerCircleRadius", outerRadius);
            _oxygenShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            handle.UseShader(_oxygenShader);
            handle.DrawRect(viewport, Color.White);
        }

        level = State != MobState.Dead ? _oldCritLevel : DeadLevel;

        if (level > 0f)
        {
            // Inferus
            if (ScreenTexture == null)
                return;

            _critDistortionShader.SetParameter("critLevel", level);
            _critDistortionShader.SetParameter("zoom", zoom);
            _critDistortionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_critDistortionShader);
            handle.DrawRect(viewport, Color.White);
        }

        handle.UseShader(null);
    }

    private float GetDiff(float value, float lastFrameTime)
    {
        var adjustment = value * 5f * lastFrameTime;

        if (value < 0f)
            adjustment = Math.Clamp(adjustment, value, -value);
        else
            adjustment = Math.Clamp(adjustment, -value, value);

        return adjustment;
    }
}

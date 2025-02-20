﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Utils;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableSliderRepeat : DrawableOsuHitObject, ITrackSnaking
    {
        public new SliderRepeat HitObject => (SliderRepeat)base.HitObject;

        [CanBeNull]
        public Slider Slider => DrawableSlider?.HitObject;

        public DrawableSlider DrawableSlider => (DrawableSlider)ParentHitObject;

        private double animDuration;

        public SkinnableDrawable CirclePiece { get; private set; }

        public ReverseArrowPiece Arrow { get; private set; }

        private Drawable scaleContainer;

        public override bool DisplayResult => false;

        public DrawableSliderRepeat()
            : base(null)
        {
        }

        public DrawableSliderRepeat(SliderRepeat sliderRepeat)
            : base(sliderRepeat)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Origin = Anchor.Centre;
            Size = new Vector2(OsuHitObject.OBJECT_RADIUS * 2);

            AddInternal(scaleContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    // no default for this; only visible in legacy skins.
                    CirclePiece = new SkinnableDrawable(new OsuSkinComponentLookup(OsuSkinComponents.SliderTailHitCircle), _ => Empty())
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    Arrow = new ReverseArrowPiece(),
                }
            });

            ScaleBindable.BindValueChanged(scale => scaleContainer.Scale = new Vector2(scale.NewValue));
        }

        protected override void OnApply()
        {
            base.OnApply();

            Position = HitObject.Position - DrawableSlider.Position;
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (HitObject.StartTime <= Time.Current)
                ApplyResult(r => r.Type = DrawableSlider.Tracking.Value ? r.Judgement.MaxResult : r.Judgement.MinResult);
        }

        protected override void UpdateInitialTransforms()
        {
            animDuration = Math.Min(300, HitObject.SpanDuration);

            this.Animate(
                d => d.FadeIn(animDuration),
                d => d.ScaleTo(0.5f).ScaleTo(1f, animDuration * 2, Easing.OutElasticHalf)
            );
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            base.UpdateHitStateTransforms(state);

            switch (state)
            {
                case ArmedState.Idle:
                    this.Delay(HitObject.TimePreempt).FadeOut();
                    break;

                case ArmedState.Miss:
                    this.FadeOut(animDuration);
                    break;

                case ArmedState.Hit:
                    this.FadeOut(animDuration, Easing.Out);

                    const float final_scale = 1.5f;

                    Arrow.ScaleTo(Scale * final_scale, animDuration, Easing.Out);
                    CirclePiece.ScaleTo(Scale * final_scale, animDuration, Easing.Out);
                    break;
            }
        }

        private bool hasRotation;

        public void UpdateSnakingPosition(Vector2 start, Vector2 end)
        {
            // When the repeat is hit, the arrow should fade out on spot rather than following the slider
            if (IsHit) return;

            bool isRepeatAtEnd = HitObject.RepeatIndex % 2 == 0;
            List<Vector2> curve = ((PlaySliderBody)DrawableSlider.Body.Drawable).CurrentCurve;

            Position = isRepeatAtEnd ? end : start;

            if (curve.Count < 2)
                return;

            int searchStart = isRepeatAtEnd ? curve.Count - 1 : 0;
            int direction = isRepeatAtEnd ? -1 : 1;

            Vector2 aimRotationVector = Vector2.Zero;

            // find the next vector2 in the curve which is not equal to our current position to infer a rotation.
            for (int i = searchStart; i >= 0 && i < curve.Count; i += direction)
            {
                if (Precision.AlmostEquals(curve[i], Position))
                    continue;

                aimRotationVector = curve[i];
                break;
            }

            float aimRotation = MathUtils.RadiansToDegrees(MathF.Atan2(aimRotationVector.Y - Position.Y, aimRotationVector.X - Position.X));
            while (Math.Abs(aimRotation - Arrow.Rotation) > 180)
                aimRotation += aimRotation < Arrow.Rotation ? 360 : -360;

            // The clock may be paused in a scenario like the editor.
            if (!hasRotation || !Clock.IsRunning)
            {
                Arrow.Rotation = aimRotation;
                hasRotation = true;
            }
            else
            {
                // If we're already snaking, interpolate to smooth out sharp curves (linear sliders, mainly).
                Arrow.Rotation = Interpolation.ValueAt(Math.Clamp(Clock.ElapsedFrameTime, 0, 100), Arrow.Rotation, aimRotation, 0, 50, Easing.OutQuint);
            }
        }
    }
}

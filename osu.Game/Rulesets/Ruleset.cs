﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Linq;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Scoring;
using osu.Game.Overlays.Settings;

namespace osu.Game.Rulesets
{
    public abstract class Ruleset
    {
        public readonly RulesetInfo RulesetInfo;

        public virtual IEnumerable<BeatmapStatistic> GetBeatmapStatistics(WorkingBeatmap beatmap) => new BeatmapStatistic[] { };

        public IEnumerable<Mod> GetAllMods()
        {
            List<Mod> modList = new List<Mod>();

            foreach (ModType type in Enum.GetValues(typeof(ModType)))
                modList.AddRange(GetModsFor(type).Where(mod => mod != null).SelectMany(mod =>
                {
                    var multiMod = mod as MultiMod;

                    if (multiMod != null)
                        return multiMod.Mods;

                    return new[] { mod };
                }));

            return modList.ToArray();
        }

        public abstract IEnumerable<Mod> GetModsFor(ModType type);

        public Mod GetAutoplayMod() => GetAllMods().First(mod => mod is ModAutoplay);

        protected Ruleset(RulesetInfo rulesetInfo)
        {
            RulesetInfo = rulesetInfo;
        }

        /// <summary>
        /// Attempt to create a hit renderer for a beatmap
        /// </summary>
        /// <param name="beatmap">The beatmap to create the hit renderer for.</param>
        /// <param name="isForCurrentRuleset">Whether the hit renderer should assume the beatmap is for the current ruleset.</param>
        /// <exception cref="BeatmapInvalidForRulesetException">Unable to successfully load the beatmap to be usable with this ruleset.</exception>
        /// <returns></returns>
        public abstract RulesetContainer CreateRulesetContainerWith(WorkingBeatmap beatmap, bool isForCurrentRuleset);

        public abstract DifficultyCalculator CreateDifficultyCalculator(Beatmap beatmap);

        public abstract ScoreProcessor CreateScoreProcessor();

        public virtual Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.fa_question_circle };

        public abstract string Description { get; }

        public abstract IEnumerable<KeyCounter> CreateGameplayKeys();

        public virtual SettingsSubsection CreateSettings() => null;

        /// <summary>
        /// Do not override this unless you are a legacy mode.
        /// </summary>
        public virtual int LegacyID => -1;
    }
}

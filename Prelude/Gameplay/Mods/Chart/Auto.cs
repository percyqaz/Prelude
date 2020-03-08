﻿using Prelude.Utilities;

namespace Prelude.Gameplay.Mods
{
    //This mod sets all hits to dead on 0ms hits and marks every note as hit.
    //Allows the player to see the file play through without disabling HP systems/having to see the feedback for missing
    public class Auto : Mod
    {
        public override void ApplyToHitData(ChartWithModifiers Chart, ref HitData[] HitData, DataGroup Data)
        {
            for (int i = 0; i < HitData.Length; i++)
            {
                for (byte k = 0; k < HitData[i].hit.Length; k++)
                {
                    if (HitData[i].hit[k] == 1)
                    {
                        HitData[i].hit[k] = 2; //Marks all "needs to be hit" objects as "hit"
                    }
                }
            }
        }

        public override int Status => 2; //No auto score should be saved

        public override string Name => "Auto";

        public override string Description => "Automatically plays the chart for you";

        public override bool Visible => true;
    }
}
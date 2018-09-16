using ModComponentMapper;

namespace InstrumentPack
{
    public class Harmonica : Instrument
    {
        private const string SKILL_NAME = "Skill_Harmonica";

        internal override string InstrumentType => "harmonica";

        internal override string MistakeAudio => "Play_SndMistakeHarmonica";

        internal override Skill GetSkill()
        {
            return ModUtils.GetSkillByName(SKILL_NAME);
        }
    }
}
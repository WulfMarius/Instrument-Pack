using ModComponentMapper;

namespace InstrumentPack
{
    public class Guitar : Instrument
    {
        private const string SKILL_NAME = "Skill_Guitar";

        internal override string InstrumentType => "guitar";

        internal override string MistakeAudio => "Play_SndMistakeGuitar";

        internal override Skill GetSkill()
        {
            return ModUtils.GetSkillByName(SKILL_NAME);
        }
    }
}
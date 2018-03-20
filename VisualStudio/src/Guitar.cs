using ModComponentMapper;

namespace InstrumentPack
{
    public class Guitar : Instrument
    {
        private const string SKILL_NAME = "Skill_Guitar";

        internal override Skill GetSkill()
        {
            return ModUtils.GetSkillByName(SKILL_NAME);
        }

        internal override string InstrumentType => "guitar";
    }
}
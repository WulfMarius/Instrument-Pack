using UnityEngine;

namespace InstrumentPack
{
    public class Playing : MonoBehaviour
    {
        public Instrument Instrument;
        public string MistakeAudio;

        private const int SKILL_POINT_INTERVAL = 10;

        private float averageCorrectionDelay;
        private float averageMistakeDelay;
        private int maxMistake;

        private int lastAppliedOffset;
        private int offset;

        private float lastSkillPointUpdate;
        private float nextCorrection;
        private float nextMistake;
        private float timePlayed;

        public void RefreshSkillEffect()
        {
            float currentLevel = Instrument.GetSkill().GetCurrentTierNumber() + Instrument.GetSkill().GetProgressToNextLevelAsNormalizedValue(0);

            this.maxMistake = (int)(9 - currentLevel * 2);
            this.averageMistakeDelay = Mathf.Pow(5, currentLevel);
            this.averageCorrectionDelay = Mathf.Pow(0.8f, currentLevel);
        }

        private static float getRandomDelay(float average)
        {
            return Random.Range(average * 0.5f, average * 1.5f);
        }

        private void ApplyOffset()
        {
            if (lastAppliedOffset == offset)
            {
                return;
            }

            Instrument.SetPitch(Mathf.Pow(2, offset / 12f));
            lastAppliedOffset = offset;
        }

        private void AwardSkillPoints()
        {
            GameManager.GetSkillsManager().IncrementPointsAndNotify(Instrument.GetSkill().m_SkillType, SKILL_POINT_INTERVAL, SkillsManager.PointAssignmentMode.AssignInAnyMode);

            this.RefreshSkillEffect();

            lastSkillPointUpdate = timePlayed;
        }

        private void ChangeOffset()
        {
            this.offset += Random.Range(-this.maxMistake, this.maxMistake);
        }

        private void MakeCorrection()
        {
            if (offset < 0)
            {
                offset++;
            }
            else if (offset > 0)
            {
                offset--;
            }

            this.nextCorrection += getRandomDelay(averageCorrectionDelay);
        }

        private void MakeMistake()
        {
            this.offset = Random.Range(-maxMistake, maxMistake);
            GameAudioManager.PlaySound(MistakeAudio, this.gameObject);

            this.nextMistake += getRandomDelay(averageMistakeDelay);
        }

        private void Start()
        {
            this.timePlayed = 0;
            this.nextCorrection = getRandomDelay(averageCorrectionDelay);
            this.nextMistake = getRandomDelay(averageMistakeDelay);
        }

        private void Update()
        {
            if (GameManager.m_IsPaused)
            {
                if (this.Instrument.IsPlaying)
                {
                    this.Instrument.Pause();
                }
                return;
            }

            if (this.Instrument.IsPaused)
            {
                this.Instrument.Continue();
                return;
            }

            if (!this.Instrument.IsPlaying)
            {
                return;
            }

            if (InputManager.GetFirePressed(GameManager.Instance()))
            {
                this.Instrument.StopPlaying();
            }

            timePlayed += Time.deltaTime;
            if (timePlayed - lastSkillPointUpdate > SKILL_POINT_INTERVAL)
            {
                AwardSkillPoints();
            }

            if (timePlayed > nextCorrection)
            {
                MakeCorrection();
            }

            if (timePlayed > nextMistake)
            {
                MakeMistake();
            }

            ApplyOffset();
        }
    }
}
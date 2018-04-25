using ModComponentAPI;
using ModComponentMapper;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace InstrumentPack
{
    public abstract class Instrument
    {
        public EquippableModComponent EquippableModComponent;

        private const float TARGET_VOLUME = 0.4f;

        private SmbPitchShiftingSampleProvider pitchShifter;
        private AudioFileReader reader;
        private bool starting;
        private WaveOutEvent waveOutEvent;

        public bool IsPaused
        {
            get
            {
                return this.waveOutEvent != null && this.waveOutEvent.PlaybackState == PlaybackState.Paused;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return this.waveOutEvent != null && this.waveOutEvent.PlaybackState == PlaybackState.Playing;
            }
        }

        internal abstract string InstrumentType { get; }

        internal abstract string MistakeAudio { get; }

        public void Continue()
        {
            this.waveOutEvent.Play();

            LockControls();
        }

        public void OnEquipped()
        {
            EquipItemPopupUtils.ShowItemPopups(Localization.Get("GAMEPLAY_Play"), string.Empty, false, false, false, true);
        }

        public void OnPrimaryAction()
        {
            if (starting)
            {
                return;
            }

            if (IsPlaying)
            {
                StopPlaying();
            }
            else
            {
                this.EquippableModComponent.StartCoroutine(StartPlaying());
            }
        }

        public void OnUnequipped()
        {
            StopPlaying();
        }

        public void Pause()
        {
            this.waveOutEvent.Pause();

            RestoreControls();
        }

        public void SetPitch(float pitch)
        {
            this.pitchShifter.PitchFactor = pitch;
        }

        public void StopPlaying()
        {
            if (this.waveOutEvent == null)
            {
                return;
            }

            this.waveOutEvent.Stop();
        }

        internal abstract Skill GetSkill();

        private string GetSongsDirectory()
        {
            Assembly modComponentApiAssembly = Assembly.GetAssembly(typeof(ModToolComponent));
            if (modComponentApiAssembly == null)
            {
                Debug.Log("[Instrument-Pack] Could not resolve assembly containing ModToolComponent.");
                return null;
            }

            return Path.Combine(Path.Combine(Path.GetDirectoryName(modComponentApiAssembly.Location), "Instrument-Pack-Songs"), InstrumentType);
        }

        private void LockControls()
        {
            GameManager.GetPlayerManagerComponent().SetControlMode(PlayerControlMode.BigCarry);
            GameManager.GetPlayerMovementComponent().SetForceLimpSlow(true);
            GameManager.m_BlockNonMovementInput = true;
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            this.waveOutEvent.Dispose();
            this.waveOutEvent = null;

            this.reader.Dispose();
            this.reader = null;

            RestoreControls();
        }

        private void RestoreControls()
        {
            GameManager.m_BlockNonMovementInput = false;
            GameManager.GetPlayerMovementComponent().SetForceLimpSlow(false);

            if (GameManager.GetPlayerManagerComponent().GetControlMode() == PlayerControlMode.BigCarry)
            {
                GameManager.GetPlayerManagerComponent().SetControlMode(PlayerControlMode.Normal);
            }
        }

        private IEnumerator StartPlaying()
        {
            try
            {
                this.starting = true;
                LockControls();

                string songsDirectory = this.GetSongsDirectory();
                PrepareSongJob prepareSongJob = new PrepareSongJob(songsDirectory);
                prepareSongJob.Start();
                yield return this.EquippableModComponent.StartCoroutine(prepareSongJob.WaitFor());

                if (prepareSongJob.SelectedSong == null)
                {
                    HUDMessage.AddMessage("[Instrument-Pack]: No songs in directory '" + songsDirectory + "'");
                    yield break;
                }

                Debug.Log("[Instrument-Pack]: Selected song " + prepareSongJob.SelectedSong);

                this.reader = prepareSongJob.Reader;
                if (this.reader == null)
                {
                    HUDMessage.AddMessage("[Instrument-Pack]: Cannot play song '" + prepareSongJob.SelectedSong + "'.", 10, true);
                    yield break;
                }

                this.reader.Volume = TARGET_VOLUME * InterfaceManager.m_Panel_OptionsMenu.m_State.m_MasterVolume / prepareSongJob.MaxSample;

                this.pitchShifter = new SmbPitchShiftingSampleProvider(reader);

                this.waveOutEvent = new WaveOutEvent();
                this.waveOutEvent.PlaybackStopped += this.OnPlaybackStopped;
                this.waveOutEvent.Init(pitchShifter);
                this.waveOutEvent.Play();

                if (this.EquippableModComponent.EquippedModel != null)
                {
                    Playing playing = ModUtils.GetOrCreateComponent<Playing>(this.EquippableModComponent.EquippedModel);
                    playing.Instrument = this;
                    playing.MistakeAudio = this.MistakeAudio;
                    playing.RefreshSkillEffect();
                }
                else
                {
                    // the equipped model is gone -> stop playing
                    StopPlaying();
                }
            }
            finally
            {
                this.starting = false;
            }
        }
    }
}
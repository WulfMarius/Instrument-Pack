using ModComponentAPI;
using ModComponentMapper;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections;
using System.IO;
using System.Linq;
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

        internal abstract Skill GetSkill();

        private string GetSongsDirectory()
        {
            Assembly modComponentApiAssembly = Assembly.GetAssembly(typeof(ModToolComponent));
            if (modComponentApiAssembly == null)
            {
                Debug.Log("Could not resolve assembly containing ModToolComponent.");
                return null;
            }

            return Path.Combine(Path.Combine(Path.GetDirectoryName(modComponentApiAssembly.Location), "Instrument-Pack-Songs"), InstrumentType);
        }

        private void LockControls()
        {
            ModUtils.FreezePlayer();
            GameManager.GetPlayerManagerComponent().SetControlMode(PlayerControlMode.InConversation);
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
            GameManager.GetPlayerManagerComponent().SetControlMode(PlayerControlMode.Normal);
            ModUtils.UnfreezePlayer();
        }

        private string SelectNextSong()
        {
            string songsDirectory = GetSongsDirectory();
            string[] songs = Directory.Exists(songsDirectory) ? Directory.GetFiles(songsDirectory, "*.mp3").Where(file => file.EndsWith(".mp3")).ToArray() : new string[0];

            Debug.Log("Found " + songs.Length + " songs in " + songsDirectory);

            if (songs.Length == 0)
            {
                HUDMessage.AddMessage("No songs in directory '" + songsDirectory + "'");
                return null;
            }

            int index = Random.Range(0, songs.Count());
            return songs[index];
        }

        private IEnumerator StartPlaying()
        {
            this.starting = true;
            LockControls();

            string nextSong = this.SelectNextSong();
            if (nextSong == null)
            {
                this.starting = false;
                yield break;
            }

            this.reader = new AudioFileReader(nextSong);

            AnalyzeVolumeJob analyzeVolumeJob = new AnalyzeVolumeJob(this.reader);
            analyzeVolumeJob.Start();
            yield return this.EquippableModComponent.StartCoroutine(analyzeVolumeJob.WaitFor());
            this.reader.Volume = TARGET_VOLUME * InterfaceManager.m_Panel_OptionsMenu.m_State.m_MasterVolume / analyzeVolumeJob.maxSample;

            this.pitchShifter = new SmbPitchShiftingSampleProvider(reader);

            this.waveOutEvent = new WaveOutEvent();
            this.waveOutEvent.PlaybackStopped += this.OnPlaybackStopped;
            this.waveOutEvent.Init(pitchShifter);
            this.waveOutEvent.Play();

            Playing playing = ModUtils.GetOrCreateComponent<Playing>(this.EquippableModComponent.EquippedModel);
            playing.Instrument = this;
            playing.MistakeAudio = "Play_SndMistakeGuitar";
            playing.RefreshSkillEffect();

            this.starting = false;
        }

        private void StopPlaying()
        {
            if (this.waveOutEvent == null)
            {
                return;
            }

            this.waveOutEvent.Stop();
        }
    }
}
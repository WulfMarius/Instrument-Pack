using NAudio.Wave;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace InstrumentPack
{
    public class PrepareSongJob
    {
        public float MaxSample;
        public AudioFileReader Reader;
        public string SelectedSong;

        private readonly object m_Handle = new object();
        private readonly string songsDirectory;
        private bool m_IsDone;
        private System.Threading.Thread m_Thread;

        public PrepareSongJob(string songsDirectory)
        {
            this.songsDirectory = songsDirectory;
        }

        public bool IsDone
        {
            get
            {
                lock (m_Handle)
                {
                    return m_IsDone;
                }
            }
            set
            {
                lock (m_Handle)
                {
                    m_IsDone = value;
                }
            }
        }

        public virtual void Abort()
        {
            m_Thread.Abort();
        }

        public virtual void Start()
        {
            m_Thread = new System.Threading.Thread(Run);
            m_Thread.Start();
        }

        public IEnumerator WaitFor()
        {
            while (!IsDone)
            {
                yield return null;
            }
        }

        private void Run()
        {
            IsDone = false;
            try
            {
                ThreadFunction();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Instrument-Pack]: Failed to prepare song: " + e);
            }
            IsDone = true;
        }

        private string SelectSong()
        {
            string[] songs = Directory.Exists(songsDirectory) ? Directory.GetFiles(songsDirectory, "*.mp3").Where(file => file.EndsWith(".mp3")).ToArray() : new string[0];

            Debug.Log("[Instrument-Pack]: Found " + songs.Length + " songs in " + songsDirectory);

            if (songs.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, songs.Count());
            return songs[index];
        }

        private void ThreadFunction()
        {
            this.SelectedSong = this.SelectSong();
            if (SelectedSong == null)
            {
                return;
            }

            this.Reader = new AudioFileReader(SelectedSong);

            this.MaxSample = 0;

            this.Reader.Position = 0;

            float[] buffer = new float[this.Reader.WaveFormat.SampleRate];
            while (true)
            {
                int read = Reader.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                for (int i = 0; i < read; i++)
                {
                    var abs = Mathf.Abs(buffer[i]);
                    if (abs > this.MaxSample)
                    {
                        this.MaxSample = abs;
                    }
                }
            }

            this.Reader.Position = 0;

            if (MaxSample == 0)
            {
                MaxSample = 1;
            }
        }
    }
}
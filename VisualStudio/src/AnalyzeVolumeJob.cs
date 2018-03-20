using NAudio.Wave;
using System.Collections;
using UnityEngine;

namespace InstrumentPack
{
    public class AnalyzeVolumeJob
    {
        private readonly object m_Handle = new object();
        private bool m_IsDone;
        private System.Threading.Thread m_Thread;

        public float maxSample;
        private readonly AudioFileReader reader;

        public AnalyzeVolumeJob(AudioFileReader reader)
        {
            this.reader = reader;
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

        protected virtual void ThreadFunction()
        {
            this.maxSample = 0;

            this.reader.Position = 0;

            float[] buffer = new float[this.reader.WaveFormat.SampleRate];
            while (true)
            {
                int read = reader.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                for (int i = 0; i < read; i++)
                {
                    var abs = Mathf.Abs(buffer[i]);
                    if (abs > this.maxSample)
                    {
                        this.maxSample = abs;
                    }
                }
            }

            this.reader.Position = 0;

            if (maxSample == 0)
            {
                maxSample = 1;
            }
        }

        private void Run()
        {
            IsDone = false;
            ThreadFunction();
            IsDone = true;
        }
    }
}
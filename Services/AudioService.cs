using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using AppCleanerPro.Core;

namespace AppCleanerPro.Services
{
    public static class AudioService
    {
        private static readonly SystemEngine Engine = new();
        private static byte[]? _cleanChimeBytes;
        private static byte[]? _ramChimeBytes;

        static AudioService()
        {
            try
            {
                _cleanChimeBytes = GenerateCleanChimeWav();
                _ramChimeBytes = GenerateRamBoostChimeWav();
            }
            catch { }
        }

        public static void PlayCleanComplete()
        {
            Task.Run(() =>
            {
                try
                {
                    var settings = Engine.LoadSettings();
                    if (!settings.SoundEffects || _cleanChimeBytes == null) return;

                    using var ms = new MemoryStream(_cleanChimeBytes);
                    using var player = new SoundPlayer(ms);
                    player.Play();
                }
                catch { }
            });
        }

        public static void PlayRamBoosted()
        {
            Task.Run(() =>
            {
                try
                {
                    var settings = Engine.LoadSettings();
                    if (!settings.SoundEffects || _ramChimeBytes == null) return;

                    using var ms = new MemoryStream(_ramChimeBytes);
                    using var player = new SoundPlayer(ms);
                    player.Play();
                }
                catch { }
            });
        }

        // ── Procedural WAV Synthesis ─────────────────────────────────────────────

        private static byte[] GenerateCleanChimeWav()
        {
            const int sampleRate = 44100;
            // 3-note pleasant ascending chord: E5 (659Hz), A5 (880Hz), C#6 (1108Hz)
            double[] freqs = { 659.25, 880.0, 1108.73 };
            int noteSamples = (int)(sampleRate * 0.12);
            int tailSamples = (int)(sampleRate * 0.25);
            int totalSamples = (freqs.Length * noteSamples) + tailSamples;

            var pcm = new short[totalSamples];

            for (int i = 0; i < freqs.Length; i++)
            {
                double freq = freqs[i];
                int start = i * noteSamples;
                int len = totalSamples - start;

                for (int s = 0; s < len; s++)
                {
                    double t = (double)s / sampleRate;
                    // Exponential decay
                    double envelope = Math.Exp(-5.0 * t);
                    // Sine + gentle 2nd harmonic
                    double sample = (Math.Sin(2.0 * Math.PI * freq * t) * 0.75 +
                                     Math.Sin(2.0 * Math.PI * (freq * 2) * t) * 0.25) * envelope;

                    int idx = start + s;
                    if (idx < totalSamples)
                    {
                        int current = pcm[idx] + (int)(sample * 12000.0);
                        pcm[idx] = (short)Math.Clamp(current, short.MinValue, short.MaxValue);
                    }
                }
            }

            return CreateWav(pcm, sampleRate);
        }

        private static byte[] GenerateRamBoostChimeWav()
        {
            const int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * 0.35);
            var pcm = new short[totalSamples];

            // Futuristic upward frequency sweep from 580Hz to 1200Hz
            for (int s = 0; s < totalSamples; s++)
            {
                double t = (double)s / sampleRate;
                double sweep = 580.0 + (620.0 * Math.Pow((double)s / totalSamples, 1.5));
                double envelope = Math.Sin(Math.PI * Math.Min(t * 12.0, 1.0)) * Math.Exp(-6.5 * t);
                double sample = Math.Sin(2.0 * Math.PI * sweep * t) * envelope;

                pcm[s] = (short)Math.Clamp((int)(sample * 14000.0), short.MinValue, short.MaxValue);
            }

            return CreateWav(pcm, sampleRate);
        }

        private static byte[] CreateWav(short[] pcm, int sampleRate)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            int subchunk2Size = pcm.Length * 2; // 16-bit = 2 bytes per sample

            // RIFF chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + subchunk2Size);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt sub-chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);             // Subchunk1Size (16 for PCM)
            bw.Write((short)1);        // AudioFormat (1 = PCM)
            bw.Write((short)1);        // NumChannels (1 = Mono)
            bw.Write(sampleRate);      // SampleRate
            bw.Write(sampleRate * 2);  // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
            bw.Write((short)2);        // BlockAlign (NumChannels * BitsPerSample/8)
            bw.Write((short)16);       // BitsPerSample

            // data sub-chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(subchunk2Size);
            foreach (var sample in pcm)
                bw.Write(sample);

            bw.Flush();
            return ms.ToArray();
        }
    }
}

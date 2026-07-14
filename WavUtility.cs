using System;
using UnityEngine;

namespace RagdollMod
{
    public class WavUtility
    {
        public static AudioClip ToAudioClip(byte[] wavData)
        {
            try
            {
                if (wavData == null || wavData.Length < 44)
                {
                    Debug.LogError("WAV data is null or too small");
                    return null;
                }

                // Check RIFF header
                if (wavData[0] != 'R' || wavData[1] != 'I' || wavData[2] != 'F' || wavData[3] != 'F')
                {
                    Debug.LogError("Invalid WAV file - missing RIFF header");
                    return null;
                }

                // Read format chunk info
                int audioFormat = BitConverter.ToInt16(wavData, 20);
                int numChannels = BitConverter.ToInt16(wavData, 22);
                int sampleRate = BitConverter.ToInt32(wavData, 24);
                int byteRate = BitConverter.ToInt32(wavData, 28);
                int blockAlign = BitConverter.ToInt16(wavData, 32);
                int bitsPerSample = BitConverter.ToInt16(wavData, 34);
                int bytesPerSample = bitsPerSample / 8;

                // Find data chunk
                int dataPos = 36;
                int dataSize = 0;

                while (dataPos < wavData.Length - 8)
                {
                    if (wavData[dataPos] == 'd' && wavData[dataPos + 1] == 'a' && 
                        wavData[dataPos + 2] == 't' && wavData[dataPos + 3] == 'a')
                    {
                        dataSize = BitConverter.ToInt32(wavData, dataPos + 4);
                        dataPos += 8;
                        break;
                    }
                    dataPos++;
                }

                if (dataSize == 0 || dataPos >= wavData.Length)
                {
                    Debug.LogError("Could not find valid data chunk in WAV file");
                    return null;
                }

                // Calculate sample count correctly
                int sampleCount = dataSize / (numChannels * bytesPerSample);

                Debug.Log($"WAV Info: SampleRate={sampleRate}, Channels={numChannels}, BitsPerSample={bitsPerSample}, SampleCount={sampleCount}");

                // Read audio data
                float[] samples = new float[sampleCount * numChannels];
                int sampleIndex = 0;

                for (int i = 0; i < sampleCount; i++)
                {
                    for (int c = 0; c < numChannels; c++)
                    {
                        int byteOffset = dataPos + (i * numChannels + c) * bytesPerSample;

                        if (byteOffset + bytesPerSample > wavData.Length)
                        {
                            Debug.LogWarning($"Reading beyond buffer at offset {byteOffset}");
                            break;
                        }

                        if (bytesPerSample == 2)
                        {
                            short sample = BitConverter.ToInt16(wavData, byteOffset);
                            samples[sampleIndex++] = sample / 32768f;
                        }
                        else if (bytesPerSample == 1)
                        {
                            byte sample = wavData[byteOffset];
                            samples[sampleIndex++] = (sample - 128) / 128f;
                        }
                    }
                }

                // Create audio clip with correct parameters
                AudioClip audioClip = AudioClip.Create("oof", sampleCount, numChannels, sampleRate, false);
                audioClip.SetData(samples, 0);

                Debug.Log($"Successfully loaded WAV: {sampleRate}Hz, {numChannels} channels, {sampleCount} samples");
                return audioClip;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing WAV file: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
    }
}

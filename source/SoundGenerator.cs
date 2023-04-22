using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vibrante.UserControls;

namespace Vibrante
{
    internal class SoundGenerator
    {
        public static int sampleRate = 44100;

        /// <summary>
        /// Init tracks generation variables and output sound duration.
        /// </summary>
        /// <param name="sound_duration_in_ms">The duration between 0 and the last point.</param>
        /// <param name="amplitude_divider">The divisor to use to avoid saturation of the output.</param>
        public static void Init (out double sound_duration_in_ms, out double amplitude_divider)
        {
            sound_duration_in_ms = 0;

            double total_amplitude = 0;
            
            foreach (ComposerTrack composerTrack in MainWindow.composer.TracksContainer.Children)
            {
                composerTrack.generationVar_CurrentPitchIntegral = 0;
                composerTrack.generationVar_CurrentPitchPointIndex = 0;
                composerTrack.generationVar_CurrentVolumePointIndex = 0;
                composerTrack.generationVar_CurrentPanningPointIndex = 0;

                if (composerTrack.pitchTab.pointList.Count > 0)
                {
                    sound_duration_in_ms = Math.Max(sound_duration_in_ms, composerTrack.pitchTab.pointList.Last().X);
                }

                total_amplitude += composerTrack.volumeTab.constantValue ?? CommonUtils.GetMaxY(composerTrack.volumeTab.pointList);
            }

            amplitude_divider = total_amplitude / 100;
        }

        /// <summary>
        /// Create a sound based on the points of each track and export it to an output.wav file
        /// </summary>
        public static void Generate()
        {
            int trackIndex = 0;

            Init(out double soundDurationInMs, out double amplitudeDivider);

            bool useStereo = MainWindow.composer.stereoEnabled;
            int sampleCount = ((int)Math.Ceiling((soundDurationInMs / 1000) * sampleRate));

            WaveFileWriter waveFileWriter = new WaveFileWriter("output.wav", new WaveFormat(sampleRate, useStereo ? 2 : 1));

            for (int i = 0; i < sampleCount; i++)
            {
                // Time in ms of the current sample
                double currentTimeInMs = ((double)i / sampleRate * 1000);

                float sampleValue = 0;

                // Used instead of sampleValue for stereo sounds
                float leftSampleValue = 0;
                float rightSampleValue = 0;


                foreach (ComposerTrack composerTrack in MainWindow.composer.TracksContainer.Children)
                {
                    // If the track contains pitch points
                    if (composerTrack.pitchTab.pointList.Count > 0)
                    {
                        // If we are after the first pitch point
                        if (currentTimeInMs >= composerTrack.pitchTab.pointList.First().X)
                        {
                            // If the last point is not yet reached
                            if (currentTimeInMs < composerTrack.pitchTab.pointList.Last().X)
                            {
                                double currentFrequency = composerTrack.pitchTab.GetValueFromPointList(currentTimeInMs, ref composerTrack.generationVar_CurrentPitchPointIndex);
                                double angleIncrement = 2 * Math.PI * currentFrequency / sampleRate;

                                composerTrack.generationVar_CurrentPitchIntegral += angleIncrement;

                                // If the amplitude is not constant and there is no point, set it to 100
                                if (composerTrack.volumeTab.pointList.Count == 0 && composerTrack.volumeTab.constantValue == null)
                                    composerTrack.volumeTab.constantValue = 100;

                                double amplitude = composerTrack.volumeTab.constantValue == null ?
                                    composerTrack.volumeTab.GetValueFromPointList(currentTimeInMs, ref composerTrack.generationVar_CurrentVolumePointIndex) / 100
                                    : (double)composerTrack.volumeTab.constantValue / 100;

                                amplitude /= amplitudeDivider;

                                if (!useStereo) // Mono
                                {
                                    sampleValue += (float)(amplitude * Math.Sin(composerTrack.generationVar_CurrentPitchIntegral));
                                }
                                else // Stereo
                                {
                                    double panning = composerTrack.panningTab.constantValue == null ?
                                        composerTrack.panningTab.GetValueFromPointList(currentTimeInMs, ref composerTrack.generationVar_CurrentPanningPointIndex) / 100
                                        : (double)composerTrack.panningTab.constantValue / 100;

                                    double leftAmplitude = amplitude;
                                    double rightAmplitude = amplitude;

                                    if (panning < 0)
                                    {
                                        leftAmplitude *= panning + 1;
                                    }
                                    else if (panning > 0)
                                    {
                                        rightAmplitude *= 1 - panning;
                                    }

                                    leftAmplitude /= amplitudeDivider;
                                    rightAmplitude /= amplitudeDivider;

                                    leftSampleValue += (float)(leftAmplitude * Math.Sin(composerTrack.generationVar_CurrentPitchIntegral));
                                    rightSampleValue += (float)(rightAmplitude * Math.Sin(composerTrack.generationVar_CurrentPitchIntegral));
                                }
                            }
                        }
                    }

                }

                if (useStereo)
                {
                    waveFileWriter.WriteSample(CommonUtils.Clamp(leftSampleValue, -1, 1));
                    waveFileWriter.WriteSample(CommonUtils.Clamp(rightSampleValue, -1, 1));
                }
                else
                {
                    waveFileWriter.WriteSample(CommonUtils.Clamp(sampleValue, -1, 1));
                }

            }

            waveFileWriter.Close();
        }
    }
}

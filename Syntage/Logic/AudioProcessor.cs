﻿using System.Collections.Generic;
using Jacobi.Vst.Core;
using Jacobi.Vst.Framework;
using Jacobi.Vst.Framework.Plugin;
using Syntage.Framework.Parameters;
using Syntage.Logic.Audio;
using Syntage.Plugin;

namespace Syntage.Logic
{
    public class AudioProcessor : VstPluginAudioProcessorBase, IVstPluginBypass, IAudioStreamProvider
    {
        private readonly List<AudioStream> _audioStreams = new List<AudioStream>();
        private readonly AudioStream _mainStream;
        private bool _bypass;
        
        public readonly PluginController PluginController;

        public Input Input { get; }

        public Routing Commutator { get; }
        public Oscillator OscillatorA { get; }
        public Oscillator OscillatorB { get; }
        public Noise NoiseGenerator { get; }
        public ADSR Envelope { get; }
        public ButterworthFilter Filter { get; }
        public Distortion Distortion { get; }
        public Clip Clip { get; }
        public Master Master { get; }
		public Oscillograph Oscillograph { get; }

        public AudioProcessor(Plugin.PluginController pluginController) :
			base(0, 2, 0)
        {
            PluginController = pluginController;
            _mainStream = (AudioStream)CreateAudioStream();

            Input = new Input(this);

            Commutator = new Routing(this);
            Envelope = new ADSR(this);
            OscillatorA = new Oscillator(this);
            OscillatorB = new Oscillator(this);
            NoiseGenerator = new Noise(this);
            Filter = new ButterworthFilter(this);
            Distortion = new Distortion(this);
            Clip = new Clip(this);
            Master = new Master(this);
			Oscillograph = new Oscillograph(this);
		}

        public bool Bypass
        {
            get { return _bypass; }
            set
            {
                _bypass = value;
                Commutator.Power.Value = (_bypass) ? EPowerStatus.Off : EPowerStatus.On;
            }
        }

        public override int BlockSize
        {
            get { return base.BlockSize; }
            set
            {
                if (base.BlockSize == value)
                    return;

                base.BlockSize = value;

                foreach (var stream in _audioStreams)
                    stream.SetBlockSize(BlockSize);
            }
        }

        public IEnumerable<Parameter> CreateParameters()
        {
            var parameters = new List<Parameter>();

            parameters.AddRange(Commutator.CreateParameters("C"));
            parameters.AddRange(OscillatorA.CreateParameters("A"));
            parameters.AddRange(Envelope.CreateParameters("E"));
            parameters.AddRange(OscillatorB.CreateParameters("B"));
            parameters.AddRange(NoiseGenerator.CreateParameters("N"));
            parameters.AddRange(Filter.CreateParameters("F"));
            parameters.AddRange(Distortion.CreateParameters("D"));
            parameters.AddRange(Clip.CreateParameters("K"));
			parameters.AddRange(Master.CreateParameters("M"));
			parameters.AddRange(Oscillograph.CreateParameters("O"));
			
			return parameters;
        }

        public IAudioStream CreateAudioStream()
        {
            var stream = new AudioStream();
            stream.Initialize(OutputCount, this);
            stream.SetBlockSize(BlockSize);

            _audioStreams.Add(stream);

            return stream;
        }

        public void ReleaseAudioStream(IAudioStream stream)
        {
            _audioStreams.Remove(stream as AudioStream);
        }

        public int CurrentStreamLenght { get; private set; }

        public override void Process(VstAudioBuffer[] inChannels, VstAudioBuffer[] outChannels)
        {
            CurrentStreamLenght = outChannels[0].SampleCount;

            Commutator.Process(_mainStream);

            // отправляем результат 
            _mainStream.WriteToVstOut(outChannels);
        }
    }
}

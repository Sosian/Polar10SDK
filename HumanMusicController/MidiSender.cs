using System;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace HumanMusicController
{
    public class MidiSender
    {
        private readonly string midiPortName;

        public MidiSender(string midiPortName)
        {
            this.midiPortName = midiPortName;

        }

        public void SendMidiMsg(int heartrate)
        {
            using (var outputDevice = OutputDevice.GetByName(midiPortName))
            {
                outputDevice.SendEvent(new NoteOnEvent(new SevenBitNumber((byte)heartrate), new SevenBitNumber((byte)heartrate)));
                Thread.Sleep(500);
                outputDevice.SendEvent(new NoteOffEvent(new SevenBitNumber((byte)heartrate), new SevenBitNumber((byte)heartrate)));
            }
        }
    }
}
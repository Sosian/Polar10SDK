using System;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace PolarH10
{
    public class MidiSender
    {
        public MidiSender()
        {

        }

        public void SendMidiMsg(int heartrate)
        {
            using (var outputDevice = OutputDevice.GetByName("loopMIDI Port"))
            {
                    outputDevice.SendEvent(new NoteOnEvent(new SevenBitNumber((byte)heartrate), new SevenBitNumber((byte)heartrate)));
                    outputDevice.SendEvent(new NoteOffEvent(new SevenBitNumber((byte)heartrate), new SevenBitNumber((byte)heartrate)));
            }
        }
    }
}
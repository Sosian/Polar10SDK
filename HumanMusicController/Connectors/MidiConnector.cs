namespace HumanMusicController.Connectors
{
    public class MidiConnector : IConnector
    {
        private readonly MidiSender midiSender;

        public MidiConnector(MidiSender midiSender)
        {
            this.midiSender = midiSender;
        }

        public void ReceiveData(HrPayload hrPayload)
        {
            midiSender.SendMidiMsg(hrPayload.Heartrate);
        }
    }
}
namespace HumanMusicController
{
    public class HrPayload
    {
        public int Heartrate { get; private set; }

        public HrPayload(int heartrate)
        {
            this.Heartrate = heartrate;
        }

        public override string ToString()
        {
            return Heartrate.ToString();
        }
    }
}
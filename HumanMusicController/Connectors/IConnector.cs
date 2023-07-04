namespace HumanMusicController.Connectors
{
    public interface IConnector
    {
        public void ReceiveData(HrPayload hrPayload);
    }
}
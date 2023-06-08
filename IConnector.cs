namespace PolarH10
{
    public interface IConnector
    {
        public void ReceiveData(HrPayload hrPayload);
    }
}
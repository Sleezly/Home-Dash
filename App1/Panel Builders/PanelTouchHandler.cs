namespace Hashboard
{
    public class PanelTouchHandler
    {
        public enum ResponseExpected
        {
            EntityUpdated,
            None,
        };

        public string Service { get; }
        public ResponseExpected Response { get; }

        public PanelTouchHandler(string serviceAction, ResponseExpected responseExpected)
        {
            Service = serviceAction;
            Response = responseExpected;
        }
    }
}

namespace HelloDev.Entities
{
    internal interface IEcsEventChannel
    {
        void Flush();
        void Dispose();
    }
}

namespace Hourglass.Ecs
{
    public interface IComponent
    {
        int OwnerId { get; set; }
        string ComponentName { get; }

        bool CanSerialize { get; }
        void Deserialize(byte[] data);
        byte[] Serialize();
    }
}

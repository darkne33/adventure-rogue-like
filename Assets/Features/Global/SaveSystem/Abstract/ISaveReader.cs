namespace Infrastructure.SaveSystem
{
    public interface ISaveReader<in T>
    {
        public void ReadSave(T data);
    }
}
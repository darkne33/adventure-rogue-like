namespace Infrastructure.SaveSystem
{
    public interface ISaveWriter<in T>
    {
        public void WriteSave(T saveData);
    }
}